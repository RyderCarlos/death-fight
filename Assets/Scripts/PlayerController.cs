using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float crouchSpeedMultiplier = 0.5f;
    
    [Header("Physics")]
    public LayerMask groundMask = 1;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D bodyCollider;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    
    private bool isGrounded;
    private bool isCrouching;
    private bool facingRight = true;
    private float horizontalInput;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<BoxCollider2D>();
        
        originalColliderSize = bodyCollider.size;
        originalColliderOffset = bodyCollider.offset;
        
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -bodyCollider.bounds.extents.y, 0);
        }
    }
    
    private void Update()
    {
        GetInput();
        CheckGrounded();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        UpdateAnimations();
    }
    
    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
            
        isCrouching = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }
    
    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }
    
    private void HandleMovement()
    {
        if (isCrouching && isGrounded)
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed * crouchSpeedMultiplier, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }
        
        if (horizontalInput > 0 && !facingRight)
            Flip();
        else if (horizontalInput < 0 && facingRight)
            Flip();
    }
    
    private void HandleJump()
    {
        
    }
    
    private void TryJump()
    {
        if (isGrounded && !isCrouching)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }
    
    private void HandleCrouch()
    {
        if (isCrouching && isGrounded)
        {
            bodyCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
            bodyCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - originalColliderSize.y * 0.25f);
        }
        else
        {
            bodyCollider.size = originalColliderSize;
            bodyCollider.offset = originalColliderOffset;
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsCrouching", isCrouching && isGrounded);
            animator.SetFloat("VelocityY", rb.velocity.y);
        }
    }
    
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}