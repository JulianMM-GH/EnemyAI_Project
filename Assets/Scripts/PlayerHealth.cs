using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("I-Frame Settings")]
    [SerializeField] private float invulnerabilityDuration = 1.0f;

    private float currentHealth;
    private float iFrameTimer = 0f;

    public static event Action<float, float> OnHealthChanged; // passes (currentHealth, maxHealth)
    public static event Action OnPlayerDeath;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => iFrameTimer > 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;

        // Push initial health values to the UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        // Count down i-frame timer
        if (iFrameTimer > 0f)
        {
            iFrameTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(float amount)
    {
        // Block damage if dead or currently in i-frames
        if (IsDead || IsInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Trigger UI update event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Start i-frame protection
        iFrameTimer = invulnerabilityDuration;

        Debug.Log($"Player took {amount} damage! I-Frames active. Current Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        OnPlayerDeath?.Invoke();
        Debug.Log("Player has died!");

        if (TryGetComponent<PlayerController>(out var controller))
        {
            controller.enabled = false;
        }
    }
}
