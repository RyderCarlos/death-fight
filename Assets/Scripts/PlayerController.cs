using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float groundCheckDistance = 0.1f;
    
    [Header("组件引用")]
    public Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    [Header("状态变量")]
    public bool isGrounded;
    public bool facingRight = true;
    public float horizontalInput;
    public bool jumpInput;
    public bool crouchInput;
    
    [Header("地面检测")]
    public Transform groundCheck;
    public LayerMask groundLayerMask;
    
    void Start()
    {
        // 获取组件引用
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 如果没有设置groundCheck，创建一个
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        // 获取输入
        GetInput();
        
        // 检测是否在地面
        CheckGrounded();
        
        // 处理移动
        HandleMovement();
        
        // 处理跳跃
        HandleJump();
        
        // 更新动画
        UpdateAnimations();
    }
    
    void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpInput = Input.GetButtonDown("Jump");
        crouchInput = Input.GetButton("Fire1"); // 可以改为其他按键
    }
    
    void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckDistance, groundLayerMask);
    }
    
    void HandleMovement()
    {
        // 水平移动
        if (!crouchInput) // 下蹲时不能移动
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        
        // 处理角色转向
        if (horizontalInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && facingRight)
        {
            Flip();
        }
    }
    
    void HandleJump()
    {
        if (jumpInput && isGrounded && !crouchInput)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !facingRight;
    }
    
    void UpdateAnimations()
    {
        // 设置动画参数
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", crouchInput);
        animator.SetFloat("VelocityY", rb.velocity.y);
    }
    
    // 在Scene视图中显示地面检测范围
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}