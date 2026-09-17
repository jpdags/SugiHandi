// QuestData.cs — ScriptableObject defining the demo's quest chain.
//
// The quest flow: accept → step 1 → step 2 → ... → complete → reward shown
// InteractionHandler drives progress. ProgressTracker saves state to disk.
// No fail state. If objectives are incomplete, GameInterface shows a nudge — no punishment.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Quest Data

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestStep
{
    [Tooltip("Unique ID for this step. Used to match GameEvents.RaiseQuestObjectiveCompleted(stepId).")]
    public string stepId;

    [Tooltip("What the player needs to do (shown to player via GameInterface).")]
    public string objective;

    [Tooltip("Description of what triggers this step's completion (dev reference — not shown to player).")]
    public string completionTriggerNote;

    [Tooltip("What an NPC says (or what text appears) when this step is completed.")]
    [TextArea(2, 4)]
    public string dialogueOnCompletion;
}

[CreateAssetMenu(fileName = "Quest_QuestName", menuName = "SugiHandi/Quest Data")]
public class QuestData : ScriptableObject
{
    [Tooltip("Unique identifier for this quest. Saved to disk.")]
    public string questId;

    [Tooltip("Display title shown in the UI when quest is active.")]
    public string title;

    [Tooltip("NPC ID of the character who gives this quest.")]
    public string questGiverNpcId;

    [Tooltip("Short synopsis shown when quest is accepted (2-3 sentences).")]
    [TextArea(3, 5)]
    public string synopsis;

    [Tooltip("Sequential quest steps. Player must complete them in order.")]
    public List<QuestStep> steps = new List<QuestStep>();

    [Tooltip("Narrative/relational reward shown on quest completion. " +
             "No coins, no XP — this is not a trading game.")]
    [TextArea(2, 4)]
    public string reward;

    [Tooltip("Dialogue text from the quest giver NPC when the quest is complete.")]
    [TextArea(2, 4)]
    public string completionDialogue;
}
