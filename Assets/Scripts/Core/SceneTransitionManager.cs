// SceneTransitionManager.cs — Singleton that manages all scene loading and audio transitions.
//
// Responsibilities:
//   - Fade-to-black screen wipe between scenes (using a full-screen CanvasGroup overlay)
//   - Playing and crossfading BGM and ambience AudioSources when scenes change
//   - Applying ambient color from SceneConfig
//   - Persisting across scene loads (DontDestroyOnLoad)
//
// SETUP IN UNITY:
//   1. Create an empty GameObject named "_SceneTransitionManager" in the MainMenu scene.
//   2. Add this component to it.
//   3. Create a full-screen Canvas (Screen Space - Overlay, sort order 999).
//      Add a black Image child and add a CanvasGroup to it. Assign as fadeOverlayGroup.
//   4. Add two AudioSource components to this GameObject:
//      - bgmSource: Audio Mixer Group = Music, Loop = true, Play On Awake = false
//      - ambienceSource: Audio Mixer Group = Ambience, Loop = true, Play On Awake = false
//   5. Assign both AudioSources in the Inspector below.
//
// All other systems request scene transitions via:
//   SceneTransitionManager.Instance.LoadScene("SceneName");

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Overlay")]
    [Tooltip("CanvasGroup on a full-screen black Image (sort order 999). Controls fade-to-black.")]
    public CanvasGroup fadeOverlayGroup;

    [Tooltip("Duration of the fade-out (scene → black) in seconds.")]
    public float fadeOutDuration = 0.4f;

    [Tooltip("Duration of the fade-in (black → scene) in seconds.")]
    public float fadeInDuration  = 0.3f;

    [Header("Audio Sources")]
    [Tooltip("AudioSource for background music (Music mixer group, Loop = true).")]
    public AudioSource bgmSource;

    [Tooltip("AudioSource for ambient sound (Ambience mixer group, Loop = true).")]
    public AudioSource ambienceSource;

    [Tooltip("Duration of BGM/ambience crossfade in seconds.")]
    public float audioCrossfadeDuration = 1.0f;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isTransitioning = false;
    private SceneConfig _currentConfig;

    public SceneConfig CurrentConfig => _currentConfig;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start with overlay fully transparent
        if (fadeOverlayGroup != null)
        {
            fadeOverlayGroup.alpha = 0f;
            fadeOverlayGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnSceneTransitionRequested += LoadScene;
    }

    private void OnDisable()
    {
        GameEvents.OnSceneTransitionRequested -= LoadScene;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Trigger a fade-to-black then load the named scene.
    /// Safe to call from anywhere — ignores duplicate calls while transitioning.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionToScene(sceneName));
    }

    /// <summary>
    /// Called by SceneInitializer when a scene finishes loading.
    /// Applies BGM, ambience, and ambient color from the config.
    /// </summary>
    public void ApplySceneConfig(SceneConfig config)
    {
        _currentConfig = config;

        // Apply ambient lighting color
        RenderSettings.ambientLight = config.ambientColor;

        // Crossfade audio
        StartCoroutine(CrossfadeAudio(config));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator TransitionToScene(string sceneName)
    {
        _isTransitioning = true;

        // 1. Fade screen to black
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeOutDuration));

        // 2. Load the scene (synchronous for simplicity — use async for large scenes)
        SceneManager.LoadScene(sceneName);

        // 3. Wait one frame for scene to initialize
        yield return null;

        // 4. Fade screen back in
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeInDuration));

        _isTransitioning = false;
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlayGroup == null) yield break;

        fadeOverlayGroup.blocksRaycasts = (to > 0.5f); // block input during fade-out
        fadeOverlayGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlayGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadeOverlayGroup.alpha = to;
        if (to <= 0f) fadeOverlayGroup.blocksRaycasts = false;
    }

    private IEnumerator CrossfadeAudio(SceneConfig config)
    {
        float elapsed = 0f;
        float startBgmVol      = bgmSource      != null ? bgmSource.volume      : 0f;
        float startAmbienceVol = ambienceSource != null ? ambienceSource.volume : 0f;

        // If clip is changing, fade out first then swap
        bool bgmChanging      = bgmSource      != null && bgmSource.clip      != config.bgmClip;
        bool ambienceChanging = ambienceSource != null && ambienceSource.clip != config.ambienceClip;

        // Fade out old audio if changing
        if (bgmChanging || ambienceChanging)
        {
            while (elapsed < audioCrossfadeDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (audioCrossfadeDuration * 0.5f);
                if (bgmChanging      && bgmSource      != null) bgmSource.volume      = Mathf.Lerp(startBgmVol,      0f, t);
                if (ambienceChanging && ambienceSource != null) ambienceSource.volume = Mathf.Lerp(startAmbienceVol, 0f, t);
                yield return null;
            }
        }

        // Swap clips
        if (bgmSource != null)
        {
            bgmSource.clip   = config.bgmClip;
            bgmSource.volume = 0f;
            if (config.bgmClip != null) bgmSource.Play();
            else bgmSource.Stop();
        }

        if (ambienceSource != null)
        {
            ambienceSource.clip   = config.ambienceClip;
            ambienceSource.volume = 0f;
            if (config.ambienceClip != null) ambienceSource.Play();
            else ambienceSource.Stop();
        }

        // Fade in new audio
        elapsed = 0f;
        while (elapsed < audioCrossfadeDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (audioCrossfadeDuration * 0.5f);
            if (bgmSource      != null) bgmSource.volume      = Mathf.Lerp(0f, config.bgmVolume,      t);
            if (ambienceSource != null) ambienceSource.volume = Mathf.Lerp(0f, config.ambienceVolume, t);
            yield return null;
        }

        // Snap to target
        if (bgmSource      != null) bgmSource.volume      = config.bgmVolume;
        if (ambienceSource != null) ambienceSource.volume = config.ambienceVolume;
    }
}
