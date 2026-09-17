// KodeksEntryData.cs — ScriptableObject for a single Butuanon Kodeks entry.
//
// IMPORTANT: The 'sceneContext' field shows WHERE the word was encountered — not what it means.
// The Kodeks intentionally shows context instead of translation, reinforcing incidental learning.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Kodeks Entry

using UnityEngine;

[CreateAssetMenu(fileName = "Kodeks_word", menuName = "SugiHandi/Kodeks Entry")]
public class KodeksEntryData : ScriptableObject
{
    [Tooltip("The Butuanon word or phrase. This is the key used in {wordKey} dialogue tags.")]
    public string wordKey; // e.g. "manaog"

    [Tooltip("The word as it should appear in the Kodeks panel heading.")]
    public string displayWord; // e.g. "Manaog"

    [Tooltip("The scene context in which the player encountered this word. " +
             "This is shown in the Kodeks — NOT a translation. " +
             "e.g. 'Heard from Lola Bulan at the dock, when she called you by name.'")]
    [TextArea(2, 4)]
    public string sceneContext;

    [Tooltip("Cultural significance of this word in Butuan society. " +
             "A brief, historically grounded note — still not a translation.")]
    [TextArea(2, 4)]
    public string culturalSignificance;

    [Tooltip("Optional: sprite or icon representing this word (artifact image, cultural symbol, etc.)")]
    public Sprite icon;
}
