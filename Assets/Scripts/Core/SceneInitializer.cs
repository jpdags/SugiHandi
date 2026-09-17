// SceneInitializer.cs — Placed in every gameplay scene. Applies that scene's SceneConfig on Start.
//
// This is the bridge between the SceneConfig data asset and the live scene.
// When Unity loads a scene, this MonoBehaviour fires and tells SceneTransitionManager
// which config to apply (BGM, ambience, ambient color, etc.).
//
// SETUP IN UNITY (per scene):
//   1. Create an empty GameObject named "_SceneInitializer" in the scene root.
//   2. Add this component to it.
//   3. Assign the matching SceneConfig asset (e.g. SceneConfig_RiverVillage) in the Inspector.
//   4. Optionally assign a PlayerSpawn transform — otherwise the script finds it by tag.

using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Tooltip("The SceneConfig asset for this specific scene. Must match the scene's name.")]
    public SceneConfig config;

    [Tooltip("Optional: direct reference to the player spawn point. If null, found by tag.")]
    public Transform playerSpawnPoint;

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[SceneInitializer] No SceneConfig assigned! Scene will load without audio or atmosphere.");
            return;
        }

        // Apply audio and atmosphere via SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ApplySceneConfig(config);
        }
        else
        {
            Debug.LogWarning("[SceneInitializer] SceneTransitionManager not found. " +
                             "Make sure it exists in the MainMenu or a persistent scene.");
        }

        // Spawn the player at the spawn point
        SpawnPlayer();

        // Raise scene loaded event so other systems can react (e.g. trigger intro cinematics)
        GameEvents.RaiseSceneLoaded(config);

        Debug.Log($"[SceneInitializer] Scene '{config.displayName}' initialized.");
    }

    private void SpawnPlayer()
    {
        // Find spawn point: use direct reference if assigned, otherwise find by tag
        Transform spawn = playerSpawnPoint;

        if (spawn == null)
        {
            GameObject spawnObj = GameObject.FindGameObjectWithTag(config.spawnPointTag);
            if (spawnObj != null) spawn = spawnObj.transform;
        }

        if (spawn == null)
        {
            Debug.LogWarning($"[SceneInitializer] No spawn point found (tag: '{config.spawnPointTag}'). " +
                              "Player will spawn at world origin.");
            return;
        }

        // Find the player by tag and move them to spawn
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawn.position;
            Debug.Log($"[SceneInitializer] Player spawned at {spawn.position}");
        }
    }
}
