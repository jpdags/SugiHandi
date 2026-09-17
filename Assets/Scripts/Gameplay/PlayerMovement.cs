// PlayerMovement.cs — Tap-to-move for the Manaog (player character).
//
// Mechanic: player taps a destination ? Manaog walks there smoothly.
// Tapping an NPC or artifact ? routed to WorldManager.HandleTap() ? interaction triggered.
// No virtual joystick. No drag-to-move. Single-tap only.
//
// Input: Unity Input System (com.unity.inputsystem)
//   Uses Pointer.current for both mouse (editor) and touch (Android).
//
// Movement: Physics-based via Rigidbody2D.MovePosition in FixedUpdate.
//   Frame-rate independent; plays correctly with a Kinematic Rigidbody2D.
//
// Animation: Uses animator.Play() directly — no state-machine parameter
//   dependencies (the IsWalking bool was never added to the controller and
//   caused transitions to never fire). Four directional walk clips + one
//   shared idle. The player idles facing the direction they last walked.
//
// SETUP IN UNITY:
//   1. Add PlayerMovement to the Manaog GameObject.
//   2. Add a Rigidbody2D (Body Type: Kinematic) and Collider2D to the Manaog.
//   3. Set moveSpeed in the Inspector (default 5).
//   4. Assign the Animator component in the Inspector.
//   5. Ensure the Animator Controller has states named exactly:
//        Player_idle
//        Player_walkFront  Player_walkBack  Player_walkLeft  Player_walkRight
//   6. Install: Window ? Package Manager ? "Input System"

