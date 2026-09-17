// GameEvents.cs — Centralized C# event bus for all SugiHandi systems.
// Systems communicate via this static class only — no direct cross-references between managers.
// Pattern: fire-and-forget events. Any system can raise; any system can listen.

using System;

public static class GameEvents
{
    // ── WorldManager / Proximity ─────────────────────────────────────────────
    /// <summary>Fired when the player enters interaction range of an NPC.</summary>
    public static event Action<NPCInteractable> OnNPCEnterRange;
    /// <summary>Fired when the player leaves interaction range of an NPC.</summary>
    public static event Action<NPCInteractable> OnNPCExitRange;
    /// <summary>Fired when the player enters interaction range of an artifact.</summary>
    public static event Action<ArtifactInteractable> OnArtifactEnterRange;

    // ── Dialogue ─────────────────────────────────────────────────────────────
    /// <summary>Fired by DialogueHandler when a dialogue sequence begins.</summary>
    public static event Action<DialogueData> OnDialogueStart;
    /// <summary>Fired by DialogueHandler when a dialogue sequence ends.</summary>
    public static event Action OnDialogueEnd;
    /// <summary>Fired when a Butuanon word key is encountered in dialogue text.
    /// ProgressTracker listens to this to silently log Kodeks entries.</summary>
    public static event Action<string> OnBuruanonWordEncountered; // wordKey e.g. "manaog"

    // ── Quest ────────────────────────────────────────────────────────────────
    /// <summary>Fired by InteractionHandler when the player accepts a quest.</summary>
    public static event Action<QuestData> OnQuestAccepted;
    /// <summary>Fired by InteractionHandler when a quest step objective is completed.</summary>
    public static event Action<string> OnQuestObjectiveCompleted; // stepId
    /// <summary>Fired by InteractionHandler when all quest steps are verified complete.</summary>
    public static event Action<QuestData> OnQuestCompleted;

    // ── Artifact ─────────────────────────────────────────────────────────────
    /// <summary>Fired by ArtifactInteractable when the player examines an artifact.</summary>
    public static event Action<ArtifactData> OnArtifactExamined;

    // ── Environmental Puzzle ─────────────────────────────────────────────────
    /// <summary>Fired by EnvironmentalPuzzle when the player completes the tap sequence.</summary>
    public static event Action<string> OnPuzzleCompleted; // puzzleId

    // ── Kodeks ───────────────────────────────────────────────────────────────
    /// <summary>Fired by ProgressTracker when a new Kodeks entry is silently logged.
    /// GameInterface listens to this to populate the Kodeks panel (shown post-scene).</summary>
    public static event Action<KodeksEntryData> OnKodeksEntryLogged;

    // ── Scene Flow ───────────────────────────────────────────────────────────
    /// <summary>Fired when the demo scene's primary content is complete.
    /// GameInterface listens to show the Summary screen.</summary>
    public static event Action OnDemoSceneComplete;

    // ── Raise Methods ────────────────────────────────────────────────────────
    public static void RaiseNPCEnterRange(NPCInteractable npc)           => OnNPCEnterRange?.Invoke(npc);
    public static void RaiseNPCExitRange(NPCInteractable npc)            => OnNPCExitRange?.Invoke(npc);
    public static void RaiseArtifactEnterRange(ArtifactInteractable a)   => OnArtifactEnterRange?.Invoke(a);
    public static void RaiseDialogueStart(DialogueData data)             => OnDialogueStart?.Invoke(data);
    public static void RaiseDialogueEnd()                                => OnDialogueEnd?.Invoke();
    public static void RaiseBuruanonWordEncountered(string wordKey)      => OnBuruanonWordEncountered?.Invoke(wordKey);
    public static void RaiseQuestAccepted(QuestData quest)               => OnQuestAccepted?.Invoke(quest);
    public static void RaiseQuestObjectiveCompleted(string stepId)       => OnQuestObjectiveCompleted?.Invoke(stepId);
    public static void RaiseQuestCompleted(QuestData quest)              => OnQuestCompleted?.Invoke(quest);
    public static void RaiseArtifactExamined(ArtifactData artifact)      => OnArtifactExamined?.Invoke(artifact);
    public static void RaisePuzzleCompleted(string puzzleId)             => OnPuzzleCompleted?.Invoke(puzzleId);
    public static void RaiseKodeksEntryLogged(KodeksEntryData entry)     => OnKodeksEntryLogged?.Invoke(entry);
    public static void RaiseDemoSceneComplete()                          => OnDemoSceneComplete?.Invoke();
}
