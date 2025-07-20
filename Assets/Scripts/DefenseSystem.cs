using UnityEngine;

public class DefenseSystem : MonoBehaviour
{
    [Header("Defense Settings")]
    public float blockDamageReduction = 0.8f;
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.5f;
    public float dodgeCooldown = 1f;
    public float blockStamina = 100f;
    public float blockStaminaDrain = 20f;
    
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private AttackSystem attackSystem;
    
    private bool isBlocking;
    private bool isDodging;
    private float currentStamina;
    private float lastDodgeTime;
    private Vector2 dodgeDirection;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        attackSystem = GetComponent<AttackSystem>();
        
        currentStamina = blockStamina;
    }
    
    private void Update()
    {
        GetDefenseInput();
        UpdateStamina();
        UpdateAnimations();
    }
    
    private void GetDefenseInput()
    {
        if (isDodging)
            return;
            
        bool blockInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (blockInput && currentStamina > 0 && !attackSystem.IsAttacking())
        {
            StartBlocking();
        }
        else
        {
            StopBlocking();
        }
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            TryDodge();
        }
    }
    
    private void StartBlocking()
    {
        if (!isBlocking)
        {
            isBlocking = true;
        }
    }
    
    private void StopBlocking()
    {
        if (isBlocking)
        {
            isBlocking = false;
        }
    }
    
    private void TryDodge()
    {
        if (Time.time - lastDodgeTime < dodgeCooldown || isDodging)
            return;
            
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        dodgeDirection = new Vector2(horizontal, vertical).normalized;
        
        if (dodgeDirection.magnitude < 0.1f)
        {
            dodgeDirection = new Vector2(-transform.localScale.x, 0).normalized;
        }
        
        PerformDodge();
    }
    
    private void PerformDodge()
    {
        isDodging = true;
        lastDodgeTime = Time.time;
        
        if (animator != null)
        {
            animator.SetTrigger("Dodge");
        }
        
        rb.velocity = dodgeDirection * (dodgeDistance / dodgeDuration);
        
        Invoke(nameof(EndDodge), dodgeDuration);
    }
    
    private void EndDodge()
    {
        isDodging = false;
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
    
    private void UpdateStamina()
    {
        if (isBlocking)
        {
            currentStamina -= blockStaminaDrain * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                StopBlocking();
            }
        }
        else
        {
            currentStamina += blockStaminaDrain * 0.5f * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, blockStamina);
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("IsBlocking", isBlocking);
            animator.SetBool("IsDodging", isDodging);
        }
    }
    
    public bool TryBlockDamage(float incomingDamage, out float finalDamage)
    {
        if (isBlocking && currentStamina > 0)
        {
            finalDamage = incomingDamage * (1f - blockDamageReduction);
            currentStamina -= incomingDamage * 0.5f;
            
            if (animator != null)
            {
                animator.SetTrigger("BlockHit");
            }
            
            return true;
        }
        
        finalDamage = incomingDamage;
        return false;
    }
    
    public bool IsBlocking()
    {
        return isBlocking;
    }
    
    public bool IsDodging()
    {
        return isDodging;
    }
    
    public float GetStaminaPercentage()
    {
        return currentStamina / blockStamina;
    }
}