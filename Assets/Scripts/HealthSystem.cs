using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public bool canRegenerate = false;
    public float regenerationRate = 5f;
    
    private float currentHealth;
    private DefenseSystem defenseSystem;
    private Animator animator;
    
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    
    private void Start()
    {
        currentHealth = maxHealth;
        defenseSystem = GetComponent<DefenseSystem>();
        animator = GetComponent<Animator>();
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void Update()
    {
        if (canRegenerate && currentHealth < maxHealth)
        {
            Heal(regenerationRate * Time.deltaTime);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0)
            return;
            
        float finalDamage = damage;
        
        if (defenseSystem != null)
        {
            bool blocked = defenseSystem.TryBlockDamage(damage, out finalDamage);
            
            if (blocked)
            {
                Debug.Log($"Blocked! Reduced damage from {damage} to {finalDamage}");
            }
        }
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (animator != null && currentHealth > 0)
        {
            animator.SetTrigger("Hurt");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"{gameObject.name} took {finalDamage} damage. Health: {currentHealth}/{maxHealth}");
    }
    
    public void Heal(float amount)
    {
        if (currentHealth >= maxHealth)
            return;
            
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
    }
    
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        OnDeath?.Invoke();
        
        PlayerController playerController = GetComponent<PlayerController>();
        AttackSystem attackSystem = GetComponent<AttackSystem>();
        DefenseSystem defenseSystem = GetComponent<DefenseSystem>();
        
        if (playerController != null) playerController.enabled = false;
        if (attackSystem != null) attackSystem.enabled = false;
        if (defenseSystem != null) defenseSystem.enabled = false;
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}