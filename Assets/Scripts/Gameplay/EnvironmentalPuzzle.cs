// EnvironmentalPuzzle.cs — Manages the tap-markers-in-order environmental interaction.
//
// From the dev reference §5.6:
//   "Specific scenes replace tap-to-talk with a puzzle-like sequence —
//    e.g. tap eight stone markers in a culturally significant order.
//    No explicit instructions. No timer. No failure state."
//
// How it works:
//   - markersInOrder[] defines the CORRECT sequence (assign PuzzleMarkers in correct order).
//   - Player taps markers in any order.
//   - If the tapped marker is the next correct one in sequence → record it, show visual.
//   - If the tapped marker is WRONG → silently reset the sequence (no failure message shown).
//   - When all markers tapped in order → fire GameEvents.RaisePuzzleCompleted(puzzleId).
//
// NO instructions shown. NO timer. NO failure message. The reset is silent.
//
// SETUP IN UNITY:
//   1. Create an empty "EnvironmentalPuzzle" GameObject in the Objects parent.
//   2. Add this component.
//   3. Assign markersInOrder[] with PuzzleMarker components in the correct order.
//   4. Set puzzleId to a unique string.

using System.Collections.Generic;
using UnityEngine;

public class EnvironmentalPuzzle : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID for this puzzle. Saved to disk on completion.")]
    public string puzzleId;

    [Header("Sequence")]
    [Tooltip("The PuzzleMarker components in the CORRECT tap order. Index 0 = first to tap.")]
    public List<PuzzleMarker> markersInOrder = new List<PuzzleMarker>();

    // ── State ────────────────────────────────────────────────────────────────
    private int _nextExpectedIndex = 0;
    private bool _isComplete = false;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Start()
    {
        // If already completed in a prior session, disable this puzzle
        if (ProgressTracker.Instance != null && ProgressTracker.Instance.HasCompletedPuzzle(puzzleId))
        {
            _isComplete = true;
            SetAllMarkersCompleteVisual();
            Debug.Log($"[EnvironmentalPuzzle] '{puzzleId}' was already completed — restoring state.");
        }
    }

    // ── Called by PuzzleMarker ───────────────────────────────────────────────
    /// <summary>
    /// Called by PuzzleMarker.OnTapped() when the player taps a marker.
    /// markerIndex is the 0-based index of the tapped marker.
    /// </summary>
    public void OnMarkerTapped(int markerIndex)
    {
        if (_isComplete) return;

        // Is this the next expected marker in the sequence?
        if (_nextExpectedIndex < markersInOrder.Count &&
            markersInOrder[_nextExpectedIndex].markerIndex == markerIndex)
        {
            // Correct tap
            markersInOrder[_nextExpectedIndex].SetTappedVisual();
            _nextExpectedIndex++;

            if (_nextExpectedIndex >= markersInOrder.Count)
            {
                OnSequenceComplete();
            }
        }
        else
        {
            // Wrong order — silently reset. No error shown to player.
            ResetSequence();
        }
    }

    // ── Sequence Events ──────────────────────────────────────────────────────
    private void OnSequenceComplete()
    {
        _isComplete = true;
        Debug.Log($"[EnvironmentalPuzzle] Sequence complete: {puzzleId}");

        // Visual feedback for completion (optional — add particle effect, sound, etc.)
        SetAllMarkersCompleteVisual();

        GameEvents.RaisePuzzleCompleted(puzzleId);
    }

    /// <summary>
    /// Silently resets the sequence when a wrong marker is tapped.
    /// No failure message, no sound — the player simply tries again from the start.
    /// </summary>
    private void ResetSequence()
    {
        _nextExpectedIndex = 0;
        foreach (var marker in markersInOrder)
        {
            marker.ResetVisual();
        }
        // Intentionally no debug log or UI feedback — the reset is invisible to the player
    }

    private void SetAllMarkersCompleteVisual()
    {
        foreach (var marker in markersInOrder)
        {
            marker.SetTappedVisual();
        }
    }
}
