// InteractionHandler.cs — Manages the quest lifecycle and environmental puzzle check.
//
// Responsibilities:
//   - Check if an NPC should offer a quest (and offer it via GameInterface)
//   - Track which step of the active quest the player is on
//   - On quest completion attempt: verify objectives with ProgressTracker
//     → if complete: mark finished, fire event
//     → if incomplete: show gentle nudge via GameInterface — NO fail state, no punishment
//   - Listen for puzzle completion events
//
// Quest flow:
//   NPCInteractable.Interact()
//     → InteractionHandler.CheckQuestOffer()     (show "Accept Quest?" UI)
//     → Player accepts → InteractionHandler.AcceptQuest()
//     → Player performs objectives (explore, talk, examine)
//     → DialogueHandler fires questStepIdToComplete events
//     → InteractionHandler.OnObjectiveCompleted() → checks against QuestData
//     → All steps done → InteractionHandler.CompleteQuest()

using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    public static InteractionHandler Instance { get; private set; }

    [Header("Quest")]
    [Tooltip("The demo's quest data. Assign in the Inspector.")]
    public QuestData demoQuestData;

    // ── State ────────────────────────────────────────────────────────────────
    private bool _questActive = false;
    private int _currentStepIndex = 0;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameEvents.OnQuestObjectiveCompleted += OnObjectiveCompleted;
        GameEvents.OnPuzzleCompleted         += OnPuzzleCompleted;

        // Restore quest state from save
        RestoreQuestState();
    }

    private void OnDestroy()
    {
        GameEvents.OnQuestObjectiveCompleted -= OnObjectiveCompleted;
        GameEvents.OnPuzzleCompleted         -= OnPuzzleCompleted;
    }

    // ── Quest Offer ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called by NPCInteractable when the NPC has a quest to offer.
    /// Shows a quest offer panel via GameInterface if quest is not yet active.
    /// </summary>
    public void CheckQuestOffer(NPCInteractable npc)
    {
        if (demoQuestData == null || _questActive) return;

        // Only offer if this NPC is the designated quest giver
        if (npc.npcId != demoQuestData.questGiverNpcId) return;

        // Don't offer if quest was already completed in a prior session
        if (ProgressTracker.Instance.IsQuestComplete()) return;

        GameInterface.Instance.ShowQuestOffer(demoQuestData);
    }

    /// <summary>
    /// Called by GameInterface "Accept" button on the quest offer panel.
    /// </summary>
    public void AcceptQuest()
    {
        if (demoQuestData == null) return;

        _questActive = true;
        _currentStepIndex = 0;

        GameEvents.RaiseQuestAccepted(demoQuestData);
        GameInterface.Instance.HideQuestOffer();
        GameInterface.Instance.ShowQuestObjective(demoQuestData.steps[_currentStepIndex].objective);

        Debug.Log($"[InteractionHandler] Quest accepted: {demoQuestData.title}");
    }

    // ── Quest Progress ────────────────────────────────────────────────────────

    private void OnObjectiveCompleted(string stepId)
    {
        if (!_questActive || demoQuestData == null) return;
        if (_currentStepIndex >= demoQuestData.steps.Count) return;

        QuestStep currentStep = demoQuestData.steps[_currentStepIndex];

        if (currentStep.stepId != stepId) return; // Not the current step

        _currentStepIndex++;

        if (_currentStepIndex >= demoQuestData.steps.Count)
        {
            // All steps done — complete the quest
            CompleteQuest();
        }
        else
        {
            // Show next objective
            GameInterface.Instance.ShowQuestObjective(demoQuestData.steps[_currentStepIndex].objective);
            Debug.Log($"[InteractionHandler] Next objective: {demoQuestData.steps[_currentStepIndex].objective}");
        }
    }

    private void CompleteQuest()
    {
        _questActive = false;
        GameEvents.RaiseQuestCompleted(demoQuestData);
        GameInterface.Instance.ShowQuestComplete(demoQuestData);
        Debug.Log($"[InteractionHandler] Quest complete: {demoQuestData.title}");

        // Check if demo is fully done (quest + puzzle both complete)
        CheckDemoComplete();
    }

    // ── Quest State Restore ──────────────────────────────────────────────────
    private void RestoreQuestState()
    {
        if (ProgressTracker.Instance == null || demoQuestData == null) return;

        int savedStep = ProgressTracker.Instance.GetCompletedStepIndex();
        bool questDone = ProgressTracker.Instance.IsQuestComplete();

        if (questDone)
        {
            _questActive = false;
            _currentStepIndex = demoQuestData.steps.Count;
        }
        else if (savedStep >= 0)
        {
            _questActive = true;
            _currentStepIndex = savedStep + 1;
            if (_currentStepIndex < demoQuestData.steps.Count)
                GameInterface.Instance.ShowQuestObjective(demoQuestData.steps[_currentStepIndex].objective);
        }
    }

    // ── Puzzle Completion ────────────────────────────────────────────────────
    private void OnPuzzleCompleted(string puzzleId)
    {
        Debug.Log($"[InteractionHandler] Puzzle completed: {puzzleId}");
        CheckDemoComplete();
    }

    // ── Demo Completion Check ────────────────────────────────────────────────
    /// <summary>
    /// The demo is "complete" when the quest is done AND the puzzle is done.
    /// Fires OnDemoSceneComplete → GameInterface shows the Summary screen.
    /// </summary>
    private void CheckDemoComplete()
    {
        bool questDone = ProgressTracker.Instance != null && ProgressTracker.Instance.IsQuestComplete();

        // For demo: quest completion alone is sufficient to trigger summary
        // Adjust this logic if you want puzzle completion to also be required
        if (questDone)
        {
            GameEvents.RaiseDemoSceneComplete();
        }
    }
}
