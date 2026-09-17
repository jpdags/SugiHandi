using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "SugiHandi/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [Serializable]
    public class DialogueTriggerEntry
    {
        [Tooltip("The unique ID of the trigger or NPC in the scene (e.g. 'Elder_Start', 'Fisherman_Quest1')")]
        public string triggerId;
        
        [Tooltip("The dialogue data to load when this trigger is activated")]
        public DialogueData dialogueData;
    }

    [Tooltip("List of all dialogues and their triggers in the game.")]
    public List<DialogueTriggerEntry> dialogueEntries = new List<DialogueTriggerEntry>();

    /// <summary>
    /// Retrieves the DialogueData associated with a specific trigger ID.
    /// </summary>
    public DialogueData GetDialogue(string triggerId)
    {
        var entry = dialogueEntries.Find(e => e.triggerId == triggerId);
        if (entry != null)
        {
            return entry.dialogueData;
        }
        
        Debug.LogWarning($"DialogueDatabase: No dialogue found for trigger ID '{triggerId}'");
        return null;
    }
}