using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // -- Inspector -------------------------------------------------------------
    [Header("Movement")]
    [Tooltip("World units per second the Manaog walks.")]
    public float moveSpeed = 5f;

    [Tooltip("Distance threshold at which the player is considered 'at' the destination.")]
    public float arrivalThreshold = 0.08f;

    [Header("References")]
    [Tooltip("Animator component for walk/idle/direction transitions.")]
    public Animator animator;

    [Header("UI Highlight")]
    [Tooltip("If true, shows a grid highlight where tapped.")]
    public bool showGridHighlight = true;
    public Color highlightColor        = new Color(1f, 0.85f, 0.3f, 0.55f);
    public Color blockedHighlightColor = new Color(1f, 0.25f, 0.25f, 0.65f);

    [Header("Obstacle Navigation")]
    [Tooltip("If true, tapping an obstacle moves the player to the nearest walkable adjacent tile. If false, tap is ignored.")]
    public bool moveToNearestIfBlocked = false;

    // -- State -----------------------------------------------------------------
    private Rigidbody2D    _rb;
    private Vector2        _targetPosition;
    private bool           _isMoving      = false;
    private bool           _isInputLocked = false;
    private Camera         _mainCamera;
    private GameObject     _highlightCursor;
    private SpriteRenderer _highlightRenderer;
    private Grid           _grid;

    // Cached event delegates so we can properly unsubscribe in OnDestroy
    private Action<DialogueData> _onDialogueStart;
    private Action               _onDialogueEnd;

    // Animation state tracking — avoids restarting a clip every frame
    private string _lastPlayedAnim = "";

    // Tracks last facing direction so idle plays in the correct facing direction
    private enum FacingDir { Front, Back, Left, Right }
    private FacingDir _facingDir = FacingDir.Front;

    public bool IsMoving => _isMoving;

    // -- Lifecycle ------------------------------------------------------------
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType               = RigidbodyType2D.Kinematic; // enforce — safe if editor set wrong
        _rb.gravityScale           = 0f;
        _rb.freezeRotation         = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _targetPosition = _rb.position;

        // Cache Camera.main once — calling Camera.main every frame is expensive
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        // Store delegates so they can be properly unsubscribed later
        _onDialogueStart = _ => LockInput(true);
        _onDialogueEnd   = () => LockInput(false);

        GameEvents.OnDialogueStart += _onDialogueStart;
        GameEvents.OnDialogueEnd   += _onDialogueEnd;

        _grid = FindObjectOfType<Grid>();

        if (showGridHighlight && _grid != null)
            CreateHighlightCursor();

        // Start in idle-front state
        PlayAnim("Player_idle");
    }

    private void OnDestroy()
    {
        // Use the stored delegates — anonymous lambdas cannot be unsubscribed
        if (_onDialogueStart != null) GameEvents.OnDialogueStart -= _onDialogueStart;
        if (_onDialogueEnd   != null) GameEvents.OnDialogueEnd   -= _onDialogueEnd;

        if (_highlightCursor != null) Destroy(_highlightCursor);
    }

    // -- Input (Update — runs every frame) -----------------------------------
    private void Update()
    {
        HandleTapInput();
    }

    private void HandleTapInput()
    {
        if (_isInputLocked) return;
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos  = _mainCamera.ScreenToWorldPoint(screenPos);

        // Let WorldManager check for NPC / artifact taps first
        bool tappedInteractable = WorldManager.Instance != null
                               && WorldManager.Instance.HandleTap(worldPos);
        if (tappedInteractable) return;

        // Snap to grid cell centre if a Grid is present
        Vector2 destination = SnapToGrid(worldPos);

        // Check tile walkability
        bool isWalkable = WalkabilityManager.Instance == null
                       || WalkabilityManager.Instance.IsWalkable(destination);

        if (isWalkable)
        {
            ShowHighlight(destination, highlightColor);
            MoveTo(destination);
        }
        else if (moveToNearestIfBlocked
              && WalkabilityManager.Instance != null
              && WalkabilityManager.Instance.TryGetNearestWalkable(destination, out Vector2 nearest))
        {
            ShowHighlight(nearest, highlightColor);
            MoveTo(nearest);
        }
        else
        {
            // Blocked — red tile feedback; player does not move
            ShowHighlight(destination, blockedHighlightColor);
        }
    }

    // -- Movement (FixedUpdate — synced with physics) -------------------------
    private void FixedUpdate()
    {
        if (!_isMoving) return;

        Vector2 currentPos   = _rb.position;
        float   distToTarget = Vector2.Distance(currentPos, _targetPosition);

        if (distToTarget <= arrivalThreshold)
        {
            // Snap exactly to destination and stop
            _rb.MovePosition(_targetPosition);
            StopMoving();
            return;
        }

        // Step toward target — MoveTowards guarantees no overshoot
        float   step   = moveSpeed * Time.fixedDeltaTime;
        Vector2 newPos = Vector2.MoveTowards(currentPos, _targetPosition, step);
        _rb.MovePosition(newPos);

        // Update facing direction from actual movement delta
        UpdateFacingDirection(_targetPosition - currentPos);
    }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Commands the player to walk toward <paramref name="destination"/>.
    /// Safe to call from external systems (interactables, cutscene triggers, etc.).
    /// </summary>
    public void MoveTo(Vector2 destination)
    {
        // Ignore taps right on the player — prevents idle-flicker on fat fingers
        if (Vector2.Distance(_rb.position, destination) < arrivalThreshold) return;

        _targetPosition = destination;

        if (!_isMoving)
        {
            _isMoving = true;
            // Kick off walk anim immediately using current facing direction
            PlayWalkAnim(_facingDir);
        }
    }

    // -- Input Lock (dialogue / examine) -------------------------------------

    /// <summary>Locks or unlocks player input. Subscribed to OnDialogueStart / OnDialogueEnd.</summary>
    public void LockInput(bool locked)
    {
        _isInputLocked = locked;

        if (locked)
        {
            _isMoving          = false;
            _rb.linearVelocity = Vector2.zero;
            PlayIdleAnim();

            if (_highlightCursor != null)
                _highlightCursor.SetActive(false);
        }
    }

    // -- Animation ------------------------------------------------------------

    /// <summary>
    /// Resolves which directional walk clip to use from the movement delta,
    /// updates _facingDir, and plays the clip if direction changed.
    /// </summary>
    private void UpdateFacingDirection(Vector2 delta)
    {
        if (animator == null) return;

        FacingDir newDir;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            newDir = delta.x > 0 ? FacingDir.Right : FacingDir.Left;
        else
            newDir = delta.y > 0 ? FacingDir.Back : FacingDir.Front;

        // Only update if direction actually changed — avoids redundant Play calls
        if (newDir == _facingDir && _lastPlayedAnim.StartsWith("Player_walk")) return;

        _facingDir = newDir;
        PlayWalkAnim(_facingDir);
    }

    private void PlayWalkAnim(FacingDir dir)
    {
        string clip = dir switch
        {
            FacingDir.Back  => "Player_walkBack",
            FacingDir.Left  => "Player_walkLeft",
            FacingDir.Right => "Player_walkRight",
            _               => "Player_walkFront",
        };
        PlayAnim(clip);
    }

    private void PlayIdleAnim()
    {
        // Single shared idle state — direction is carried over visually via the
        // last-played walk frame (Write Defaults: On in the animator controller).
        PlayAnim("Player_idle");
    }

    /// <summary>
    /// Plays an animator state by name, layer 0.
    /// Skips the call when the same state is already running to avoid clip restarts.
    /// </summary>
    private void PlayAnim(string stateName)
    {
        if (animator == null) return;
        if (stateName == _lastPlayedAnim) return;

        _lastPlayedAnim = stateName;
        animator.Play(stateName, 0, 0f); // layer 0, restart from normalised time 0
    }

    private void StopMoving()
    {
        _isMoving = false;
        PlayIdleAnim();
        // Highlight stays visible — marks where the player last tapped
    }

    // -- Helpers --------------------------------------------------------------

    /// <summary>Snaps a world position to the nearest Grid cell centre (if a Grid is present).</summary>
    private Vector2 SnapToGrid(Vector2 worldPos)
    {
        if (_grid == null) return worldPos;
        Vector3Int cell = _grid.WorldToCell(worldPos);
        return _grid.GetCellCenterWorld(cell);
    }

    private void CreateHighlightCursor()
    {
        _highlightCursor = new GameObject("ClickHighlight");
        _highlightCursor.transform.SetParent(null); // Scene root — not parented to player

        _highlightRenderer = _highlightCursor.AddComponent<SpriteRenderer>();
        _highlightRenderer.sortingOrder = 10;

        // 1x1 white pixel scaled to grid cell size
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _highlightRenderer.sprite = Sprite.Create(tex,
            new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        _highlightRenderer.color = highlightColor;

        _highlightCursor.transform.localScale = _grid.cellSize;
        _highlightCursor.SetActive(false); // Hidden until first tap
    }

    private void ShowHighlight(Vector2 worldPos, Color color)
    {
        if (_highlightCursor == null) return;
        _highlightRenderer.color           = color;
        _highlightCursor.transform.position = (Vector3)worldPos;
        _highlightCursor.SetActive(true);
    }
}
