// NPCInteractable.cs — Component on any NPC GameObject in the demo scene.
//
// SETUP IN UNITY:
//   1. Add this component to each NPC GameObject (under the NPCs parent).
//   2. Add a CircleCollider2D (isTrigger = true) for tap detection.
//   3. Assign dialogueData in the Inspector.
//   4. Set interactionRadius to match the collider visually.
//   5. WorldManager auto-registers this NPC on Awake.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID for this NPC. Must match QuestData.questGiverNpcId if this NPC gives a quest.")]
    public string npcId;

    [Tooltip("Display name shown in the dialogue box header.")]
    public string npcName;

    [Header("Dialogue")]
    [Tooltip("The dialogue tree for this NPC. Assign a DialogueData ScriptableObject.")]
    public DialogueData dialogueData;

    [Header("Interaction")]
    [Tooltip("Distance in world units at which the player can trigger dialogue.")]
    public float interactionRadius = 1.5f;

    [Header("Quest (optional)")]
    [Tooltip("If this NPC gives a quest, assign the QuestData here.")]
    public QuestData questToOffer;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (WorldManager.Instance != null)
            WorldManager.Instance.RegisterNPC(this);
    }

    private void OnDestroy()
    {
        if (WorldManager.Instance != null)
            WorldManager.Instance.UnregisterNPC(this);
    }

    // ── Interaction ──────────────────────────────────────────────────────────
    /// <summary>
    /// Called by WorldManager when the player taps this NPC while in range.
    /// Routes to DialogueHandler to begin the dialogue sequence.
    /// </summary>
    public void Interact()
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName} has no DialogueData assigned.");
            return;
        }

        // If NPC has a quest and it hasn't been accepted, InteractionHandler will handle offer
        if (questToOffer != null && !ProgressTracker.Instance.IsQuestComplete())
        {
            InteractionHandler.Instance.CheckQuestOffer(this);
        }

        // Start dialogue via DialogueHandler (the main path)
        DialogueHandler.Instance.StartDialogue(dialogueData);
    }

    // ── Gizmos (visible in Scene view for level design reference) ────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
