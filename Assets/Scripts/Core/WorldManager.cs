// WorldManager.cs — Detects player proximity to NPCs, artifacts, and puzzle markers.
//
// This is the spatial awareness layer. It does NOT directly call DialogueHandler or
// GameInterface — it fires GameEvents and other systems respond.
//
// How it works:
//   - All NPCInteractable, ArtifactInteractable, and EnvironmentalPuzzle components
//     register themselves with WorldManager on Awake via Register().
//   - Each frame (or on player movement end), WorldManager checks distances.
//   - When player is within range of an interactable, it fires the appropriate GameEvent.
//   - When the player taps, PlayerMovement calls WorldManager.HandleTapAt(worldPos)
//     which routes to the nearest in-range interactable, or moves the player otherwise.

using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Reference to the player's Transform (the Manaog).")]
    public Transform playerTransform;

    // ── Registered Interactables ─────────────────────────────────────────────
    private List<NPCInteractable> _registeredNPCs           = new List<NPCInteractable>();
    private List<ArtifactInteractable> _registeredArtifacts = new List<ArtifactInteractable>();

    // ── State ────────────────────────────────────────────────────────────────
    private NPCInteractable _lastNPCInRange;
    private ArtifactInteractable _lastArtifactInRange;

    // ── Currently "active" interactable the player can tap ───────────────────
    public NPCInteractable NearestNPC => _lastNPCInRange;
    public ArtifactInteractable NearestArtifact => _lastArtifactInRange;
    public bool IsAnyInteractableInRange => _lastNPCInRange != null || _lastArtifactInRange != null;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        CheckProximity();
    }

    // ── Registration (called by interactable components on their Awake) ───────
    public void RegisterNPC(NPCInteractable npc)
    {
        if (!_registeredNPCs.Contains(npc)) _registeredNPCs.Add(npc);
    }

    public void UnregisterNPC(NPCInteractable npc)
    {
        _registeredNPCs.Remove(npc);
    }

    public void RegisterArtifact(ArtifactInteractable artifact)
    {
        if (!_registeredArtifacts.Contains(artifact)) _registeredArtifacts.Add(artifact);
    }

    public void UnregisterArtifact(ArtifactInteractable artifact)
    {
        _registeredArtifacts.Remove(artifact);
    }

    // ── Proximity Checking ───────────────────────────────────────────────────
    private void CheckProximity()
    {
        Vector2 playerPos = playerTransform.position;

        // ── NPC proximity ────────────────────────────────────────────────────
        NPCInteractable closestNPC = null;
        float closestNPCDist = float.MaxValue;

        foreach (var npc in _registeredNPCs)
        {
            float dist = Vector2.Distance(playerPos, npc.transform.position);
            if (dist <= npc.interactionRadius && dist < closestNPCDist)
            {
                closestNPCDist = dist;
                closestNPC = npc;
            }
        }

        if (closestNPC != _lastNPCInRange)
        {
            if (_lastNPCInRange != null)
                GameEvents.RaiseNPCExitRange(_lastNPCInRange);
            if (closestNPC != null)
                GameEvents.RaiseNPCEnterRange(closestNPC);
            _lastNPCInRange = closestNPC;
        }

        // ── Artifact proximity ───────────────────────────────────────────────
        ArtifactInteractable closestArtifact = null;
        float closestArtDist = float.MaxValue;

        foreach (var artifact in _registeredArtifacts)
        {
            float dist = Vector2.Distance(playerPos, artifact.transform.position);
            if (dist <= artifact.interactionRadius && dist < closestArtDist)
            {
                closestArtDist = dist;
                closestArtifact = artifact;
            }
        }

        if (closestArtifact != _lastArtifactInRange)
        {
            if (closestArtifact != null)
                GameEvents.RaiseArtifactEnterRange(closestArtifact);
            _lastArtifactInRange = closestArtifact;
        }
    }

    /// <summary>
    /// Called by PlayerMovement when the player taps the screen.
    /// If an interactable is in range at the tap position, trigger its interaction.
    /// Otherwise, treat the tap as a movement command.
    /// Returns true if the tap was routed to an interactable (no movement needed).
    /// </summary>
    public bool HandleTap(Vector2 worldPosition)
    {
        // Check if tap hit an NPC collider directly
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        if (hit != null)
        {
            NPCInteractable npc = hit.GetComponent<NPCInteractable>();
            if (npc != null && _lastNPCInRange == npc)
            {
                npc.Interact();
                return true;
            }

            ArtifactInteractable artifact = hit.GetComponent<ArtifactInteractable>();
            if (artifact != null && _lastArtifactInRange == artifact)
            {
                artifact.Examine();
                return true;
            }

            PuzzleMarker marker = hit.GetComponent<PuzzleMarker>();
            if (marker != null)
            {
                marker.OnTapped();
                return true;
            }
        }

        return false; // Not an interactable — caller should move player here
    }
}
