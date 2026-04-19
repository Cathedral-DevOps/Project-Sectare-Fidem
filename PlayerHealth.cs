/*
PlayerHealth.cs
Attach to the player GameObject.
Provides: TakeDamage, Heal, Kill, Respawn, invulnerability, and UnityEvents for hooking UI or gameplay.

Usage:
- Call `GetComponent<PlayerHealth>().TakeDamage(10)` from enemies/traps.
- Hook `onDeath` / `onRespawn` / `onDamaged` / `onHealed` to UI or game logic in the inspector.
- Optionally set `disableOnDeath` to disable movement scripts on death and `respawnPoint` to control respawn location.
*/

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }
    [System.Serializable]
    public class HealthChangedEvent : UnityEvent<int, int> { }

    [Header("Health")]
    [Tooltip("Maximum health value.")]
    public int maxHealth = 100;
    [Tooltip("Start health. Set to 0 to start at maxHealth.")]
    public int startHealth = 0;
    [SerializeField]
    private int currentHealth;

    [Header("Death & Respawn")]
    public bool respawnEnabled = true;
    public Transform respawnPoint;
    public float respawnDelay = 3f;
    [Tooltip("MonoBehaviours to disable on death (do not include this script).")]
    public MonoBehaviour[] disableOnDeath;
    [Tooltip("GameObjects to deactivate on death (e.g., weapon models, HUD elements).")]
    public GameObject[] disableGameObjectsOnDeath;

    [Header("Invulnerability")]
    [Tooltip("Seconds of invulnerability after taking damage.")]
    public float invulnerabilityDuration = 0.5f;
    [Tooltip("Enable short invulnerability after respawn.")]
    public bool invulOnRespawn = true;
    public float respawnInvulnerability = 1.5f;

    [Header("Events")]
    public IntEvent onDamaged;
    public IntEvent onHealed;
    public UnityEvent onDeath;
    public UnityEvent onRespawn;
    public HealthChangedEvent onHealthChanged; // (current, max)

    bool isDead;
    bool isInvulnerable;
    Vector3 spawnPosition;
    Quaternion spawnRotation;
    Coroutine respawnCoroutine;

    void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Start()
    {
        currentHealth = (startHealth > 0) ? Mathf.Clamp(startHealth, 0, maxHealth) : maxHealth;
        isDead = currentHealth <= 0;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || isDead || isInvulnerable) return false;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        onDamaged?.Invoke(amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0) Die();
        else if (invulnerabilityDuration > 0f) StartCoroutine(TemporaryInvulnerability(invulnerabilityDuration));
        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        onHealed?.Invoke(amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Kill()
    {
        if (isDead) return;
        TakeDamage(maxHealth);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        onDeath?.Invoke();

        if (disableOnDeath != null)
        {
            foreach (var mb in disableOnDeath)
            {
                if (mb != null && mb != this) mb.enabled = false;
            }
        }

        if (disableGameObjectsOnDeath != null)
        {
            foreach (var go in disableGameObjectsOnDeath)
            {
                if (go != null) go.SetActive(false);
            }
        }

        if (respawnEnabled)
        {
            if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
            respawnCoroutine = StartCoroutine(RespawnCoroutine(respawnDelay));
        }
    }

    IEnumerator RespawnCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;

        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }

        if (disableOnDeath != null)
        {
            foreach (var mb in disableOnDeath)
            {
                if (mb != null) mb.enabled = true;
            }
        }

        if (disableGameObjectsOnDeath != null)
        {
            foreach (var go in disableGameObjectsOnDeath)
            {
                if (go != null) go.SetActive(true);
            }
        }

        onRespawn?.Invoke();
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (invulOnRespawn && respawnInvulnerability > 0f)
            StartCoroutine(TemporaryInvulnerability(respawnInvulnerability));
    }

    IEnumerator TemporaryInvulnerability(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    public void SetRespawnPoint(Transform t) { respawnPoint = t; }

    void OnValidate()
    {
        if (maxHealth < 1) maxHealth = 1;
        if (startHealth < 0) startHealth = 0;
        if (currentHealth < 0) currentHealth = 0;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    [ContextMenu("Debug Damage 10")]
    void DebugDamage10() { TakeDamage(10); }

    [ContextMenu("Debug Heal 10")]
    void DebugHeal10() { Heal(10); }
}
