using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("生命值设置")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isInvulnerable = false;
    
    [Header("死亡设置")]
    public UnityEvent onDeath;
    public GameObject deathEffect;
    
    [Header("引用")]
    private DefenseSystem defenseSystem;
    private Animator animator;
    
    public UnityEvent<int> OnDamageTaken;
    public UnityEvent<int> OnHealthChanged;
    
    void Start() {
        currentHealth = maxHealth;
        defenseSystem = GetComponent<DefenseSystem>();
        animator = GetComponent<Animator>();
    }
    
    public void TakeDamage(int damage) {
        if (isInvulnerable || currentHealth <= 0) return;
        
        // 应用防御系统
        float finalDamage = damage;
        if (defenseSystem != null) {
            finalDamage = defenseSystem.ProcessDamage(damage);
        }
        
        int intDamage = Mathf.RoundToInt(finalDamage);
        currentHealth -= intDamage;
        
        // 触发事件
        OnDamageTaken.Invoke(intDamage);
        OnHealthChanged.Invoke(currentHealth);
        
        // 受伤动画
        if (intDamage > 0) {
            animator.SetTrigger("Hurt");
        }
        
        // 检查死亡
        if (currentHealth <= 0) {
            Die();
        }
    }
    
    public void Heal(int amount) {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged.Invoke(currentHealth);
    }
    
    private void Die() {
        // 死亡动画
        animator.SetBool("IsDead", true);
        
        // 禁用组件
        GetComponent<PlayerController>().enabled = false;
        GetComponent<PlayerCombat>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
        
        // 死亡特效
        if (deathEffect != null) {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // 触发事件
        onDeath.Invoke();
    }
    
    public float GetHealthPercentage() {
        return (float)currentHealth / maxHealth;
    }
}