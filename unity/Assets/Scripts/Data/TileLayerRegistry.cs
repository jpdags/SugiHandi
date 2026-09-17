// TileLayerRegistry.cs — ScriptableObject catalogue of all SugiHandi Tilemap layers.
//
// PURPOSE:
//   A single asset that holds every TileLayerDefinition in render order (bottom → top).
//   TilemapLayerSetup reads this registry to validate the scene Tilemaps at play-time.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Tile Layer Registry
//   Name the asset: TileLayerRegistry.asset (one per project is enough)
//
// USAGE:
//   - Assign the asset to TilemapLayerSetup.registry in the Inspector.
//   - Call GetLayerById("ground") to look up a definition by ID at runtime or in editor tools.

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileLayerRegistry", menuName = "SugiHandi/Tile Layer Registry")]
public class TileLayerRegistry : ScriptableObject
{
    // ── Data ─────────────────────────────────────────────────────────────────

    [Tooltip("All Tilemap layer definitions for this game, ordered bottom → top render order.\n" +
             "e.g. index 0 = ground, index 1 = props, index 2 = decorations.\n\n" +
             "The order here is the authoritative render order — make sure Unity Sorting\n" +
             "Layer order and TilemapRenderer.sortingOrder match.")]
    public TileLayerDefinition[] layers;

    // ── Lookup ────────────────────────────────────────────────────────────────

    // Cached dictionary built on first access — avoids repeated linear searches.
    private Dictionary<string, TileLayerDefinition> _cache;

    /// <summary>
    /// Returns the TileLayerDefinition whose layerId matches <paramref name="id"/>,
    /// or null if not found.
    /// </summary>
    public TileLayerDefinition GetLayerById(string id)
    {
        BuildCacheIfNeeded();
        _cache.TryGetValue(id, out TileLayerDefinition result);
        return result;
    }

    /// <summary>
    /// Returns true if a layer with the given ID is registered.
    /// </summary>
    public bool HasLayer(string id)
    {
        BuildCacheIfNeeded();
        return _cache.ContainsKey(id);
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, TileLayerDefinition>(StringComparer.OrdinalIgnoreCase);

        if (layers == null) return;
        foreach (var layer in layers)
        {
            if (layer == null) continue;
            if (string.IsNullOrEmpty(layer.layerId)) continue;

            if (!_cache.ContainsKey(layer.layerId))
                _cache[layer.layerId] = layer;
            else
                Debug.LogWarning($"[TileLayerRegistry] Duplicate layerId \"{layer.layerId}\" found. " +
                                 $"Only the first entry will be used.");
        }
    }

    // Reset the cache whenever the asset is modified in the editor.
    private void OnValidate() => _cache = null;

#if UNITY_EDITOR
    // ── Editor Helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Logs a summary of all registered layers to the console. Useful during setup.
    /// Call from the Unity editor via a context menu button.
    /// </summary>
    [ContextMenu("Print Layer Summary")]
    private void PrintLayerSummary()
    {
        if (layers == null || layers.Length == 0)
        {
            Debug.Log("[TileLayerRegistry] No layers registered.");
            return;
        }

        Debug.Log($"[TileLayerRegistry] {layers.Length} layer(s) registered (bottom → top):");
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                Debug.Log($"  [{i}] <null slot>");
                continue;
            }
            Debug.Log($"  [{i}] {layers[i].EditorSummary}");
        }
    }
#endif
}