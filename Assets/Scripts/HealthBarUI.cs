using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider healthSlider;
    public Image fillImage;
    public Text healthText;
    
    [Header("Visual Settings")]
    public Color healthyColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float animationSpeed = 5f;
    public bool smoothAnimation = true;
    
    private float targetValue;
    private float currentDisplayValue;
    
    private void Start()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
            
        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();
            
        targetValue = healthSlider ? healthSlider.value : 1f;
        currentDisplayValue = targetValue;
    }
    
    private void Update()
    {
        if (smoothAnimation && healthSlider != null)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * animationSpeed);
            healthSlider.value = currentDisplayValue;
            
            if (Mathf.Abs(currentDisplayValue - targetValue) < 0.01f)
            {
                currentDisplayValue = targetValue;
                healthSlider.value = targetValue;
            }
        }
        
        UpdateHealthColor();
    }
    
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return;
        
        healthSlider.maxValue = maxHealth;
        
        if (smoothAnimation)
        {
            targetValue = currentHealth;
        }
        else
        {
            healthSlider.value = currentHealth;
            currentDisplayValue = currentHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    private void UpdateHealthColor()
    {
        if (fillImage == null || healthSlider == null) return;
        
        float healthPercentage = healthSlider.value / healthSlider.maxValue;
        
        Color targetColor;
        if (healthPercentage > 0.6f)
        {
            targetColor = Color.Lerp(midHealthColor, healthyColor, (healthPercentage - 0.6f) / 0.4f);
        }
        else if (healthPercentage > 0.3f)
        {
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, (healthPercentage - 0.3f) / 0.3f);
        }
        else
        {
            targetColor = lowHealthColor;
        }
        
        fillImage.color = targetColor;
    }
    
    public void SetHealthBarVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    public void FlashDamage()
    {
        if (fillImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }
    
    private System.Collections.IEnumerator FlashEffect()
    {
        Color originalColor = fillImage.color;
        fillImage.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        fillImage.color = originalColor;
    }
}