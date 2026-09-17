// WalkabilityManager.cs — Recognizes walkable tiles and obstacle tiles/assets.
//
// HOW IT WORKS:
//   1. Tilemap Layer Recognition:
//      - Ground/Walkable Tilemap: A tile must exist here to be walkable (if requireGroundTile is true).
//      - Obstacle Tilemaps: Any tile placed on these layers (e.g. Collision, Water, Cliffs, Walls) is blocked.
//   2. Scene Asset / Physics Obstacle Recognition:
//      - Checks for 2D colliders (trees, rocks, fences, buildings) on the designated obstacleLayerMask.
//      - Ignores Trigger colliders (so NPC interaction zones and puzzle triggers don't block movement).
//   3. Smart Proximity Search:
//      - Can find the nearest walkable tile if the player taps near an NPC or obstacle.
//
// SETUP IN UNITY:
//   - Add this script to your Grid GameObject (or an empty Manager GameObject in the scene).
//   - Assign your Ground Tilemap and any Obstacle Tilemaps in the Inspector (or let it auto-find them by name).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WalkabilityManager : MonoBehaviour
{
    public static WalkabilityManager Instance { get; private set; }

    [Header("Grid Reference")]
    [Tooltip("The main Grid component in the scene. If empty, will auto-find.")]
    public Grid grid;

    [Header("Tilemap Layer Recognition")]
    [Tooltip("The primary ground/floor tilemap. Tiles here are walkable.")]
    public Tilemap groundTilemap;

    [Tooltip("Tilemaps where any present tile is treated as an impassable obstacle (e.g. Collision, Water, Walls).")]
    public List<Tilemap> obstacleTilemaps = new List<Tilemap>();

    [Tooltip("If true, movement is ONLY allowed on cells where groundTilemap has a tile.")]
    public bool requireGroundTile = true;

    [Header("Asset & Object Collision Recognition")]
    [Tooltip("LayerMask containing physical obstacles (trees, rocks, buildings, props, water colliders).")]
    public LayerMask obstacleLayerMask;

    [Tooltip("Check radius within a tile when scanning for 2D colliders (keeps checks contained within the tile).")]
    public float colliderCheckRadius = 0.35f;

    [Tooltip("If true, trigger colliders (like NPC interaction zones) are ignored and won't block movement.")]
    public bool ignoreTriggers = true;

    [Header("Scene View Debug")]
    [Tooltip("Draws walkable (green) and obstacle (red) tiles in the Unity Scene view.")]
    public bool showDebugGizmos = false;
    [Range(2, 20)]
    public int gizmoRange = 8;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (grid == null)
            grid = GetComponent<Grid>() ?? FindObjectOfType<Grid>();

        AutoFindTilemapsIfEmpty();
    }

    /// <summary>
    /// Auto-detects standard Tilemaps in the scene if not explicitly assigned in the Inspector.
    /// </summary>
    private void AutoFindTilemapsIfEmpty()
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();

        foreach (var tm in allTilemaps)
        {
            string lowerName = tm.name.ToLower();

            // Auto-detect Ground
            if (groundTilemap == null && (lowerName.Contains("ground") || lowerName.Contains("floor") || lowerName.Contains("walkable") || lowerName.Contains("base")))
            {
                groundTilemap = tm;
                continue;
            }

            // Auto-detect Obstacles
            if (lowerName.Contains("collision") || lowerName.Contains("obstacle") || lowerName.Contains("water") || lowerName.Contains("wall") || lowerName.Contains("cliff"))
            {
                if (!obstacleTilemaps.Contains(tm))
                {
                    obstacleTilemaps.Add(tm);
                }
            }
        }
    }

    // ── Core Walkability Queries ─────────────────────────────────────────────

    /// <summary>
    /// Checks if a world position is walkable.
    /// </summary>
    public bool IsWalkable(Vector2 worldPosition)
    {
        if (grid == null)
        {
            // Fallback if no grid exists: check only 2D colliders
            return !HasObstacleCollider(worldPosition);
        }

        Vector3Int cellPos = grid.WorldToCell(worldPosition);
        return IsWalkableCell(cellPos);
    }

    /// <summary>
    /// Checks if a specific grid cell coordinate is walkable.
    /// </summary>
    public bool IsWalkableCell(Vector3Int cellPosition)
    {
        // 1. Check if ground tile is present (if required)
        if (requireGroundTile && groundTilemap != null)
        {
            if (!groundTilemap.HasTile(cellPosition))
                return false; // No ground under this tile
        }

        // 2. Check if an obstacle tile exists at this coordinate
        for (int i = 0; i < obstacleTilemaps.Count; i++)
        {
            if (obstacleTilemaps[i] != null && obstacleTilemaps[i].HasTile(cellPosition))
            {
                return false; // Blocked by obstacle tilemap
            }
        }

        // 3. Check for 2D colliders on obstacle LayerMask at the center of the cell
        if (grid != null)
        {
            Vector2 cellCenter = grid.GetCellCenterWorld(cellPosition);
            if (HasObstacleCollider(cellCenter))
            {
                return false; // Blocked by asset/prop/building collider
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether there is a physical 2D collider blocking the given world position.
    /// </summary>
    public bool HasObstacleCollider(Vector2 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, colliderCheckRadius, obstacleLayerMask);

        foreach (var hit in hits)
        {
            // Skip triggers (e.g. NPC interaction radius, puzzle marker triggers)
            if (ignoreTriggers && hit.isTrigger)
                continue;

            // Skip player character collider
            if (hit.GetComponent<PlayerMovement>() != null)
                continue;

            return true; // Found a blocking obstacle collider
        }

        return false;
    }

    /// <summary>
    /// Finds the nearest walkable cell within a search radius from a starting world position.
    /// Returns true if a walkable neighbor was found.
    /// </summary>
    public bool TryGetNearestWalkable(Vector2 worldPosition, out Vector2 walkableWorldPos, int maxRadius = 3)
    {
        walkableWorldPos = worldPosition;
        if (grid == null) return false;

        Vector3Int startCell = grid.WorldToCell(worldPosition);

        // If the cell itself is already walkable, return it
        if (IsWalkableCell(startCell))
        {
            walkableWorldPos = grid.GetCellCenterWorld(startCell);
            return true;
        }

        // Search outward in expanding rings
        float closestDistance = float.MaxValue;
        Vector3Int bestCell = startCell;
        bool found = false;

        for (int r = 1; r <= maxRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    // Check only the perimeter of the current ring
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;

                    Vector3Int neighborCell = startCell + new Vector3Int(x, y, 0);

                    if (IsWalkableCell(neighborCell))
                    {
                        Vector2 neighborWorld = grid.GetCellCenterWorld(neighborCell);
                        float dist = Vector2.Distance(worldPosition, neighborWorld);

                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            bestCell = neighborCell;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                walkableWorldPos = grid.GetCellCenterWorld(bestCell);
                return true;
            }
        }

        return false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || grid == null) return;

        Vector3 centerPos = transform.position;
        if (Camera.main != null) centerPos = Camera.main.transform.position;

        Vector3Int centerCell = grid.WorldToCell(centerPos);
        Vector3 cellSize = grid.cellSize;

        for (int x = -gizmoRange; x <= gizmoRange; x++)
        {
            for (int y = -gizmoRange; y <= gizmoRange; y++)
            {
                Vector3Int cell = centerCell + new Vector3Int(x, y, 0);
                Vector2 world = grid.GetCellCenterWorld(cell);
                bool walkable = IsWalkableCell(cell);

                Gizmos.color = walkable ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.45f);
                Gizmos.DrawWireCube(world, cellSize * 0.9f);
            }
        }
    }
}
