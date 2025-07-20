using UnityEngine;

public class PhysicsSetup : MonoBehaviour
{
    [Header("Physics Settings")]
    public bool preventHorizontalPush = true;
    public bool preventVerticalPush = true;
    public float maxPushForce = 0.01f;
    
    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private bool isPlayer;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
        isPlayer = GetComponent<PlayerController>() != null;
        
        SetupPhysicsMaterial();
        SetupRigidbodyConstraints();
    }
    
    private void SetupPhysicsMaterial()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.sharedMaterial == null)
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D("NoFriction");
            material.friction = 0f;
            material.bounciness = 0f;
            col.sharedMaterial = material;
            
            Debug.Log($"Applied no-friction material to {gameObject.name}");
        }
    }
    
    private void SetupRigidbodyConstraints()
    {
        if (rb != null)
        {
            rb.freezeRotation = true;
            
            if (preventHorizontalPush && preventVerticalPush)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            else if (preventHorizontalPush)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (preventHorizontalPush || preventVerticalPush)
        {
            PreventUnwantedMovement();
        }
    }
    
    private void PreventUnwantedMovement()
    {
        Vector2 currentPosition = transform.position;
        Vector2 movement = currentPosition - lastPosition;
        
        bool shouldPreventMovement = false;
        
        if (isPlayer)
        {
            PlayerController playerController = GetComponent<PlayerController>();
            AttackSystem attackSystem = GetComponent<AttackSystem>();
            
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            bool isAttacking = attackSystem != null && attackSystem.IsAttacking();
            
            if (Mathf.Abs(movement.x) > maxPushForce && Mathf.Abs(horizontalInput) < 0.1f && !isAttacking)
            {
                shouldPreventMovement = true;
            }
        }
        else
        {
            EnemyAI enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                Vector2 intendedVelocity = rb.velocity;
                if (Mathf.Abs(movement.x) > maxPushForce && Mathf.Abs(intendedVelocity.x) < 0.1f)
                {
                    shouldPreventMovement = true;
                }
            }
        }
        
        if (shouldPreventMovement)
        {
            Vector2 correctedPosition = lastPosition;
            if (!preventVerticalPush)
            {
                correctedPosition.y = currentPosition.y;
            }
            
            transform.position = correctedPosition;
            
            if (preventHorizontalPush)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        
        lastPosition = transform.position;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollisionPrevention(collision);
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollisionPrevention(collision);
    }
    
    private void HandleCollisionPrevention(Collision2D collision)
    {
        if (preventHorizontalPush)
        {
            GameObject other = collision.gameObject;
            
            bool isCharacterCollision = (
                other.GetComponent<PlayerController>() != null ||
                other.GetComponent<EnemyAI>() != null
            );
            
            if (isCharacterCollision)
            {
                Vector2 pushDirection = collision.contacts[0].normal;
                
                if (Mathf.Abs(pushDirection.x) > 0.1f || Mathf.Abs(pushDirection.y) > 0.1f)
                {
                    rb.velocity = Vector2.zero;
                    
                    Vector2 separation = pushDirection * 0.05f;
                    if (preventVerticalPush) separation.y = 0;
                    if (preventHorizontalPush) separation.x = 0;
                    
                    if (separation != Vector2.zero)
                        transform.position += (Vector3)separation;
                }
            }
        }
    }
}