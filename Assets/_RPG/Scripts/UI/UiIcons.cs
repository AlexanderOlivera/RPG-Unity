using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Iconos procedurales para la mochila. Se dibujan en tiempo de ejecución
/// (32x32, filtro Point) para no depender de assets externos ni de Resources.
/// </summary>
public static class UiIcons
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    /// <summary>Nombre de inventario -> clave de icono.</summary>
    public static string KeyFor(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return "item";
        switch (itemName)
        {
            case InventorySystem.CoinItem: return "coin";
            case InventorySystem.GemItem: return "gem";
            case InventorySystem.HeartItem: return "heart";
            case InventorySystem.FragmentItem: return "fragment";
            case "Venda de hierbas": return "heart";
            case "Escudo de corteza": return "shield";
            case "Poción de bruma": return "boot";
            case "Amuleto del sello": return "sword";
            case "Amuleto de la segunda vida": return "star";
            case "Bendición del sello": return "star";
            case "Sello de reserva": return "star";
            case "Filo del bosque": return "sword";
            case "Filo forjado": return "sword";
            default: return "item";
        }
    }

    public static Sprite Get(string key)
    {
        if (string.IsNullOrEmpty(key)) key = "item";
        Sprite cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        Sprite built = Build(key);
        cache[key] = built;
        return built;
    }

    static Sprite Build(string key)
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Paint(key, (x + 0.5f) / size, (y + 0.5f) / size));
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = "Icon_" + key;
        return sprite;
    }

    static Color Paint(string key, float u, float v)
    {
        Vector2 p = new Vector2(u, v);

        switch (key)
        {
            case "coin":
            {
                float d = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
                if (d > 0.46f) return Clear;
                float k = d > 0.36f ? 0.62f : 1f;
                if (d < 0.13f) k = 1.2f;
                return new Color(k, k * 0.84f, k * 0.22f, 1f);
            }

            case "gem":
            {
                float d = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f) * 0.82f;
                if (d > 0.45f) return Clear;
                float k = d > 0.30f ? 0.58f : 1f;
                return new Color(0.35f * k, 0.92f * k, 1f, 1f);
            }

            case "heart":
            {
                float d = Mathf.Min(Vector2.Distance(p, new Vector2(0.33f, 0.63f)),
                                    Vector2.Distance(p, new Vector2(0.67f, 0.63f)));
                bool triangle = v < 0.63f && Mathf.Abs(u - 0.5f) < (v - 0.08f) * 0.62f;
                if (d > 0.21f && !triangle) return Clear;
                return new Color(0.95f, 0.28f, 0.36f, 1f);
            }

            case "fragment":
            {
                float d = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
                if (Mathf.Abs(d - 0.32f) > 0.11f) return Clear;
                return new Color(0.55f, 1f, 0.66f, 1f);
            }

            case "potion":
            {
                bool body = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.34f)) < 0.27f;
                bool neck = Mathf.Abs(u - 0.5f) < 0.09f && v > 0.45f && v < 0.82f;
                bool cork = Mathf.Abs(u - 0.5f) < 0.12f && v >= 0.82f && v < 0.9f;
                if (cork) return new Color(0.62f, 0.44f, 0.26f, 1f);
                if (neck) return new Color(0.62f, 0.86f, 0.95f, 0.85f);
                if (body) return new Color(0.35f, 0.78f, 0.95f, 1f);
                return Clear;
            }

            case "shield":
            {
                float halfWidth = 0.34f - Mathf.Max(0f, v - 0.42f) * 0.75f;
                bool inside = Mathf.Abs(u - 0.5f) < halfWidth && v > 0.16f && v < 0.86f;
                if (!inside) return Clear;
                float edge = Mathf.Min(halfWidth - Mathf.Abs(u - 0.5f), Mathf.Min(v - 0.16f, 0.86f - v));
                return edge < 0.06f ? new Color(0.30f, 0.45f, 0.62f, 1f) : new Color(0.52f, 0.72f, 0.92f, 1f);
            }

            case "boot":
            {
                bool leg = Mathf.Abs(u - 0.40f) < 0.13f && v > 0.30f && v < 0.84f;
                bool foot = v >= 0.16f && v < 0.34f && u > 0.27f && u < 0.76f;
                if (!leg && !foot) return Clear;
                return new Color(0.66f, 0.88f, 1f, 1f);
            }

            case "sword":
            {
                bool blade = Mathf.Abs(u - 0.5f) < 0.07f && v > 0.36f && v < 0.88f;
                bool guard = Mathf.Abs(v - 0.35f) < 0.05f && u > 0.28f && u < 0.72f;
                bool grip = Mathf.Abs(u - 0.5f) < 0.06f && v > 0.12f && v < 0.30f;
                if (blade) return new Color(0.92f, 0.94f, 1f, 1f);
                if (guard) return new Color(0.85f, 0.68f, 0.32f, 1f);
                if (grip) return new Color(0.55f, 0.38f, 0.24f, 1f);
                return Clear;
            }

            case "star":
            {
                bool horizontal = Mathf.Abs(v - 0.5f) < 0.07f && Mathf.Abs(u - 0.5f) < 0.36f;
                bool vertical = Mathf.Abs(u - 0.5f) < 0.07f && Mathf.Abs(v - 0.5f) < 0.36f;
                bool core = Vector2.Distance(p, new Vector2(0.5f, 0.5f)) < 0.15f;
                if (!horizontal && !vertical && !core) return Clear;
                return new Color(1f, 0.92f, 0.55f, 1f);
            }

            default:
            {
                bool inside = u > 0.18f && u < 0.82f && v > 0.18f && v < 0.82f;
                if (!inside) return Clear;
                return new Color(0.78f, 0.74f, 0.62f, 1f);
            }
        }
    }
}
