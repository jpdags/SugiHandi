// ArtifactData.cs — ScriptableObject for a single examinable artifact.
//
// One artifact is required for the demo build.
// The examine screen shows: artifact sprite + Butuanon inscription + historical note.
// NO translation of the inscription is ever shown.
// Examining the artifact also fires a Kodeks log event via GameEvents.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Artifact Data

using UnityEngine;

[CreateAssetMenu(fileName = "Artifact_ArtifactName", menuName = "SugiHandi/Artifact Data")]
public class ArtifactData : ScriptableObject
{
    [Tooltip("Unique ID for this artifact. Stored in SaveData.examinedArtifactIds.")]
    public string artifactId;

    [Tooltip("Display name shown in the examine screen header (can include Butuanon).")]
    public string artifactName;

    [Tooltip("Pixel art sprite of the artifact. Import at 16 PPU, Point (no filter).")]
    public Sprite artifactSprite;

    [Tooltip("The Butuanon inscription carved on the artifact. Shown as-is — no translation shown.")]
    [TextArea(2, 3)]
    public string butuanonInscription;

    [Tooltip("A short historical note about this artifact in the context of Butuan culture. " +
             "This is cultural context, not a translation of the inscription.")]
    [TextArea(3, 5)]
    public string historicalNote;

    [Tooltip("The Butuanon word key this artifact's Kodeks entry corresponds to. " +
             "Must match a KodeksEntryData asset's wordKey field.")]
    public string kodeksWordKey;

    [Tooltip("Reference to the KodeksEntryData that gets logged when this artifact is examined.")]
    public KodeksEntryData kodeksEntry;
}
