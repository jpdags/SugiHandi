// TilemapLayerSetup.cs — Runtime validator for SugiHandi Tilemap layer configuration.
//
// PURPOSE:
//   Validates that every Tilemap child of this GameObject matches the rules defined
//   in TileLayerRegistry. Logs warnings if anything is misconfigured.
//   Does NOT modify any data — purely a read-and-warn system.
//
// SETUP IN UNITY:
//   1. Create a root GameObject named "TilemapRoot" in your scene.
//   2. Add a Grid component to TilemapRoot.
//   3. Under TilemapRoot, create one child GameObject per layer.
//      Name each child EXACTLY after its layerId (e.g. "ground", "props").
//   4. On each child: add Tilemap + TilemapRenderer.
//      For collision layers: also add TilemapCollider2D.
//   5. Add TilemapLayerSetup to TilemapRoot and assign the TileLayerRegistry asset.
//
// CHILD NAMING CONVENTION:
//   TilemapRoot
//   ├─ ground          → matches TileLayerDefinition.layerId = "ground"
//   ├─ props           → matches TileLayerDefinition.layerId = "props"
//   └─ decorations     → matches TileLayerDefinition.layerId = "decorations"

using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapLayerSetup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("The Tile Layer Registry asset listing all layer definitions.\n" +
             "Right-click Assets → Create → SugiHandi → Tile Layer Registry to create one.")]
    public TileLayerRegistry registry;

    [Tooltip("If true, validation runs again every time you enter Play mode.\n" +
             "Safe to leave on — it only reads and logs, never writes.")]
    public bool validateOnAwake = true;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (validateOnAwake)
            ValidateLayers();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks every direct child of this GameObject and checks that:
    ///   • Its name matches a known layerId in the registry.
    ///   • Its TilemapRenderer sorting layer + order match the definition.
    ///   • Its TilemapCollider2D active state matches the definition's hasCollision flag.
    ///
    /// Warnings are printed to the Console. No data is modified.
    /// </summary>
    public void ValidateLayers()
    {
        if (registry == null)
        {
            Debug.LogWarning("[TilemapLayerSetup] No TileLayerRegistry assigned. " +
                             "Assign one in the Inspector.", this);
            return;
        }

        int childCount = transform.childCount;
        if (childCount == 0)
        {
            Debug.LogWarning("[TilemapLayerSetup] No child Tilemaps found under this GameObject. " +
                             "Add child GameObjects named after each layerId.", this);
            return;
        }

        bool allValid = true;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string childName = child.gameObject.name.ToLowerInvariant().Trim();

            // ── Check if this child's name maps to a known layer ──────────────
            TileLayerDefinition def = registry.GetLayerById(childName);
            if (def == null)
            {
                Debug.LogWarning($"[TilemapLayerSetup] Child \"{child.name}\" does not match any " +
                                 $"layerId in the registry. Rename it to match a layerId " +
                                 $"(e.g. \"ground\"), or add a new TileLayerDefinition.", child);
                allValid = false;
                continue;
            }

            // ── Validate TilemapRenderer ──────────────────────────────────────
            TilemapRenderer renderer = child.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"[TilemapLayerSetup] Layer \"{def.displayName}\" ({child.name}) " +
                                 $"is missing a TilemapRenderer.", child);
                allValid = false;
            }
            else
            {
                bool sortingLayerMatch = renderer.sortingLayerName == def.sortingLayerName;
                bool sortingOrderMatch = renderer.sortingOrder == def.sortingOrderInLayer;

                if (!sortingLayerMatch)
                {
                    Debug.LogWarning($"[TilemapLayerSetup] Layer \"{def.displayName}\" ({child.name}): " +
                                     $"TilemapRenderer.sortingLayerName is \"{renderer.sortingLayerName}\" " +
                                     $"but registry expects \"{def.sortingLayerName}\".", child);
                    allValid = false;
                }

                if (!sortingOrderMatch)
                {
                    Debug.LogWarning($"[TilemapLayerSetup] Layer \"{def.displayName}\" ({child.name}): " +
                                     $"TilemapRenderer.sortingOrder is {renderer.sortingOrder} " +
                                     $"but registry expects {def.sortingOrderInLayer}.", child);
                    allValid = false;
                }
            }

            // ── Validate TilemapCollider2D ────────────────────────────────────
            TilemapCollider2D col = child.GetComponent<TilemapCollider2D>();
            bool hasColliderComponent = col != null && col.enabled;

            if (def.hasCollision && !hasColliderComponent)
            {
                Debug.LogWarning($"[TilemapLayerSetup] Layer \"{def.displayName}\" ({child.name}): " +
                                 $"Registry says hasCollision=true, but no active TilemapCollider2D found. " +
                                 $"Add and enable one.", child);
                allValid = false;
            }
            else if (!def.hasCollision && hasColliderComponent)
            {
                Debug.LogWarning($"[TilemapLayerSetup] Layer \"{def.displayName}\" ({child.name}): " +
                                 $"Registry says hasCollision=false, but an active TilemapCollider2D is present. " +
                                 $"Disable or remove it for a purely visual layer.", child);
                allValid = false;
            }

            if (allValid || renderer != null)
            {
                // Only print OK line if the individual layer checks above didn't already fail it.
                bool thisLayerOk = (renderer != null &&
                                    renderer.sortingLayerName == def.sortingLayerName &&
                                    renderer.sortingOrder == def.sortingOrderInLayer &&
                                    def.hasCollision == hasColliderComponent);
                if (thisLayerOk)
                {
                    Debug.Log($"[TilemapLayerSetup] ✓ Layer \"{def.displayName}\" ({child.name}) — OK.", child);
                }
            }
        }

        if (allValid)
            Debug.Log("[TilemapLayerSetup] All Tilemap layers validated successfully.", this);
    }

#if UNITY_EDITOR
    // ── Editor Context Menu ───────────────────────────────────────────────────

    /// <summary>
    /// Allows manual validation from the Inspector context menu without entering Play mode.
    /// </summary>
    [ContextMenu("Validate Layers Now")]
    private void ValidateLayersFromEditor()
    {
        ValidateLayers();
    }
#endif
}