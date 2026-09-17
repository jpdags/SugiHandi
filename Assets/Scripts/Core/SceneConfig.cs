// SceneConfig.cs — ScriptableObject defining a SugiHandi scene's identity and setup.
//
// One asset per scene. Place all assets in Resources/Scenes/ so SceneTransitionManager
// can load them by scene name at runtime without serialized references.
//
// HOW TO USE:
//   1. Right-click in Project → Create → SugiHandi → Scene Config
//   2. Name it SceneConfig_<SceneName> (must match the Unity scene file name exactly)
//   3. Fill in all fields in the Inspector
//   4. Attach SceneInitializer to a GameObject in the scene and assign this asset to it
//
// ATMOSPHERE affects the ambient color and post-processing preset applied on load.

using UnityEngine;

public enum SceneAtmosphere
{
    Vibrant,    // River Village — warm, communal, busy daytime
    Quiet,      // Trade Port — eerily still, abandoned, dusk tones
    Sacred,     // Sacred Ruins — cool, spiritual, mysterious
    Forested,   // Forested Hills — lush green, dappled light, peaceful
    Neutral     // Main Menu / Summary Screen — no atmospheric override
}

[CreateAssetMenu(fileName = "SceneConfig_SceneName", menuName = "SugiHandi/Scene Config")]
public class SceneConfig : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Must exactly match the Unity scene file name (used for SceneManager.LoadScene).")]
    public string sceneName;

    [Tooltip("Human-readable name shown on loading screens or chapter headers.")]
    public string displayName;

    [Tooltip("Short atmospheric description. Shown briefly at scene entry (optional).")]
    [TextArea(1, 2)]
    public string locationSubtitle;

    [Header("Audio")]
    [Tooltip("Background music clip for this scene. Played on the Music mixer channel.")]
    public AudioClip bgmClip;

    [Tooltip("Looping ambient sound clip for this scene. Played on the Ambience mixer channel.")]
    public AudioClip ambienceClip;

    [Range(0f, 1f)]
    [Tooltip("BGM volume for this scene (0–1). Default 0.8.")]
    public float bgmVolume = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("Ambience volume for this scene (0–1). Default 0.4.")]
    public float ambienceVolume = 0.4f;

    [Header("Atmosphere")]
    [Tooltip("Controls ambient color tones and post-processing applied on load.")]
    public SceneAtmosphere atmosphere = SceneAtmosphere.Neutral;

    [Tooltip("Color of the ambient light for this scene. Blended in by SceneTransitionManager.")]
    public Color ambientColor = new Color(1f, 0.95f, 0.85f); // warm default

    [Header("Progression")]
    [Tooltip("If true, the player must complete the previous scene's quest to enter this scene.")]
    public bool requiresPreviousQuestComplete = false;

    [Tooltip("ID of the quest that is active/triggered in this scene. Leave empty if none.")]
    public string activeQuestId;

    [Tooltip("Scene to load when the player completes this scene's content. Leave empty to show Summary Screen.")]
    public string nextSceneName = "SummaryScreen";

    [Header("Scene Hierarchy Labels")]
    [Tooltip("Tag name of the Player Spawn Point GameObject in this scene.")]
    public string spawnPointTag = "PlayerSpawn";
}
