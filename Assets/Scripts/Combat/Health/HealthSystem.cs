using System.Collections;
using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("血量设置")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool autoRegeneration = false;
    public float regenRate = 5f; // 每秒恢复的血量
    
    [Header("无敌时间")]
    public float invincibilityTime = 0.5f;
    private bool isInvincible = false;
    
    [Header("硬直设置")]
    public float hitstunTime = 0.2f;
    private bool inHitstun = false;
    
    [Header("暴击设置")]
    public float criticalChance = 0.1f; // 10%暴击率
    public float criticalMultiplier = 1.5f;
    
    [Header("视觉效果")]
    public bool flashOnHit = true;
    public Color hitFlashColor = Color.red;
    public float flashDuration = 0.1f;
    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D rb2d;
    private Animator animator;
    
    // 状态枚举
    public enum HealthState
    {
        健康,    // 80-100%
        良好,    // 60-80%
        受伤,    // 40-60%
        危险,    // 20-40%
        濒死     // 0-20%
    }
    
    public HealthState currentHealthState { get; private set; }
    
    // 事件
    public event Action<DamageInfo> OnTakeDamage;
    public event Action<float> OnHeal;
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action<HealthState> OnHealthStateChanged;
    
    // 统计
    public float totalDamageTaken = 0f;
    public float totalHealing = 0f;
    public int deathCount = 0;
    
    void Start()
    {
        // 初始化血量
        currentHealth = maxHealth;
        
        // 获取组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // 初始化状态
        UpdateHealthState();
        
        // 注册到全局管理器
        HealthManager.Instance?.RegisterHealthSystem(this);
    }
    
    void Update()
    {
        // 自动血量恢复
        if (autoRegeneration && currentHealth < maxHealth && currentHealth > 0)
        {
            Heal(regenRate * Time.deltaTime);
        }
    }
    
    public void TakeDamage(DamageInfo damageInfo)
    {
        // 无敌状态或已死亡时不受伤害
        if (isInvincible || currentHealth <= 0)
        {
            return;
        }
        
        // 计算最终伤害
        damageInfo.CalculateFinalDamage();
        float finalDamage = damageInfo.finalDamage;
        
        // 应用伤害
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        totalDamageTaken += finalDamage;
        
        // 触发伤害事件
        OnTakeDamage?.Invoke(damageInfo);
        
        // 视觉效果
        if (flashOnHit)
        {
            StartCoroutine(FlashEffect());
        }
        
        // 击退效果
        ApplyKnockback(damageInfo);
        
        // 硬直效果
        if (damageInfo.finalDamage > 0)
        {
            StartCoroutine(ApplyHitstun(hitstunTime));
        }
        
        // 无敌时间
        StartCoroutine(InvincibilityCoroutine());
        
        // 更新血量状态
        UpdateHealthState();
        
        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
        
        // 动画触发
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
            if (damageInfo.isCritical)
            {
                animator.SetTrigger("CriticalHit");
            }
        }
        
        Debug.Log($"{gameObject.name} 受到 {finalDamage} 点伤害，剩余血量: {currentHealth}");
    }
    
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return; // 死亡状态不能恢复
        
        float healAmount = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += healAmount;
        totalHealing += healAmount;
        
        OnHeal?.Invoke(healAmount);
        UpdateHealthState();
        
        Debug.Log($"{gameObject.name} 恢复了 {healAmount} 点血量，当前血量: {currentHealth}");
    }
    
    public void Die()
    {
        currentHealth = 0;
        deathCount++;
        
        OnDeath?.Invoke();
        
        // 死亡视觉效果
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }
        
        // 死亡动画
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        Debug.Log($"{gameObject.name} 已死亡");
    }
    
    public void Revive(float healthPercentage = 1f)
    {
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        
        // 恢复颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        OnRevive?.Invoke();
        UpdateHealthState();
        
        Debug.Log($"{gameObject.name} 已复活，血量: {currentHealth}");
    }
    
    void UpdateHealthState()
    {
        HealthState newState;
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage >= 0.8f)
            newState = HealthState.健康;
        else if (healthPercentage >= 0.6f)
            newState = HealthState.良好;
        else if (healthPercentage >= 0.4f)
            newState = HealthState.受伤;
        else if (healthPercentage >= 0.2f)
            newState = HealthState.危险;
        else
            newState = HealthState.濒死;
        
        if (newState != currentHealthState)
        {
            currentHealthState = newState;
            OnHealthStateChanged?.Invoke(newState);
        }
    }
    
    void ApplyKnockback(DamageInfo damageInfo)
    {
        if (rb2d != null && damageInfo.knockbackForce > 0)
        {
            Vector2 knockbackDirection = damageInfo.knockbackDirection.normalized;
            rb2d.AddForce(knockbackDirection * damageInfo.knockbackForce, ForceMode2D.Impulse);
        }
    }
    
    IEnumerator ApplyHitstun(float duration)
    {
        inHitstun = true;
        
        // 暂停玩家输入或AI行为
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        yield return new WaitForSeconds(duration);
        
        // 恢复控制
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        inHitstun = false;
    }
    
    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }
    
    IEnumerator FlashEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    // 公共方法
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    public bool InHitstun()
    {
        return inHitstun;
    }
    
    void OnDestroy()
    {
        // 从全局管理器注销
        HealthManager.Instance?.UnregisterHealthSystem(this);
    }
    
    // Properties for compatibility
    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}