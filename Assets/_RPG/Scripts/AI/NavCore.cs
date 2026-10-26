using System.Collections.Generic;
using UnityEngine;

/// <summary>A* sobre rejilla booleana (8 direcciones, sin cortar esquinas).</summary>
public static class GridPathfinder
{
    static readonly int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    public static List<Vector2Int> Find(bool[,] blocked, int w, int h, Vector2Int start, Vector2Int goal)
    {
        int total = w * h;
        if (total <= 0) return null;

        float[] g = new float[total];
        float[] f = new float[total];
        int[] parent = new int[total];
        bool[] closed = new bool[total];

        for (int i = 0; i < total; i++) { g[i] = float.MaxValue; f[i] = float.MaxValue; parent[i] = -1; }

        int startIndex = start.x * h + start.y;
        int goalIndex = goal.x * h + goal.y;

        List<int> open = new List<int>(256);
        g[startIndex] = 0f;
        f[startIndex] = Heuristic(start, goal);
        open.Add(startIndex);

        int expansions = 0;
        while (open.Count > 0 && expansions < 40000)
        {
            int bestPos = 0;
            for (int i = 1; i < open.Count; i++)
                if (f[open[i]] < f[open[bestPos]]) bestPos = i;

            int current = open[bestPos];
            open.RemoveAt(bestPos);

            if (current == goalIndex) return Reconstruct(parent, current, h);
            if (closed[current]) continue;
            closed[current] = true;
            expansions++;

            int cx = current / h;
            int cy = current % h;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + dx[d];
                int ny = cy + dy[d];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (blocked[nx, ny]) continue;
                if (d >= 4 && (blocked[cx + dx[d], cy] || blocked[cx, cy + dy[d]])) continue;

                int ni = nx * h + ny;
                if (closed[ni]) continue;

                float step = d >= 4 ? 1.4142f : 1f;
                float candidate = g[current] + step;
                if (candidate >= g[ni]) continue;

                g[ni] = candidate;
                f[ni] = candidate + Heuristic(new Vector2Int(nx, ny), goal);
                parent[ni] = current;
                if (!open.Contains(ni)) open.Add(ni);
            }
        }
        return null;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int ax = Mathf.Abs(a.x - b.x);
        int ay = Mathf.Abs(a.y - b.y);
        return (ax > ay) ? (ax - ay) + 1.4142f * ay : (ay - ax) + 1.4142f * ax;
    }

    static List<Vector2Int> Reconstruct(int[] parent, int current, int h)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (current != -1)
        {
            path.Add(new Vector2Int(current / h, current % h));
            current = parent[current];
        }
        path.Reverse();
        path.RemoveAt(0);
        return path;
    }
}

/// <summary>Punto de acceso único a la navegación de enemigos.</summary>
public static class EnemyNavigation
{
    public static NavGridBuilder EnsureGrid()
    {
        if (NavGridBuilder.Instance != null) return NavGridBuilder.Instance;
        GameObject go = new GameObject("~NavGrid");
        return go.AddComponent<NavGridBuilder>();
    }

    public static int WallsLayer { get { return LayerMask.NameToLayer("Walls"); } }

    public static bool IsReady { get { return NavGridBuilder.Instance != null && NavGridBuilder.Instance.IsReady; } }
}
