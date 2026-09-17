// ArtifactInteractable.cs — Component on an artifact GameObject in the scene.
//
// For the demo: one artifact is required.
// When tapped: opens the artifact examine screen via GameInterface.
// Also fires GameEvents.RaiseArtifactExamined → ProgressTracker logs it silently.
//
// SETUP IN UNITY:
//   1. Create an artifact sprite (16×16 or 32×32 px, glowing effect via URP 2D Light).
//   2. Add this component and a CircleCollider2D (isTrigger = true).
//   3. Assign artifactData (ArtifactData ScriptableObject) in the Inspector.
//   4. WorldManager auto-registers on Awake.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ArtifactInteractable : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The data for this artifact. Assign an ArtifactData ScriptableObject.")]
    public ArtifactData artifactData;

    [Header("Interaction")]
    [Tooltip("Distance in world units at which the player can examine this artifact.")]
    public float interactionRadius = 1.2f;

    [Header("Visual Feedback")]
    [Tooltip("Optional: particle or glow effect to indicate this is interactable.")]
    public GameObject glowEffect;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (WorldManager.Instance != null)
            WorldManager.Instance.RegisterArtifact(this);
    }

    private void OnDestroy()
    {
        if (WorldManager.Instance != null)
            WorldManager.Instance.UnregisterArtifact(this);
    }

    private void Start()
    {
        // If already examined in a previous session, hide the glow
        if (ProgressTracker.Instance != null && artifactData != null)
        {
            bool alreadyExamined = ProgressTracker.Instance.HasExaminedArtifact(artifactData.artifactId);
            if (glowEffect != null) glowEffect.SetActive(!alreadyExamined);
        }
    }

    // ── Interaction ──────────────────────────────────────────────────────────
    /// <summary>
    /// Called by WorldManager when the player taps this artifact while in range.
    /// Opens the examine screen and fires the artifact examined event.
    /// </summary>
    public void Examine()
    {
        if (artifactData == null)
        {
            Debug.LogWarning("[ArtifactInteractable] No ArtifactData assigned.");
            return;
        }

        // Tell GameInterface to show the examine panel
        GameInterface.Instance.ShowArtifactExamine(artifactData);

        // Fire event → ProgressTracker will log it silently
        GameEvents.RaiseArtifactExamined(artifactData);

        // Hide glow once examined
        if (glowEffect != null) glowEffect.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
