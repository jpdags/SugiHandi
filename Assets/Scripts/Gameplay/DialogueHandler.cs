// DialogueHandler.cs — Manages the active dialogue session for SugiHandi.
//
// Responsibilities:
//   - Load a DialogueData and track which node the player is currently on
//   - Parse {wordKey} tags in dialogue text and fire Butuanon word events
//   - Tell GameInterface what to display (text + options)
//   - Advance the dialogue tree when the player selects an option
//
// HARD RULES enforced here:
//   - No translation is ever passed to GameInterface
//   - {wordKey} tags are stripped before display — the word shows naturally in text
//   - Butuanon word events are fired silently to ProgressTracker via GameEvents

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }

    // ── State ────────────────────────────────────────────────────────────────
    private DialogueData _currentDialogue;
    private DialogueNode _currentNode;
    public bool IsDialogueActive => _currentDialogue != null;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Begin a dialogue session with the given DialogueData.
    /// Called by NPCInteractable.Interact().
    /// </summary>
    public void StartDialogue(DialogueData data)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("[DialogueHandler] Tried to start dialogue while one is already active.");
            return;
        }

        _currentDialogue = data;
        _currentNode = data.GetStartNode();

        if (_currentNode == null)
        {
            Debug.LogError($"[DialogueHandler] Start node '{data.startNodeId}' not found in {data.name}");
            return;
        }

        GameEvents.RaiseDialogueStart(data);
        DisplayCurrentNode();
    }

    /// <summary>
    /// Called by GameInterface when the player taps a dialogue option button.
    /// optionIndex corresponds to the index in _currentNode.options.
    /// </summary>
    public void SelectOption(int optionIndex)
    {
        if (!IsDialogueActive || _currentNode == null) return;

        if (optionIndex < 0 || optionIndex >= _currentNode.options.Count)
        {
            Debug.LogWarning($"[DialogueHandler] Invalid option index: {optionIndex}");
            return;
        }

        DialogueOption selectedOption = _currentNode.options[optionIndex];

        // Show NPC's response to this option
        string cleanResponse = ParseAndFireBuruanonTags(selectedOption.npcResponse);
        GameInterface.Instance.ShowNPCResponse(cleanResponse);

        // Fire quest step completion if this option triggers one
        if (!string.IsNullOrEmpty(selectedOption.questStepIdToComplete))
            GameEvents.RaiseQuestObjectiveCompleted(selectedOption.questStepIdToComplete);

        // Advance to next node or end dialogue
        if (!string.IsNullOrEmpty(selectedOption.nextNodeId))
        {
            DialogueNode nextNode = _currentDialogue.GetNode(selectedOption.nextNodeId);
            if (nextNode != null)
            {
                _currentNode = nextNode;
                DisplayCurrentNode();
            }
            else
            {
                Debug.LogWarning($"[DialogueHandler] Next node '{selectedOption.nextNodeId}' not found.");
                EndDialogue();
            }
        }
        else
        {
            // No next node specified — end after showing response
            // Small delay via GameInterface so player can read the response, then close
            GameInterface.Instance.ShowDialogueEndButton();
        }
    }

    /// <summary>
    /// End the current dialogue session. Called by GameInterface "Close" button
    /// or when dialogue tree reaches a terminal node.
    /// </summary>
    public void EndDialogue()
    {
        _currentDialogue = null;
        _currentNode = null;
        GameEvents.RaiseDialogueEnd();
        GameInterface.Instance.HideDialogueBox();
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private void DisplayCurrentNode()
    {
        if (_currentNode == null) { EndDialogue(); return; }

        string cleanNpcText = ParseAndFireBuruanonTags(_currentNode.npcText);

        if (_currentNode.isEndNode)
        {
            GameInterface.Instance.ShowEndNode(_currentDialogue.npcName, cleanNpcText);
            // End automatically after a beat
        }
        else
        {
            GameInterface.Instance.ShowDialogueNode(
                npcName: _currentDialogue.npcName,
                npcText: cleanNpcText,
                options: _currentNode.options
            );
        }
    }

    /// <summary>
    /// Finds all {wordKey} tags in the input text, fires a Butuanon word event for each,
    /// and returns the text with tags stripped (word is left in naturally).
    /// 
    /// Example: "The {Gabok} took everything."
    ///   → Fires: GameEvents.RaiseBuruanonWordEncountered("Gabok")
    ///   → Returns: "The Gabok took everything."  ← no translation, word shown naturally
    /// </summary>
    private string ParseAndFireBuruanonTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Match all {wordKey} patterns
        MatchCollection matches = Regex.Matches(text, @"\{(\w+)\}");
        foreach (Match match in matches)
        {
            string wordKey = match.Groups[1].Value;
            GameEvents.RaiseBuruanonWordEncountered(wordKey);
        }

        // Strip the braces — the word stays in the text naturally
        return Regex.Replace(text, @"\{(\w+)\}", "$1");
    }
}
