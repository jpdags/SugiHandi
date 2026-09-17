using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Pathfinder
{
    // Each node represents one tile the algorithm is considering
    private class Node
    {
        public Vector3Int cell;
        public Node parent;      // so we can trace the path backward at the end
        public float gCost;      // distance from start
        public float hCost;      // estimated distance to end (heuristic)
        public float fCost => gCost + hCost;  // total score — lower = better

        public Node(Vector3Int cell, Node parent, float g, float h)
        {
            this.cell = cell;
            this.parent = parent;
            this.gCost = g;
            this.hCost = h;
        }
    }

    // ------------------------------------------------------------
    // Directions the character can move — 4-directional only.
    // If you want diagonal movement, uncomment the extra four lines.
    // IMPORTANT: diagonals need a corner-cut check (see IsWalkable) 
    // or your character will clip through wall corners.
    // ------------------------------------------------------------
    private static readonly Vector3Int[] Neighbors = new Vector3Int[]
    {
        new Vector3Int( 0,  1, 0),  // up
        new Vector3Int( 0, -1, 0),  // down
        new Vector3Int(-1,  0, 0),  // left
        new Vector3Int( 1,  0, 0),  // right
        // new Vector3Int( 1,  1, 0),  // diagonal: up-right
        // new Vector3Int(-1,  1, 0),  // diagonal: up-left
        // new Vector3Int( 1, -1, 0),  // diagonal: down-right
        // new Vector3Int(-1, -1, 0),  // diagonal: down-left
    };

    public static List<Vector3Int> FindPath(
        Vector3Int start,
        Vector3Int end,
        Tilemap ground,
        Tilemap obstacles)
    {
        // Early exit: if destination is already blocked, don't bother
        if (!IsWalkable(end, ground, obstacles))
            return null;

        // Open list: tiles we know about but haven't fully evaluated yet
        // Closed set: tiles we've already finalized — never revisit them
        List<Node> open = new List<Node>();
        HashSet<Vector3Int> closed = new HashSet<Vector3Int>();

        open.Add(new Node(start, null, 0, Heuristic(start, end)));

        while (open.Count > 0)
        {
            // Pick the node with the lowest fCost (best candidate)
            Node current = GetLowestF(open);

            // Reached the destination — trace back through parents to build path
            if (current.cell == end)
                return TracePath(current);

            open.Remove(current);
            closed.Add(current.cell);

            foreach (Vector3Int dir in Neighbors)
            {
                Vector3Int neighborCell = current.cell + dir;

                if (closed.Contains(neighborCell)) continue;
                if (!IsWalkable(neighborCell, ground, obstacles)) continue;

                // Diagonal moves cost more than cardinal (1.4 ≈ √2)
                // For 4-directional only this is always 1
                float moveCost = (dir.x != 0 && dir.y != 0) ? 1.4f : 1f;
                float newG = current.gCost + moveCost;

                Node existing = open.Find(n => n.cell == neighborCell);
                if (existing == null)
                {
                    // Brand new tile — add it to open list
                    open.Add(new Node(neighborCell, current, newG, Heuristic(neighborCell, end)));
                }
                else if (newG < existing.gCost)
                {
                    // We found a cheaper route to a tile already in the open list
                    existing.gCost = newG;
                    existing.parent = current;
                }
            }
        }

        return null; // no path found — tile is unreachable
    }

    // Manhattan distance: correct heuristic for 4-directional grid movement
    // Switch to Euclidean (Vector3.Distance) if you enable diagonals
    private static float Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Node GetLowestF(List<Node> open)
    {
        Node best = open[0];
        foreach (Node n in open)
            if (n.fCost < best.fCost) best = n;
        return best;
    }

    private static bool IsWalkable(Vector3Int cell, Tilemap ground, Tilemap obstacles)
    {
        return ground.HasTile(cell) && !obstacles.HasTile(cell);
    }

    // Walk backwards from the end node through parent references,
    // then reverse so the list goes start → end
    private static List<Vector3Int> TracePath(Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.cell);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}