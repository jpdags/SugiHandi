// PlayerMovement.cs — Tap-to-move for the Manaog (player character).
//
// Mechanic: player taps a destination → Manaog walks there smoothly.
// Tapping an NPC or artifact → routed to WorldManager.HandleTap() → interaction triggered.
// No virtual joystick. No drag-to-move. Single-tap only.
//
// Input: Unity Input System (com.unity.inputsystem)
//   Uses Pointer.current for both mouse (editor) and touch (Android).
//
// Movement: Coroutine-based smooth movement via Vector2.MoveTowards.
//   For demo scope, this is sufficient. No A* needed for a single small scene.
//
// SETUP IN UNITY:
//   1. Add PlayerMovement to the Manaog GameObject.
//   2. Add a Rigidbody2D (Kinematic) and Collider2D to the Manaog.
//   3. Set moveSpeed in the Inspector.
//   4. Assign animatorController (optional — for walk/idle animation).
//   5. Install: Window → Package Manager → "Input System"

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("World units per second the Manaog walks.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Distance threshold at which the player is considered 'at' the destination.")]
    public float arrivalThreshold = 0.05f;

    [Header("References")]
    [Tooltip("Optional: Animator component for walk/idle transitions.")]
    public Animator animator;

    // ── State ────────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Vector2 _targetPosition;
    private Coroutine _moveCoroutine;
    private bool _isMoving = false;
    private bool _isInputLocked = false; // Locked during dialogue/examine

    public bool IsMoving => _isMoving;

    // Animator parameter hashes (cache for performance)
    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; // 2D top-down — no gravity
        _rb.freezeRotation = true;
        _targetPosition = transform.position;
    }

    private void Start()
    {
        // Lock input when dialogue is active
        GameEvents.OnDialogueStart += _ => LockInput(true);
        GameEvents.OnDialogueEnd   += () => LockInput(false);
    }

    private void OnDestroy()
    {
        GameEvents.OnDialogueStart -= _ => LockInput(true);
        GameEvents.OnDialogueEnd   -= () => LockInput(false);
    }

    private void Update()
    {
        HandleTapInput();
    }

    // ── Input Handling ───────────────────────────────────────────────────────
    private void HandleTapInput()
    {
        if (_isInputLocked) return;
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        // Ask WorldManager if this tap hits an interactable
        bool tappedInteractable = WorldManager.Instance != null && WorldManager.Instance.HandleTap(worldPos);

        if (!tappedInteractable)
        {
            // Move player to tapped world position
            MoveTo(worldPos);
        }
    }

    // ── Movement ─────────────────────────────────────────────────────────────
    public void MoveTo(Vector2 destination)
    {
        _targetPosition = destination;

        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget()
    {
        _isMoving = true;
        SetWalkAnimation(true);

        while (Vector2.Distance(_rb.position, _targetPosition) > arrivalThreshold)
        {
            Vector2 newPos = Vector2.MoveTowards(_rb.position, _targetPosition, moveSpeed * Time.deltaTime);
            _rb.MovePosition(newPos);

            // Face direction of movement
            UpdateFacingDirection(_targetPosition - _rb.position);

            yield return null;
        }

        _rb.MovePosition(_targetPosition);
        _isMoving = false;
        SetWalkAnimation(false);
        _moveCoroutine = null;
    }

    private void UpdateFacingDirection(Vector2 direction)
    {
        if (animator == null) return;
        // For a 4-directional top-down sprite, set Horizontal/Vertical parameters
        // Assumes your Animator uses "Horizontal" and "Vertical" floats
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animator.SetFloat("Horizontal", Mathf.Sign(direction.x));
            animator.SetFloat("Vertical", 0f);
        }
        else
        {
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", Mathf.Sign(direction.y));
        }
    }

    private void SetWalkAnimation(bool isWalking)
    {
        if (animator != null)
            animator.SetBool(AnimIsWalking, isWalking);
    }

    // ── Input Lock (during dialogue / examine screen) ─────────────────────────
    public void LockInput(bool locked)
    {
        _isInputLocked = locked;
        if (locked)
        {
            // Stop any current movement when input is locked
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            _isMoving = false;
            SetWalkAnimation(false);
        }
    }
}
