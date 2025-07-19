using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBar : MonoBehaviour
{
    [Header("UI组件")]
    public Image fillImage;
    public TextMeshProUGUI healthText;
    public Image backgroundImage;
    
    [Header("样式设置")]
    public HealthBarStyle style = HealthBarStyle.水平;
    public bool showText = true;
    public bool showPercentage = false;
    
    [Header("动画设置")]
    public bool smoothTransition = true;
    public float transitionSpeed = 2f;
    public bool flashOnDamage = true;
    public Color flashColor = Color.red;
    
    [Header("颜色设置")]
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.yellow;
    public Color criticalColor = Color.red;
    
    // 内部状态
    private float targetFillAmount = 1f;
    private HealthSystem targetHealthSystem;
    private Coroutine flashCoroutine;
    
    public enum HealthBarStyle
    {
        水平,
        垂直,
        圆形,
        分段
    }
    
    public void Initialize(HealthSystem healthSystem)
    {
        targetHealthSystem = healthSystem;
        
        if (targetHealthSystem != null)
        {
            // 订阅事件
            targetHealthSystem.OnTakeDamage += OnHealthChanged;
            targetHealthSystem.OnHeal += OnHealed;
            targetHealthSystem.OnDeath += OnDeath;
            targetHealthSystem.OnRevive += OnRevive;
            
            // 初始化显示
            UpdateDisplay();
        }
    }
    
    void Update()
    {
        if (smoothTransition && fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(
                fillImage.fillAmount, 
                targetFillAmount, 
                Time.deltaTime * transitionSpeed
            );
        }
    }
    
    void OnHealthChanged(DamageInfo damageInfo)
    {
        UpdateDisplay();
        
        if (flashOnDamage)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashEffect());
        }
    }
    
    void OnHealed(float healAmount)
    {
        UpdateDisplay();
    }
    
    void OnDeath()
    {
        UpdateDisplay();
        // 可以添加死亡特效
    }
    
    void OnRevive()
    {
        UpdateDisplay();
        // 可以添加复活特效
    }
    
    void UpdateDisplay()
    {
        if (targetHealthSystem == null) return;
        
        float healthPercentage = targetHealthSystem.GetHealthPercentage();
        
        // 更新填充
        if (smoothTransition)
        {
            targetFillAmount = healthPercentage;
        }
        else if (fillImage != null)
        {
            fillImage.fillAmount = healthPercentage;
        }
        
        // 更新颜色
        UpdateColor(healthPercentage);
        
        // 更新文本
        UpdateText();
    }
    
    void UpdateColor(float healthPercentage)
    {
        if (fillImage == null) return;
        
        Color targetColor;
        if (healthPercentage > 0.6f)
            targetColor = healthyColor;
        else if (healthPercentage > 0.3f)
            targetColor = damagedColor;
        else
            targetColor = criticalColor;
        
        fillImage.color = targetColor;
    }
    
    void UpdateText()
    {
        if (healthText == null || !showText) return;
        
        if (targetHealthSystem != null)
        {
            if (showPercentage)
            {
                float percentage = targetHealthSystem.GetHealthPercentage() * 100f;
                healthText.text = $"{percentage:F0}%";
            }
            else
            {
                healthText.text = $"{targetHealthSystem.currentHealth:F0}/{targetHealthSystem.maxHealth:F0}";
            }
        }
    }
    
    IEnumerator FlashEffect()
    {
        if (fillImage == null) yield break;
        
        Color originalColor = fillImage.color;
        fillImage.color = flashColor;
        
        yield return new WaitForSeconds(0.1f);
        
        fillImage.color = originalColor;
        flashCoroutine = null;
    }
    
    void OnDestroy()
    {
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnTakeDamage -= OnHealthChanged;
            targetHealthSystem.OnHeal -= OnHealed;
            targetHealthSystem.OnDeath -= OnDeath;
            targetHealthSystem.OnRevive -= OnRevive;
        }
    }
}