using System;
using UnityEngine;

/// <summary>
/// Vida compartida por jugador, enemigos y jefe.
/// El jugador NO se destruye al morir (destroyOnDeath = false) para que el
/// GameManager pueda mostrar la pantalla de derrota y reiniciar la escena.
/// </summary>
public class Health : MonoBehaviour
{
    public int maxHealth = 30;

    [Tooltip("Si es false, el objeto no se destruye al morir (jugador).")]
    public bool destroyOnDeath = true;

    [Tooltip("Marcar solo en el jugador: activa la persistencia entre escenarios.")]
    public bool isPlayer = false;

    [Tooltip("Tiempo de invulnerabilidad tras recibir un golpe (segundos).")]
    public float invulnerabilityTime = 0.35f;

    [Tooltip("Puntos de escudo absorbente comprados en la aldea (se gastan antes que la vida).")]
    public int shieldPoints = 0;

    public event Action<Health> OnHealthChanged;
    public event Action<Health> OnDamaged;
    public event Action<Health> OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth { get { return Mathf.Max(1, maxHealth); } }
    public bool IsAlive { get { return CurrentHealth > 0; } }
    public int ShieldPoints { get { return shieldPoints; } }

    private float lastHitTime = -999f;
    private float invulnerableUntil = -999f;
    private bool dead;

    void Awake()
    {
        if (CurrentHealth <= 0) CurrentHealth = MaxHealth;
    }

    void Start()
    {
        if (CurrentHealth <= 0) CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(this);
    }

    public void SetHealth(int value, bool notify = true)
    {
        CurrentHealth = Mathf.Clamp(value, 0, MaxHealth);
        dead = CurrentHealth <= 0;
        if (notify) OnHealthChanged?.Invoke(this);
    }

    public void ResetHealth()
    {
        dead = false;
        CurrentHealth = MaxHealth;
        lastHitTime = -999f;
        invulnerableUntil = -999f;
        OnHealthChanged?.Invoke(this);
    }

    /// <summary>Escudo absorbente: se consume antes que la vida.</summary>
    public void AddShield(int amount)
    {
        if (amount <= 0) return;
        shieldPoints += amount;
        OnHealthChanged?.Invoke(this);
    }

    /// <summary>Invulnerabilidad concedida por una habilidad (esquiva).</summary>
    public void GrantInvulnerability(float seconds)
    {
        if (seconds <= 0f) return;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    /// <summary>Devuelve true si el daño se aplicó realmente.</summary>
    public bool TakeDamage(int damage)
    {
        if (damage <= 0 || dead || !IsAlive) return false;
        if (Time.time < invulnerableUntil) return false;
        if (invulnerabilityTime > 0f && Time.time - lastHitTime < invulnerabilityTime) return false;

        lastHitTime = Time.time;

        if (shieldPoints > 0)
        {
            int absorbed = Mathf.Min(shieldPoints, damage);
            shieldPoints -= absorbed;
            damage -= absorbed;
            OnHealthChanged?.Invoke(this);
            if (damage <= 0) return true;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnDamaged?.Invoke(this);
        OnHealthChanged?.Invoke(this);

        if (CurrentHealth <= 0) Die();
        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || dead) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(this);
    }

    void Die()
    {
        if (dead) return;

        // La vida extra comprada a un aldeano reanima al jugador en el sitio: no se
        // dispara OnDied, así que la pantalla de derrota nunca llega a abrirse.
        if (isPlayer && PlayerBuffs.ConsumeExtraLife())
        {
            CurrentHealth = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * 0.6f));
            lastHitTime = Time.time;
            invulnerableUntil = Time.time + 2f;
            OnHealthChanged?.Invoke(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
            if (UIRoot.Instance != null)
                UIRoot.Instance.Toast("El sello te sostiene · vidas extra restantes: " + PlayerBuffs.ExtraLives);
            return;
        }

        dead = true;
        OnDied?.Invoke(this);
        if (destroyOnDeath) Destroy(gameObject, 0.05f);
    }
}
