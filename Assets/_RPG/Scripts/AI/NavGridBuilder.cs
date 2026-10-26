using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye una rejilla de navegación 2D a partir de los colliders estáticos
/// de la escena (layer "Walls"). Es la base del sistema de navegación A* que
/// sustituye a NavMesh, que en Unity 6 no se puede hornear sobre Tilemaps 2D.
/// </summary>
public class NavGridBuilder : MonoBehaviour
{
    public static NavGridBuilder Instance { get; private set; }

    public float cellSize = 0.5f;
    public LayerMask obstacleMask;
    public Collider2D areaBounds;

    private bool[,] blocked;
    private Vector2 origin;
    private int width;
    private int height;

    public bool IsReady { get; private set; }
    public int Width { get { return width; } }
    public int Height { get { return height; } }
    public float CellSize { get { return cellSize; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Build()
    {
        if (obstacleMask.value == 0)
        {
            int walls = LayerMask.NameToLayer("Walls");
            obstacleMask = walls >= 0 ? (1 << walls) : 0;
        }

        Bounds b;
        if (areaBounds == null)
        {
            CameraFollow cf = Object.FindFirstObjectByType<CameraFollow>();
            if (cf != null) areaBounds = cf.bounds;
        }

        if (areaBounds != null)
        {
            b = areaBounds.bounds;
        }
        else
        {
            Collider2D[] all = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            if (all.Length == 0) { IsReady = false; return; }
            b = all[0].bounds;
            for (int i = 1; i < all.Length; i++) b.Encapsulate(all[i].bounds);
        }

        if (b.size.x < 1f || b.size.y < 1f) { IsReady = false; return; }

        origin = new Vector2(b.min.x, b.min.y);
        width = Mathf.Clamp(Mathf.CeilToInt(b.size.x / cellSize), 6, 512);
        height = Mathf.Clamp(Mathf.CeilToInt(b.size.y / cellSize), 6, 512);
        blocked = new bool[width, height];

        if (obstacleMask.value != 0)
        {
            Collider2D[] obstacles = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            for (int i = 0; i < obstacles.Length; i++)
            {
                Collider2D c = obstacles[i];
                if (c == null || c.isTrigger) continue;
                if (((1 << c.gameObject.layer) & obstacleMask.value) == 0) continue;

                Bounds ob = c.bounds;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((ob.min.x - origin.x) / cellSize) - 1, 0, width - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((ob.max.x - origin.x) / cellSize) + 1, 0, width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((ob.min.y - origin.y) / cellSize) - 1, 0, height - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((ob.max.y - origin.y) / cellSize) + 1, 0, height - 1);

                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                        blocked[x, y] = true;
            }
        }

        IsReady = true;
    }

    public Vector2Int ToCell(Vector2 world)
    {
        return new Vector2Int(
            Mathf.FloorToInt((world.x - origin.x) / cellSize),
            Mathf.FloorToInt((world.y - origin.y) / cellSize));
    }

    public Vector2 CellCenter(Vector2Int cell)
    {
        return origin + new Vector2((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize);
    }

    public bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
    }

    public bool IsBlocked(Vector2Int c)
    {
        return !InBounds(c) || blocked[c.x, c.y];
    }

    public List<Vector2> FindPath(Vector2 from, Vector2 to)
    {
        if (!IsReady) return null;

        Vector2Int start = ToCell(from);
        Vector2Int goal = ToCell(to);
        if (!InBounds(start) || !InBounds(goal)) return null;
        if (blocked[start.x, start.y]) start = NearestFree(start, 8);
        if (blocked[goal.x, goal.y]) goal = NearestFree(goal, 8);
        if (blocked[start.x, start.y] || blocked[goal.x, goal.y]) return null;

        List<Vector2Int> cells = GridPathfinder.Find(blocked, width, height, start, goal);
        if (cells == null || cells.Count == 0) return null;

        List<Vector2> points = new List<Vector2>(cells.Count);
        Vector2 last = from;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2 p = CellCenter(cells[i]);
            points.Add(p);
            last = p;
        }
        points.Add(to);
        return Simplify(points);
    }

    Vector2Int NearestFree(Vector2Int cell, int radius)
    {
        if (!IsBlocked(cell)) return cell;
        for (int r = 1; r <= radius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    Vector2Int c = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (!IsBlocked(c)) return c;
                }
            }
        }
        return cell;
    }

    List<Vector2> Simplify(List<Vector2> points)
    {
        if (points.Count <= 2) return points;
        List<Vector2> result = new List<Vector2>();
        int i = 0;
        result.Add(points[0]);
        while (i < points.Count - 1)
        {
            int furthest = i + 1;
            for (int j = points.Count - 1; j > i; j--)
            {
                if (LineClear(points[i], points[j])) { furthest = j; break; }
            }
            result.Add(points[furthest]);
            i = furthest;
        }
        return result;
    }

    public bool LineClear(Vector2 a, Vector2 b)
    {
        float dist = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(dist / (cellSize * 0.5f));
        if (steps <= 0) return true;
        for (int i = 1; i < steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            if (IsBlocked(ToCell(p))) return false;
        }
        return true;
    }
}
