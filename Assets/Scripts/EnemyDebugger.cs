using UnityEngine;

public class EnemyDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    public bool showGizmosInGame = true;
    
    private EnemyAI enemyAI;
    private Rigidbody2D rb;
    private HealthSystem health;
    private Transform player;
    
    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<HealthSystem>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = FindObjectOfType<PlayerController>()?.gameObject;
        }
        
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        PerformDiagnostics();
    }
    
    private void PerformDiagnostics()
    {
        Debug.Log("=== Enemy Diagnostics ===");
        
        Debug.Log($"Enemy GameObject: {gameObject.name}");
        Debug.Log($"Position: {transform.position}");
        
        Debug.Log($"EnemyAI Component: {(enemyAI != null ? "✓" : "✗")}");
        Debug.Log($"Rigidbody2D Component: {(rb != null ? "✓" : "✗")}");
        Debug.Log($"HealthSystem Component: {(health != null ? "✓" : "✗")}");
        Debug.Log($"Collider2D Component: {(GetComponent<Collider2D>() != null ? "✓" : "✗")}");
        
        if (rb != null)
        {
            Debug.Log($"Rigidbody2D frozen: {rb.freezeRotation}");
            Debug.Log($"Rigidbody2D constraints: {rb.constraints}");
            Debug.Log($"Rigidbody2D mass: {rb.mass}");
            Debug.Log($"Rigidbody2D gravity scale: {rb.gravityScale}");
        }
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            Debug.Log($"Player found: {player.name}");
            Debug.Log($"Distance to player: {distance:F2}");
        }
        else
        {
            Debug.LogError("Player not found! Check Player tag or PlayerController component.");
        }
        
        if (enemyAI != null)
        {
            Debug.Log($"Detection Range: {enemyAI.detectionRange}");
            Debug.Log($"Attack Range: {enemyAI.attackRange}");
            Debug.Log($"Move Speed: {enemyAI.moveSpeed}");
        }
        
        Debug.Log("=== End Diagnostics ===");
    }
    
    private void Update()
    {
        if (showDebugInfo && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            
            if (rb != null)
            {
                Debug.Log($"Enemy velocity: {rb.velocity}, Distance: {distance:F2}");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showGizmosInGame && enemyAI != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyAI.detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyAI.attackRange);
            
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}