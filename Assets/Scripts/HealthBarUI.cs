using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private float fillSpeed = 5f; // Speed of the health drop animation

    private float targetFillAmount = 1f;

    private void OnEnable()
    {
        // Subscribe to the health change event
        PlayerHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        // Unsubscribe to clean up memory and prevent errors
        PlayerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void Update()
    {
        // Smoothly interpolate (lerp) the visual fill to match the target health value
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        // Calculate what fraction (between 0 and 1) the health bar should be filled
        targetFillAmount = currentHealth / maxHealth;
    }
}