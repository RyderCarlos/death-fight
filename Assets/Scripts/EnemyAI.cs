using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;
    public float attackCooldown = 2f;
    public float attackDamage = 15f;
    
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private HealthSystem healthSystem;
    private bool isAttacking;
    private float lastAttackTime;
    private bool facingRight = true;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = FindObjectOfType<PlayerController>()?.gameObject;
        }
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemy found player: {player.name}");
        }
        else
        {
            Debug.LogError("Enemy could not find player! Make sure player has 'Player' tag or PlayerController component.");
        }
        
        if (rb == null)
        {
            Debug.LogError("Enemy missing Rigidbody2D component!");
        }
        
        if (healthSystem == null)
        {
            Debug.LogWarning("Enemy missing HealthSystem component!");
        }
        else
        {
            healthSystem.OnDeath += OnDeath;
        }
    }
    
    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("Enemy: Player reference is null");
            return;
        }
        
        if (healthSystem != null && !healthSystem.IsAlive())
        {
            Debug.Log("Enemy is dead, stopping AI");
            return;
        }
            
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        Debug.Log($"Distance to player: {distanceToPlayer:F2}, Detection range: {detectionRange}");
        
        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer <= attackRange && !isAttacking)
            {
                Debug.Log("Enemy trying to attack");
                TryAttack();
            }
            else if (distanceToPlayer > attackRange)
            {
                Debug.Log("Enemy moving towards player");
                MoveTowardsPlayer();
            }
        }
        else
        {
            Debug.Log("Player out of detection range");
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        
        UpdateAnimations();
    }
    
    private void MoveTowardsPlayer()
    {
        if (rb == null)
        {
            Debug.LogError("Enemy Rigidbody2D is null!");
            return;
        }
        
        Vector2 direction = (player.position - transform.position).normalized;
        Vector2 newVelocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
        rb.velocity = newVelocity;
        
        Debug.Log($"Enemy moving with velocity: {newVelocity}, direction: {direction}");
        
        if (direction.x > 0 && !facingRight)
            Flip();
        else if (direction.x < 0 && facingRight)
            Flip();
    }
    
    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;
            
        PerformAttack();
    }
    
    private void PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, LayerMask.GetMask("Player"));
        if (playerCollider != null)
        {
            HealthSystem playerHealth = playerCollider.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
        
        Invoke(nameof(EndAttack), 0.5f);
    }
    
    private void EndAttack()
    {
        isAttacking = false;
    }
    
    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsAttacking", isAttacking);
        }
    }
    
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    private void OnDeath()
    {
        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
        
        Destroy(gameObject, 2f);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}