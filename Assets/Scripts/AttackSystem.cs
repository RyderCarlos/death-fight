using UnityEngine;

public class AttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    public float lightAttackDamage = 10f;
    public float heavyAttackDamage = 20f;
    public float attackRange = 1.5f;
    public LayerMask enemyMask = 1;
    public Transform attackPoint;
    
    [Header("Attack Timing")]
    public float lightAttackDuration = 0.3f;
    public float heavyAttackDuration = 0.6f;
    public float attackCooldown = 0.1f;
    
    private Animator animator;
    private PlayerController playerController;
    private bool isAttacking;
    private float lastAttackTime;
    private int comboCount;
    private float comboResetTime = 1f;
    private float lastComboTime;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        
        if (attackPoint == null)
        {
            attackPoint = new GameObject("AttackPoint").transform;
            attackPoint.SetParent(transform);
            attackPoint.localPosition = new Vector3(1f, 0, 0);
        }
    }
    
    private void Update()
    {
        GetAttackInput();
        UpdateCombo();
        UpdateAnimations();
    }
    
    private void GetAttackInput()
    {
        if (Time.time - lastAttackTime < attackCooldown || isAttacking)
            return;
            
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            PerformLightAttack();
        }
        else if (Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1))
        {
            PerformHeavyAttack();
        }
    }
    
    private void PerformLightAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        lastComboTime = Time.time;
        comboCount++;
        
        if (animator != null)
        {
            animator.SetTrigger("LightAttack");
            animator.SetInteger("ComboCount", comboCount);
        }
        
        Invoke(nameof(EndAttack), lightAttackDuration);
        DealDamage(lightAttackDamage);
    }
    
    private void PerformHeavyAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        lastComboTime = Time.time;
        comboCount = 0;
        
        if (animator != null)
        {
            animator.SetTrigger("HeavyAttack");
        }
        
        Invoke(nameof(EndAttack), heavyAttackDuration);
        DealDamage(heavyAttackDamage);
    }
    
    private void DealDamage(float damage)
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyMask);
        
        foreach (Collider2D enemy in enemies)
        {
            if (enemy.gameObject != gameObject)
            {
                HealthSystem healthSystem = enemy.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.TakeDamage(damage);
                }
                
                Debug.Log($"Hit {enemy.name} for {damage} damage!");
            }
        }
    }
    
    private void EndAttack()
    {
        isAttacking = false;
    }
    
    private void UpdateCombo()
    {
        if (Time.time - lastComboTime > comboResetTime)
        {
            comboCount = 0;
            if (animator != null)
                animator.SetInteger("ComboCount", 0);
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", isAttacking);
        }
    }
    
    public bool IsAttacking()
    {
        return isAttacking;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}