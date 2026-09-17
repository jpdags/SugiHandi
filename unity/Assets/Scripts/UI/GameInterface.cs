// GameInterface.cs — The UI controller for all SugiHandi panels.
//
// Manages:
//   - Dialogue box (NPC text + player option buttons)
//   - Quest offer panel
//   - Quest objective HUD element
//   - Artifact examine panel
//   - Kodeks panel (shown ONLY on Summary screen, never mid-gameplay)
//   - Summary screen
//
// All show/hide transitions use Unity Coroutines on CanvasGroup.alpha.
// NEVER show the Kodeks panel during gameplay — only on Summary.
//
// SETUP IN UNITY:
//   1. Create a UI Canvas (Screen Space - Camera, using main camera).
//   2. Create child panels as listed below and assign them in the Inspector.
//   3. Add CanvasGroup to each panel (for alpha fades).
//   4. Add this component to the Canvas or a persistent manager GameObject.
//
// REQUIRED PACKAGES: TextMeshPro

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameInterface : MonoBehaviour
{
    public static GameInterface Instance { get; private set; }

    private const float PANEL_FADE_DURATION = 0.25f;

    // ── Panel References ─────────────────────────────────────────────────────
    [Header("Dialogue Box")]
    public CanvasGroup dialoguePanelGroup;
    public TextMeshProUGUI dialogueNpcNameText;
    public TextMeshProUGUI dialogueNpcBodyText;
    public Transform dialogueOptionsContainer;  // Parent of option buttons
    public Button dialogueOptionButtonPrefab;   // Prefab with a TextMeshProUGUI child
    public Button dialogueCloseButton;          // Shown only on end nodes

    [Header("Quest Offer Panel")]
    public CanvasGroup questOfferPanelGroup;
    public TextMeshProUGUI questOfferTitleText;
    public TextMeshProUGUI questOfferSynopsisText;
    public Button questAcceptButton;

    [Header("Quest Objective HUD")]
    public CanvasGroup questObjectiveHudGroup;
    public TextMeshProUGUI questObjectiveText;

    [Header("Artifact Examine Panel")]
    public CanvasGroup artifactExaminePanelGroup;
    public Image artifactImage;
    public TextMeshProUGUI artifactNameText;
    public TextMeshProUGUI artifactInscriptionText;  // Butuanon inscription — no translation shown
    public TextMeshProUGUI artifactHistoricalNoteText;
    public Button artifactCloseButton;

    [Header("Summary Screen")]
    public CanvasGroup summaryScreenGroup;
    public TextMeshProUGUI summaryTitleText;
    public Transform kodeksEntriesContainer;    // Parent for Kodeks entry UI rows
    public GameObject kodeksEntryRowPrefab;     // Prefab: word label + context label
    public Button summaryReturnMenuButton;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Hide all panels at start
        HideAll(instant: true);
    }

    private void Start()
    {
        // Wire close buttons
        if (dialogueCloseButton != null)
            dialogueCloseButton.onClick.AddListener(() => DialogueHandler.Instance.EndDialogue());

        if (artifactCloseButton != null)
            artifactCloseButton.onClick.AddListener(HideArtifactExamine);

        if (questAcceptButton != null)
            questAcceptButton.onClick.AddListener(() => InteractionHandler.Instance.AcceptQuest());

        if (summaryReturnMenuButton != null)
            summaryReturnMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Listen for demo complete event
        GameEvents.OnDemoSceneComplete += ShowSummaryScreen;
    }

    private void OnDestroy()
    {
        GameEvents.OnDemoSceneComplete -= ShowSummaryScreen;
    }

    // ── Dialogue Box ─────────────────────────────────────────────────────────

    /// <summary>Show dialogue node: NPC text + option buttons.</summary>
    public void ShowDialogueNode(string npcName, string npcText, List<DialogueOption> options)
    {
        dialogueNpcNameText.text = npcName;
        dialogueNpcBodyText.text = npcText;

        // Clear existing option buttons
        foreach (Transform child in dialogueOptionsContainer)
            Destroy(child.gameObject);

        // Create a button for each option (2–3)
        for (int i = 0; i < options.Count; i++)
        {
            int optionIndex = i; // Capture for lambda
            Button btn = Instantiate(dialogueOptionButtonPrefab, dialogueOptionsContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = options[i].optionText;
            btn.onClick.AddListener(() => DialogueHandler.Instance.SelectOption(optionIndex));
        }

        if (dialogueCloseButton != null) dialogueCloseButton.gameObject.SetActive(false);
        ShowPanel(dialoguePanelGroup);
    }

    /// <summary>Show NPC's response text (after player selects an option).</summary>
    public void ShowNPCResponse(string responseText)
    {
        dialogueNpcBodyText.text = responseText;

        // Clear option buttons while showing response
        foreach (Transform child in dialogueOptionsContainer)
            Destroy(child.gameObject);
    }

    /// <summary>Show a terminal dialogue node (NPC text, no options).</summary>
    public void ShowEndNode(string npcName, string npcText)
    {
        dialogueNpcNameText.text = npcName;
        dialogueNpcBodyText.text = npcText;

        foreach (Transform child in dialogueOptionsContainer)
            Destroy(child.gameObject);

        ShowDialogueEndButton();
        ShowPanel(dialoguePanelGroup);
    }

    /// <summary>Show the close/continue button after response or end node.</summary>
    public void ShowDialogueEndButton()
    {
        if (dialogueCloseButton != null) dialogueCloseButton.gameObject.SetActive(true);
    }

    public void HideDialogueBox()
    {
        HidePanel(dialoguePanelGroup);
    }

    // ── Quest Offer ──────────────────────────────────────────────────────────

    public void ShowQuestOffer(QuestData quest)
    {
        questOfferTitleText.text = quest.title;
        questOfferSynopsisText.text = quest.synopsis;
        ShowPanel(questOfferPanelGroup);
    }

    public void HideQuestOffer()
    {
        HidePanel(questOfferPanelGroup);
    }

    // ── Quest Objective HUD ───────────────────────────────────────────────────

    public void ShowQuestObjective(string objectiveText)
    {
        questObjectiveText.text = objectiveText;
        ShowPanel(questObjectiveHudGroup);
    }

    public void ShowQuestComplete(QuestData quest)
    {
        // Briefly show completion dialogue, then auto-hide after 3 seconds
        questObjectiveText.text = quest.completionDialogue;
        StartCoroutine(DelayedHide(questObjectiveHudGroup, 3f));
    }

    // ── Artifact Examine ─────────────────────────────────────────────────────

    public void ShowArtifactExamine(ArtifactData artifact)
    {
        if (artifactImage != null && artifact.artifactSprite != null)
            artifactImage.sprite = artifact.artifactSprite;

        artifactNameText.text = artifact.artifactName;

        // Show inscription as-is — no translation
        artifactInscriptionText.text = artifact.butuanonInscription;

        // Historical note is cultural context, not a translation
        artifactHistoricalNoteText.text = artifact.historicalNote;

        ShowPanel(artifactExaminePanelGroup);
    }

    public void HideArtifactExamine()
    {
        HidePanel(artifactExaminePanelGroup);
    }

    // ── Summary Screen ────────────────────────────────────────────────────────
    // Called by GameEvents.OnDemoSceneComplete
    // This is where the Kodeks panel content is shown — ONLY here, never mid-scene.

    public void ShowSummaryScreen()
    {
        summaryTitleText.text = "Kodeks — Words You Encountered";

        // Populate Kodeks entries from ProgressTracker
        if (kodeksEntriesContainer != null && kodeksEntryRowPrefab != null)
        {
            foreach (Transform child in kodeksEntriesContainer)
                Destroy(child.gameObject);

            var entries = ProgressTracker.Instance.LoggedKodeksEntries;
            foreach (var entry in entries)
            {
                GameObject row = Instantiate(kodeksEntryRowPrefab, kodeksEntriesContainer);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

                // Convention: first TMP = word, second TMP = scene context
                if (texts.Length >= 2)
                {
                    texts[0].text = entry.displayWord;
                    texts[1].text = entry.sceneContext; // Context, not a translation
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{entry.displayWord} — {entry.sceneContext}";
                }
            }
        }

        // Hide gameplay UI, show summary
        HidePanel(questObjectiveHudGroup, instant: true);
        ShowPanel(summaryScreenGroup);
    }

    // ── Scene Navigation ─────────────────────────────────────────────────────

    private void ReturnToMainMenu()
    {
        // Fade out summary screen, then load main menu scene
        StartCoroutine(FadeOutThenLoad(summaryScreenGroup, 0.4f, "MainMenu"));
    }

    // ── Panel Show/Hide Helpers (Coroutine-based) ─────────────────────────────

    private void ShowPanel(CanvasGroup group, bool instant = false)
    {
        if (group == null) return;
        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        group.interactable = true;

        if (instant) { group.alpha = 1f; return; }
        StartCoroutine(FadeCanvasGroup(group, 0f, 1f, PANEL_FADE_DURATION));
    }

    private void HidePanel(CanvasGroup group, bool instant = false)
    {
        if (group == null) return;
        group.blocksRaycasts = false;
        group.interactable = false;

        if (instant) { group.alpha = 0f; group.gameObject.SetActive(false); return; }
        StartCoroutine(FadeOutAndDeactivate(group, PANEL_FADE_DURATION));
    }

    private void HideAll(bool instant = false)
    {
        HidePanel(dialoguePanelGroup, instant);
        HidePanel(questOfferPanelGroup, instant);
        HidePanel(questObjectiveHudGroup, instant);
        HidePanel(artifactExaminePanelGroup, instant);
        HidePanel(summaryScreenGroup, instant);
    }

    // ── Coroutine Utilities ───────────────────────────────────────────────────

    /// <summary>Smoothly fade a CanvasGroup alpha from startAlpha to endAlpha over duration seconds.</summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        group.alpha = startAlpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        group.alpha = endAlpha;
    }

    /// <summary>Fade out a CanvasGroup then deactivate its GameObject.</summary>
    private IEnumerator FadeOutAndDeactivate(CanvasGroup group, float duration)
    {
        yield return FadeCanvasGroup(group, group.alpha, 0f, duration);
        group.gameObject.SetActive(false);
    }

    /// <summary>Wait for a delay then hide a panel.</summary>
    private IEnumerator DelayedHide(CanvasGroup group, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HidePanel(group);
    }

    /// <summary>Fade out a CanvasGroup then load a scene by name.</summary>
    private IEnumerator FadeOutThenLoad(CanvasGroup group, float duration, string sceneName)
    {
        yield return FadeCanvasGroup(group, group.alpha, 0f, duration);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
