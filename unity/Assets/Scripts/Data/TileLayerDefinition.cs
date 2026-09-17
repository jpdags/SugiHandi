// TileLayerDefinition.cs — ScriptableObject defining a single Tilemap layer for SugiHandi.
//
// PURPOSE:
//   Each instance declares the rules for one Tilemap layer (e.g. "ground", "props").
//   Designers fill in the sorting info, walkability, and collision flags once,
//   and TilemapLayerSetup validates that the scene Tilemaps match at runtime.
//
// CREATE IN UNITY:
//   Right-click in Project → Create → SugiHandi → Tilemap Layer Definition
//
// NAMING CONVENTION FOR ASSETS:
//   TileLayer_ground.asset, TileLayer_props.asset, TileLayer_decorations.asset ...
//
// LAYER RENDER ORDER (bottom → top):
//   ground  →  props  →  decorations  →  characters  →  UI

using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileLayer_LayerName", menuName = "SugiHandi/Tilemap Layer Definition")]
public class TileLayerDefinition : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────────

    [Tooltip("Unique identifier for this layer.\n" +
             "Must match the child GameObject name under TilemapRoot.\n" +
             "e.g. \"ground\", \"props\", \"decorations\"")]
    public string layerId;

    [Tooltip("Human-readable name shown in inspector labels and debug logs.\n" +
             "e.g. \"Ground / Floor\"")]
    public string displayName;

    [Tooltip("Describe which tiles belong on this layer.\n" +
             "e.g. \"Base terrain: soil, stone floor, river bed. No objects — flat tiles only.\"")]
    [TextArea(2, 4)]
    public string description;

    // ── Sorting ───────────────────────────────────────────────────────────────

    [Tooltip("Unity Sorting Layer this Tilemap must be assigned to.\n" +
             "Must exist in Edit → Project Settings → Tags and Layers → Sorting Layers.\n" +
             "e.g. \"Ground\"")]
    public string sortingLayerName = "Default";

    [Tooltip("Order Within Layer for the TilemapRenderer.\n" +
             "Lower values render behind higher values within the same sorting layer.\n" +
             "e.g. 0 for the base ground, 10 for a shadow overlay on the same layer.")]
    public int sortingOrderInLayer = 0;

    // ── Physics / Gameplay ────────────────────────────────────────────────────

    [Tooltip("If true, the player can walk on tiles placed on this layer.\n" +
             "Used by TilemapLayerSetup to assert the layer has no blocking collider.")]
    public bool isWalkable = true;

    [Tooltip("If true, this Tilemap layer should have an active TilemapCollider2D.\n" +
             "Enable for collision-only layers (invisible walls, water edges).\n" +
             "Leave OFF for purely visual layers.")]
    public bool hasCollision = false;

    // ── Editor Visuals ────────────────────────────────────────────────────────

    [Tooltip("Editor-only tint colour used to distinguish layers when multiple Tilemaps\n" +
             "are visible in the Scene view. Set the TilemapRenderer.color to this in the editor.\n" +
             "Alpha at runtime should be 1 — this is a design aid only.")]
    public Color layerColor = Color.white;

    [Tooltip("Optional sample sprites of tiles that belong on this layer.\n" +
             "Import sprites at 16 PPU, Point (no filter), for SugiHandi pixel art.\n" +
             "This is purely a visual reference for artists — not used at runtime.")]
    public Sprite[] exampleTileSprites;

#if UNITY_EDITOR
    // ── Editor Helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a one-line summary for editor logs and custom inspectors.
    /// </summary>
    public string EditorSummary =>
        $"[{layerId}] \"{displayName}\" | Sorting: {sortingLayerName}/{sortingOrderInLayer} " +
        $"| Walkable: {isWalkable} | Collision: {hasCollision}";
#endif
}