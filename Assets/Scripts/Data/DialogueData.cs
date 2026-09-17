// DialogueData.cs — ScriptableObject defining a full NPC dialogue tree.
//
// HOW BUTUANON WORDS ARE EMBEDDED:
//   Wrap Butuanon words in the dialogue text with curly braces: {manaog}
//   Example: "The {Gabok} changed everything — you know this better than I do."
//   DialogueHandler will parse these tags, strip the braces for display,
//   and fire GameEvents.RaiseBuruanonWordEncountered(wordKey) for each one.
//   The word is shown naturally in the dialogue box — no translation ever appears.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Dialogue Data

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueOption
{
    [Tooltip("What the player can say or choose to do. Shown as a button in the dialogue UI.")]
    public string optionText;

    [Tooltip("NPC's response to this choice. Embed Butuanon words as {wordKey}.")]
    [TextArea(3, 6)]
    public string npcResponse;

    [Tooltip("ID of the next DialogueNode to load. Leave empty to end dialogue.")]
    public string nextNodeId;

    [Tooltip("What narrative consequence this choice has (dev-reference only, not shown to player).")]
    public string narrativeConsequence;

    [Tooltip("Quest step ID to mark complete when this option is chosen. Leave empty for none.")]
    public string questStepIdToComplete;
}

[Serializable]
public class DialogueNode
{
    [Tooltip("Unique ID for this node. Used as the 'nextNodeId' target in options.")]
    public string nodeId;

    [Tooltip("NPC's opening line. Embed Butuanon words as {wordKey}.")]
    [TextArea(3, 6)]
    public string npcText;

    [Tooltip("2-3 player response options. Always provide at least 2.")]
    public List<DialogueOption> options = new List<DialogueOption>();

    [Tooltip("If true, dialogue ends after this node's NPC text is shown (no options presented).")]
    public bool isEndNode = false;
}

[CreateAssetMenu(fileName = "Dialogue_NPC_Name", menuName = "SugiHandi/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("ID of the first node to display when dialogue starts.")]
    public string startNodeId = "node_0";

    [Tooltip("Name of the NPC this dialogue belongs to. Used for display in the dialogue box header.")]
    public string npcName;

    [Tooltip("All dialogue nodes in this tree.")]
    public List<DialogueNode> nodes = new List<DialogueNode>();

    /// <summary>Find a node by its ID. Returns null if not found.</summary>
    public DialogueNode GetNode(string nodeId)
    {
        return nodes.Find(n => n.nodeId == nodeId);
    }

    /// <summary>Returns the starting node.</summary>
    public DialogueNode GetStartNode()
    {
        return GetNode(startNodeId);
    }
}
