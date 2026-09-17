// ProgressTracker.cs — Silently tracks all player progress during the demo session.
//
// This is the "silent bookkeeper" of SugiHandi.
// It listens to GameEvents, updates the in-memory SaveData, and persists via SaveManager.
// It NEVER shows anything to the player directly — that's GameInterface's job.
//
// What it tracks:
//   - Butuanon words encountered (Kodeks entries)
//   - Quest step completion and quest state
//   - Artifact examination history
//   - Puzzle completion
//
// IMPORTANT: Kodeks entries are logged HERE, silently.
// They are only shown to the player via GameInterface on the Summary Screen — never mid-scene.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProgressTracker : MonoBehaviour
{
    public static ProgressTracker Instance { get; private set; }

    // ── In-Memory State ──────────────────────────────────────────────────────
    private SaveData _saveData;

    // Runtime Kodeks entry references (loaded from Resources as words are encountered)
    private List<KodeksEntryData> _loggedKodeksEntries = new List<KodeksEntryData>();

    // Expose for GameInterface to read when showing the Summary/Kodeks panel
    public IReadOnlyList<KodeksEntryData> LoggedKodeksEntries => _loggedKodeksEntries;
    public SaveData CurrentSaveData => _saveData;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadSave();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ── Event Subscriptions ──────────────────────────────────────────────────
    private void SubscribeToEvents()
    {
        GameEvents.OnBuruanonWordEncountered += HandleWordEncountered;
        GameEvents.OnQuestAccepted           += HandleQuestAccepted;
        GameEvents.OnQuestObjectiveCompleted += HandleQuestObjectiveCompleted;
        GameEvents.OnQuestCompleted          += HandleQuestCompleted;
        GameEvents.OnArtifactExamined        += HandleArtifactExamined;
        GameEvents.OnPuzzleCompleted         += HandlePuzzleCompleted;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnBuruanonWordEncountered -= HandleWordEncountered;
        GameEvents.OnQuestAccepted           -= HandleQuestAccepted;
        GameEvents.OnQuestObjectiveCompleted -= HandleQuestObjectiveCompleted;
        GameEvents.OnQuestCompleted          -= HandleQuestCompleted;
        GameEvents.OnArtifactExamined        -= HandleArtifactExamined;
        GameEvents.OnPuzzleCompleted         -= HandlePuzzleCompleted;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    /// <summary>Called when a Butuanon word is encountered in dialogue.
    /// Finds the matching KodeksEntryData, logs it silently, then saves.</summary>
    private void HandleWordEncountered(string wordKey)
    {
        if (_saveData.loggedKodeksWordKeys.Contains(wordKey)) return; // already logged

        // Load the KodeksEntryData ScriptableObject from Resources
        // Convention: all KodeksEntryData assets live in Resources/Kodeks/
        KodeksEntryData entry = Resources.Load<KodeksEntryData>($"Kodeks/{wordKey}");
        if (entry == null)
        {
            Debug.LogWarning($"[ProgressTracker] No KodeksEntryData found for wordKey: '{wordKey}' — check Resources/Kodeks/");
            return;
        }

        _saveData.loggedKodeksWordKeys.Add(wordKey);
        _loggedKodeksEntries.Add(entry);

        // Notify any listeners (e.g. a small visual indicator — but NOT the full Kodeks panel)
        GameEvents.RaiseKodeksEntryLogged(entry);

        PersistSave();
        Debug.Log($"[ProgressTracker] Kodeks entry logged: {wordKey}");
    }

    private void HandleQuestAccepted(QuestData quest)
    {
        _saveData.activeQuestId = quest.questId;
        _saveData.completedQuestStepIndex = -1;
        _saveData.questCompleted = false;
        PersistSave();
    }

    private void HandleQuestObjectiveCompleted(string stepId)
    {
        // Find the step index in the current quest and advance the pointer
        // InteractionHandler is responsible for the actual step lookup;
        // ProgressTracker just increments and saves.
        _saveData.completedQuestStepIndex++;
        PersistSave();
        Debug.Log($"[ProgressTracker] Quest step completed: {stepId} (index: {_saveData.completedQuestStepIndex})");
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        _saveData.questCompleted = true;
        PersistSave();
        Debug.Log($"[ProgressTracker] Quest complete: {quest.questId}");
    }

    private void HandleArtifactExamined(ArtifactData artifact)
    {
        if (!_saveData.examinedArtifactIds.Contains(artifact.artifactId))
        {
            _saveData.examinedArtifactIds.Add(artifact.artifactId);

            // Also log the associated Kodeks entry if present
            if (!string.IsNullOrEmpty(artifact.kodeksWordKey))
            {
                HandleWordEncountered(artifact.kodeksWordKey);
            }

            PersistSave();
        }
    }

    private void HandlePuzzleCompleted(string puzzleId)
    {
        if (!_saveData.completedPuzzleIds.Contains(puzzleId))
        {
            _saveData.completedPuzzleIds.Add(puzzleId);
            PersistSave();
            Debug.Log($"[ProgressTracker] Puzzle completed: {puzzleId}");
        }
    }

    // ── Save / Load ──────────────────────────────────────────────────────────
    private void LoadSave()
    {
        _saveData = SaveManager.Instance.Load();

        // Rebuild in-memory KodeksEntryData list from saved word keys
        foreach (string wordKey in _saveData.loggedKodeksWordKeys)
        {
            KodeksEntryData entry = Resources.Load<KodeksEntryData>($"Kodeks/{wordKey}");
            if (entry != null) _loggedKodeksEntries.Add(entry);
        }
    }

    private void PersistSave()
    {
        SaveManager.Instance.Save(_saveData);
    }

    // ── Public Query Methods ─────────────────────────────────────────────────
    public bool HasExaminedArtifact(string artifactId)   => _saveData.examinedArtifactIds.Contains(artifactId);
    public bool HasCompletedPuzzle(string puzzleId)       => _saveData.completedPuzzleIds.Contains(puzzleId);
    public bool HasLoggedWord(string wordKey)             => _saveData.loggedKodeksWordKeys.Contains(wordKey);
    public bool IsQuestComplete()                         => _saveData.questCompleted;
    public int  GetCompletedStepIndex()                   => _saveData.completedQuestStepIndex;
}
