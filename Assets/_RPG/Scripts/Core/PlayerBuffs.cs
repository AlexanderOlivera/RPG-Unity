using UnityEngine;

/// <summary>Efecto real de un objeto comprado a un NPC.</summary>
public enum TradeEffect
{
    Heal = 0,
    Shield = 1,
    Haste = 2,
    Might = 3,
    Vitality = 4,
    ExtraLife = 5,
    Power = 6
}

/// <summary>
/// Mejoras del jugador compradas con monedas. Las monedas dejan de ser un
/// contador decorativo: cada aldeano vende una mejora con efecto comprobable
/// (curación, escudo, velocidad, daño, vida máxima, una vida extra o más daño
/// permanente en los ataques).
/// </summary>
public static class PlayerBuffs
{
    public const float HasteMultiplier = 1.35f;
    public const float MightMultiplier = 1.45f;

    /// <summary>Segundos restantes de la prisa comprada (velocidad +35%).</summary>
    public static float HasteRemaining;

    /// <summary>Segundos restantes de la furia comprada (daño +45%).</summary>
    public static float MightRemaining;

    /// <summary>Reanimaciones que concede el sello al llegar a 0 de vida.</summary>
    public static int ExtraLives;

    /// <summary>Daño permanente acumulado como fracción (0.15 = +15%).</summary>
    public static float PermanentDamage;

    public static float SpeedMultiplier { get { return HasteRemaining > 0f ? HasteMultiplier : 1f; } }

    /// <summary>Multiplicador total de ataque: mejoras permanentes × furia temporal.</summary>
    public static float DamageMultiplier
    {
        get
        {
            float value = 1f + Mathf.Max(0f, PermanentDamage);
            if (MightRemaining > 0f) value *= MightMultiplier;
            return value;
        }
    }

    public static int DamageBonusPercent { get { return Mathf.RoundToInt(Mathf.Max(0f, PermanentDamage) * 100f); } }

    public static void Reset()
    {
        HasteRemaining = 0f;
        MightRemaining = 0f;
        ExtraLives = 0;
        PermanentDamage = 0f;
    }

    public static void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (HasteRemaining > 0f) HasteRemaining = Mathf.Max(0f, HasteRemaining - deltaTime);
        if (MightRemaining > 0f) MightRemaining = Mathf.Max(0f, MightRemaining - deltaTime);
    }

    /// <summary>Consume una reanimación. Devuelve false si no quedaban.</summary>
    public static bool ConsumeExtraLife()
    {
        if (ExtraLives <= 0) return false;
        ExtraLives--;
        return true;
    }

    public static Health PlayerHealth()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        return player != null ? player.GetComponent<Health>() : null;
    }

    /// <summary>Aplica el efecto y devuelve el texto que se muestra al jugador.</summary>
    public static string Apply(TradeEffect effect, int power)
    {
        Health h = PlayerHealth();
        switch (effect)
        {
            case TradeEffect.Heal:
                if (h != null) h.Heal(power);
                return "vida +" + power;

            case TradeEffect.Shield:
                if (h != null) h.AddShield(power);
                return "escudo " + power;

            case TradeEffect.Haste:
                HasteRemaining = Mathf.Max(HasteRemaining, power);
                return "velocidad +35% (" + power + "s)";

            case TradeEffect.Might:
                MightRemaining = Mathf.Max(MightRemaining, power);
                return "daño +45% (" + power + "s)";

            case TradeEffect.ExtraLife:
                ExtraLives += Mathf.Max(1, power);
                return "vida extra · revives al caer (total " + ExtraLives + ")";

            case TradeEffect.Power:
                PermanentDamage += Mathf.Max(1, power) / 100f;
                return "daño de tus ataques +" + Mathf.Max(1, power) + "% para siempre (total +" + DamageBonusPercent + "%)";

            default:
                if (h != null)
                {
                    h.maxHealth += power;
                    h.Heal(power);
                }
                // La mejora es permanente: también debe sobrevivir al cambio de escena
                // (GameManager restaura maxHealth desde GameSession al cargar).
                GameSession.PlayerMaxHealth += power;
                return "vida máxima +" + power;
        }
    }

    /// <summary>Resumen corto para el HUD (vacío si no hay mejoras activas).</summary>
    public static string Summary()
    {
        string result = string.Empty;

        if (HasteRemaining > 0f)
            result = "Bruma " + Mathf.CeilToInt(HasteRemaining) + "s";

        if (MightRemaining > 0f)
            result += (result.Length > 0 ? " · " : "") + "Furia " + Mathf.CeilToInt(MightRemaining) + "s";

        if (PermanentDamage > 0.0001f)
            result += (result.Length > 0 ? " · " : "") + "Filo +" + DamageBonusPercent + "%";

        if (ExtraLives > 0)
            result += (result.Length > 0 ? " · " : "") + "Vidas extra " + ExtraLives;

        return result;
    }
}
