// MainMenuController.cs — Drives the SugiHandi main menu scene (Menu.unity).
//
// Responsibilities:
//   - Show/hide the Continue button based on whether a save file exists.
//   - Display the last-saved timestamp on the Continue button.
//   - New Game: show a confirmation panel before wiping the save.
//   - Settings: slide in/out an audio settings panel (Master / Music / SFX sliders).
//   - Quit: Application.Quit().
//
// Scene name conventions (must match Build Settings):
//   Main Menu scene  = "MainMenu"
//   Gameplay scene   = "SampleScene"
//
// SETUP IN UNITY:
//   1. Add this component to a GameObject in Menu.unity (e.g. "MainMenuController").
//   2. Ensure SaveManager is also present in the scene (it is DontDestroyOnLoad,
//      so it will persist if entered from gameplay, but the menu must bootstrap
//      it if launched directly from the editor).
//   3. Assign all serialized fields in the Inspector.
//   4. Add Menu.unity to Build Settings as "MainMenu", SampleScene as "SampleScene".

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    // -- Scene Names ----------------------------------------------------------
    private const string GAMEPLAY_SCENE = "SampleScene";

    // -- Fade Duration --------------------------------------------------------
    private const float FADE_DURATION = 0.25f;

    // -- Main Buttons ---------------------------------------------------------
    [Header("Main Buttons")]
    [Tooltip("Shown only when a save file exists.")]
    public Button continueButton;

    [Tooltip("Sub-label on the Continue button showing 'Last played: ...'.")]
    public TextMeshProUGUI continueSubLabel;

    public Button newGameButton;
    public Button settingsButton;
    public Button quitButton;

    // -- Confirm New Game Panel -----------------------------------------------
    [Header("Confirm New Game Panel")]
    public CanvasGroup confirmPanelGroup;
    public Button confirmYesButton;
    public Button confirmNoButton;

    // -- Settings Panel -------------------------------------------------------
    [Header("Settings Panel")]
    public CanvasGroup settingsPanelGroup;
    public Button settingsCloseButton;

    [Header("Audio Mixer (Settings)")]
    [Tooltip("The AudioMixer exposing 'MasterVolume', 'MusicVolume', 'SFXVolume' parameters.")]
    public AudioMixer audioMixer;

    [Tooltip("Slider controlling Master volume (0-1, mapped to dB).")]
    public Slider masterVolumeSlider;

    [Tooltip("Slider controlling Music volume (0-1, mapped to dB).")]
    public Slider musicVolumeSlider;

    [Tooltip("Slider controlling SFX volume (0-1, mapped to dB).")]
    public Slider sfxVolumeSlider;

    // -- Screen Fade Overlay --------------------------------------------------
    [Header("Screen Fade")]
    [Tooltip("Full-screen black CanvasGroup used for fade-to-black transitions.")]
    public CanvasGroup screenFadeGroup;

    // -- AudioMixer Parameter Names -------------------------------------------
    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_MUSIC  = "MusicVolume";
    private const string PARAM_SFX    = "SFXVolume";

    // -- PlayerPrefs Keys (persisting audio settings) -------------------------
    private const string PREF_MASTER = "vol_master";
    private const string PREF_MUSIC  = "vol_music";
    private const string PREF_SFX    = "vol_sfx";

    // =========================================================================
    // Lifecycle
    // =========================================================================

    private void Awake()
    {
        // Bootstrap SaveManager if the editor launches this scene directly
        // and SaveManager hasn't been instantiated by a prior scene yet.
        if (SaveManager.Instance == null)
        {
            GameObject managerGO = new GameObject("SaveManager");
            managerGO.AddComponent<SaveManager>();
            Debug.Log("[MainMenuController] Bootstrapped SaveManager for direct-launch in editor.");
        }

        // Hide all overlay panels immediately (no fade at startup)
        SetGroupVisible(confirmPanelGroup,  visible: false, instant: true);
        SetGroupVisible(settingsPanelGroup, visible: false, instant: true);

        // Start with screen faded to black; we fade in during Start.
        if (screenFadeGroup != null)
        {
            screenFadeGroup.alpha = 1f;
            screenFadeGroup.blocksRaycasts = true;
        }
    }

    private void Start()
    {
        WireButtons();
        ConfigureSaveState();
        LoadAudioPreferences();

        // Fade the screen in from black.
        StartCoroutine(FadeGroup(screenFadeGroup, 1f, 0f, 0.6f, onComplete: () =>
        {
            if (screenFadeGroup != null) screenFadeGroup.blocksRaycasts = false;
        }));
    }

    // =========================================================================
    // Wiring
    // =========================================================================

    private void WireButtons()
    {
        if (continueButton      != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (newGameButton       != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (settingsButton      != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton          != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (confirmYesButton    != null) confirmYesButton.onClick.AddListener(OnNewGameConfirmed);
        if (confirmNoButton     != null) confirmNoButton.onClick.AddListener(OnNewGameCancelled);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(OnSettingsClose);

        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider  != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider    != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    // =========================================================================
    // Save State -> Button Visibility
    // =========================================================================

    private void ConfigureSaveState()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();

        if (continueButton != null)
            continueButton.gameObject.SetActive(hasSave);

        if (hasSave && continueSubLabel != null)
        {
            SaveData data     = SaveManager.Instance.Load();
            string timestamp  = FormatTimestamp(data.lastSavedAt);
            continueSubLabel.text = "Last played: " + timestamp;
        }

        Debug.Log("[MainMenuController] Save file found: " + hasSave);
    }

    /// <summary>Converts an ISO 8601 UTC timestamp to a friendly local-time string.</summary>
    private string FormatTimestamp(string iso8601)
    {
        if (string.IsNullOrEmpty(iso8601)) return "—";
        try
        {
            DateTime utc   = DateTime.Parse(iso8601, null,
                                System.Globalization.DateTimeStyles.RoundtripKind);
            DateTime local = utc.ToLocalTime();
            return local.ToString("MMM d, yyyy  h:mm tt");
        }
        catch
        {
            return iso8601; // Fall back to raw string
        }
    }

    // =========================================================================
    // Button Handlers
    // =========================================================================

    private void OnContinueClicked()
    {
        // ProgressTracker loads the existing save on its own Awake/Start.
        LoadGameplayScene();
    }

    private void OnNewGameClicked()
    {
        // Show confirmation — don't wipe the save until the player confirms.
        SetGroupVisible(confirmPanelGroup, visible: true);
    }

    private void OnNewGameConfirmed()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();

        Debug.Log("[MainMenuController] Save wiped — starting new game.");
        LoadGameplayScene();
    }

    private void OnNewGameCancelled()
    {
        SetGroupVisible(confirmPanelGroup, visible: false);
    }

    private void OnSettingsClicked()
    {
        SetGroupVisible(settingsPanelGroup, visible: true);
    }

    private void OnSettingsClose()
    {
        SetGroupVisible(settingsPanelGroup, visible: false);
        SaveAudioPreferences();
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Quit.");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // =========================================================================
    // Scene Transition
    // =========================================================================

    private void LoadGameplayScene()
    {
        StartCoroutine(FadeOutThenLoad(GAMEPLAY_SCENE));
    }

    private IEnumerator FadeOutThenLoad(string sceneName)
    {
        if (screenFadeGroup != null)
        {
            screenFadeGroup.blocksRaycasts = true;
            yield return StartCoroutine(FadeGroup(screenFadeGroup, 0f, 1f, 0.5f));
        }
        SceneManager.LoadScene(sceneName);
    }

    // =========================================================================
    // Audio Settings
    // =========================================================================

    private void LoadAudioPreferences()
    {
        float master = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        float music  = PlayerPrefs.GetFloat(PREF_MUSIC,  1f);
        float sfx    = PlayerPrefs.GetFloat(PREF_SFX,    1f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (musicVolumeSlider  != null) musicVolumeSlider.value  = music;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfx;

        ApplyVolume(PARAM_MASTER, master);
        ApplyVolume(PARAM_MUSIC,  music);
        ApplyVolume(PARAM_SFX,    sfx);
    }

    private void SaveAudioPreferences()
    {
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat(PREF_MASTER, masterVolumeSlider.value);
        if (musicVolumeSlider  != null) PlayerPrefs.SetFloat(PREF_MUSIC,  musicVolumeSlider.value);
        if (sfxVolumeSlider    != null) PlayerPrefs.SetFloat(PREF_SFX,    sfxVolumeSlider.value);
        PlayerPrefs.Save();
    }

    private void OnMasterVolumeChanged(float value) => ApplyVolume(PARAM_MASTER, value);
    private void OnMusicVolumeChanged(float value)  => ApplyVolume(PARAM_MUSIC,  value);
    private void OnSFXVolumeChanged(float value)    => ApplyVolume(PARAM_SFX,    value);

    /// <summary>
    /// Maps a linear 0-1 slider value to dB and sets the AudioMixer parameter.
    /// Standard curve: dB = 20 * log10(value). Clamps to -80 dB to avoid -Infinity.
    /// </summary>
    private void ApplyVolume(string paramName, float linearValue)
    {
        if (audioMixer == null) return;
        float dB = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        audioMixer.SetFloat(paramName, dB);
    }

    // =========================================================================
    // Panel Fade Helpers  (same pattern as GameInterface.cs)
    // =========================================================================

    private void SetGroupVisible(CanvasGroup group, bool visible, bool instant = false)
    {
        if (group == null) return;

        if (instant)
        {
            group.alpha          = visible ? 1f : 0f;
            group.interactable   = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
            return;
        }

        if (visible)
        {
            group.gameObject.SetActive(true);
            StartCoroutine(FadeGroup(group, 0f, 1f, FADE_DURATION, onComplete: () =>
            {
                group.interactable   = true;
                group.blocksRaycasts = true;
            }));
        }
        else
        {
            group.interactable   = false;
            group.blocksRaycasts = false;
            StartCoroutine(FadeGroup(group, group.alpha, 0f, FADE_DURATION, onComplete: () =>
            {
                group.gameObject.SetActive(false);
            }));
        }
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to,
                                  float duration, Action onComplete = null)
    {
        if (group == null) yield break;
        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
        onComplete?.Invoke();
    }
}
