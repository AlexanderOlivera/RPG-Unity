using System.Collections.Generic;

/// <summary>
/// Estado de la partida que sobrevive a los cambios de escena:
/// puntaje, monedas, gemas, enemigos derrotados, fragmentos e inventario.
/// </summary>
public static class GameSession
{
    public const int DefaultPlayerMaxHealth = 100;

    public static int Score;
    public static int Coins;
    public static int Gems;
    public static int Kills;
    public static int Fragments;
    public static int CoinsSpent;
    public static int Trades;
    public static int NpcsTalked;
    public static int PlayerMaxHealth = DefaultPlayerMaxHealth;
    public static int PlayerHealth = DefaultPlayerMaxHealth;
    public static bool BossDefeated;
    public static bool Won;

    public static readonly Dictionary<string, int> Inventory = new Dictionary<string, int>();

    static GameSession()
    {
        Reset();
    }

    public static void Reset()
    {
        Score = 0;
        Coins = 0;
        Gems = 0;
        Kills = 0;
        Fragments = 0;
        CoinsSpent = 0;
        Trades = 0;
        NpcsTalked = 0;
        BossDefeated = false;
        Won = false;
        PlayerMaxHealth = DefaultPlayerMaxHealth;
        PlayerHealth = DefaultPlayerMaxHealth;
        Inventory.Clear();
        PlayerBuffs.Reset();
        Difficulty.Set(0);
    }

    /// <summary>Gasta monedas (comercio). Devuelve false si no alcanza.</summary>
    public static bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;
        Coins -= amount;
        CoinsSpent += amount;
        NotifyChangedSafe();
        return true;
    }

    static void NotifyChangedSafe()
    {
        GameManager.NotifyChanged();
    }

    public static void AddScore(int points)
    {
        if (points <= 0) return;
        Score += points;
        GameManager.NotifyChanged();
    }

    public static void RegisterKill()
    {
        Kills++;
        GameManager.NotifyChanged();
    }

    public static void AddItem(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0) return;
        int current;
        Inventory.TryGetValue(itemName, out current);
        Inventory[itemName] = current + amount;
        GameManager.NotifyChanged();
    }

    public static int GetItemCount(string itemName)
    {
        int value;
        if (string.IsNullOrEmpty(itemName)) return 0;
        Inventory.TryGetValue(itemName, out value);
        return value;
    }

    public static string InventorySummary()
    {
        if (Inventory.Count == 0) return "Mochila vacía";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (KeyValuePair<string, int> kv in Inventory)
            sb.Append(kv.Key).Append(" x").Append(kv.Value).Append("\n");
        return sb.ToString().TrimEnd();
    }
}

/// <summary>Puntos únicos del juego (una sola fuente para no duplicar valores).</summary>
public static class ScoreSystem
{
    public const int CoinPoints = 10;
    public const int GemPoints = 50;
    public const int FragmentPoints = 200;

    public static void AwardCoin()
    {
        GameSession.Coins++;
        GameSession.AddScore(CoinPoints);
    }

    public static void AwardGem()
    {
        GameSession.Gems++;
        GameSession.AddScore(GemPoints);
    }

    public static void AwardFragment()
    {
        GameSession.Fragments++;
        GameSession.AddScore(FragmentPoints);
    }

    public static void AwardKill(int points)
    {
        GameSession.AddScore(points);
    }
}

/// <summary>Fachada del inventario (usada por coleccionables y por la UI).</summary>
public static class InventorySystem
{
    public const string CoinItem = "Moneda";
    public const string GemItem = "Gema";
    public const string HeartItem = "Corazón";
    public const string FragmentItem = "Fragmento del Sello";

    public static void AddCoin() { GameSession.AddItem(CoinItem, 1); }
    public static void AddGem() { GameSession.AddItem(GemItem, 1); }
    public static void AddHeart() { GameSession.AddItem(HeartItem, 1); }
    public static void AddFragment() { GameSession.AddItem(FragmentItem, 1); }

    public static int Count(string item) { return GameSession.GetItemCount(item); }
    public static string Describe() { return GameSession.InventorySummary(); }
}

/// <summary>Dificultad opcional: multiplica daño enemigo y vida enemiga.</summary>
public static class Difficulty
{
    public static string Name = "Normal";
    public static float DamageMultiplier = 1f;
    public static float EnemyHealthMultiplier = 1f;

    public static void Set(int level)
    {
        if (level <= 0)
        {
            Name = "Normal";
            DamageMultiplier = 1f;
            EnemyHealthMultiplier = 1f;
        }
        else
        {
            Name = "Difícil";
            DamageMultiplier = 1.5f;
            EnemyHealthMultiplier = 1.6f;
        }
    }
}
