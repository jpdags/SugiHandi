// PuzzleMarker.cs — A single tappable marker in the environmental interaction sequence.
//
// SETUP IN UNITY:
//   1. Create marker GameObjects (e.g. stone marker sprites) in the scene.
//   2. Add this component + a CircleCollider2D (isTrigger = true) to each.
//   3. Set markerIndex (0-based: the marker's position in the correct sequence).
//   4. Assign parentPuzzle (the EnvironmentalPuzzle component on the parent/manager object).
//   5. WorldManager.HandleTap() detects PuzzleMarker via Physics2D.OverlapPoint.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PuzzleMarker : MonoBehaviour
{
    [Tooltip("0-based index of this marker's position in the correct tap sequence.\n" +
             "e.g. the marker that must be tapped FIRST has index 0.")]
    public int markerIndex;

    [Tooltip("Reference to the EnvironmentalPuzzle manager that owns this marker sequence.")]
    public EnvironmentalPuzzle parentPuzzle;

    [Header("Visual Feedback")]
    [Tooltip("Renderer to show a 'tapped' visual state (e.g. change sprite color or enable a glow).")]
    public SpriteRenderer markerRenderer;

    [Tooltip("Color when the marker has been tapped (dim, showing it's been activated).")]
    public Color tappedColor = new Color(0.6f, 0.9f, 0.6f, 1f);

    private bool _hasBeedTapped = false;

    // ── Called by WorldManager ───────────────────────────────────────────────
    /// <summary>
    /// Invoked by WorldManager.HandleTap() when this marker's collider is hit.
    /// Reports to the parent EnvironmentalPuzzle.
    /// </summary>
    public void OnTapped()
    {
        if (_hasBeedTapped) return; // Already activated in current sequence attempt

        if (parentPuzzle != null)
            parentPuzzle.OnMarkerTapped(markerIndex);
    }

    /// <summary>
    /// Called by EnvironmentalPuzzle to visually mark this marker as activated.
    /// </summary>
    public void SetTappedVisual()
    {
        _hasBeedTapped = true;
        if (markerRenderer != null)
            markerRenderer.color = tappedColor;
    }

    /// <summary>
    /// Called by EnvironmentalPuzzle to reset this marker's visual state
    /// when a wrong tap resets the sequence.
    /// </summary>
    public void ResetVisual()
    {
        _hasBeedTapped = false;
        if (markerRenderer != null)
            markerRenderer.color = Color.white;
    }
}
