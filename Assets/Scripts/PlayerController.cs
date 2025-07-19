using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    
    [Header("地面检测")]
    public Transform[] groundCheckPoints;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayerMask = 1;
    
    [Header("组件引用")]
    public Rigidbody2D rb2d;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    // 状态变量
    public bool isGrounded;
    private bool facingRight = true;
    private float horizontalInput;
    private bool jumpInput;
    private bool crouchInput;
    
    void Start()
    {
        // 获取组件引用
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 如果没有设置地面检测点，自动创建
        if (groundCheckPoints == null || groundCheckPoints.Length == 0)
        {
            CreateGroundCheckPoints();
        }
    }
    
    void Update()
    {
        // 获取输入
        GetInput();
        
        // 检测地面
        CheckGrounded();
        
        // 更新动画参数
        UpdateAnimator();
    }
    
    void FixedUpdate()
    {
        // 处理移动
        HandleMovement();
        
        // 处理跳跃
        HandleJump();
    }
    
    void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpInput = Input.GetKeyDown(KeyCode.Space);
        crouchInput = Input.GetKey(KeyCode.S);
    }
    
    void HandleMovement()
    {
        // 下蹲时不允许移动
        if (crouchInput) return;
        
        // 水平移动
        rb2d.velocity = new Vector2(horizontalInput * moveSpeed, rb2d.velocity.y);
        
        // 转向
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
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpForce);
        }
    }
    
    void CheckGrounded()
    {
        isGrounded = false;
        foreach (Transform checkPoint in groundCheckPoints)
        {
            Collider2D collider = Physics2D.OverlapCircle(
                checkPoint.position, 
                groundCheckRadius, 
                groundLayerMask
            );
            if (collider != null)
            {
                isGrounded = true;
                break;
            }
        }
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !facingRight;
    }
    
    void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("yVelocity", rb2d.velocity.y);
        animator.SetBool("IsCrouching", crouchInput && isGrounded);
    }
    
    void CreateGroundCheckPoints()
    {
        // 自动创建地面检测点
        groundCheckPoints = new Transform[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject checkPoint = new GameObject("GroundCheck" + i);
            checkPoint.transform.SetParent(transform);
            groundCheckPoints[i] = checkPoint.transform;
        }
        
        // 设置检测点位置
        Bounds bounds = GetComponent<Collider2D>().bounds;
        groundCheckPoints[0].localPosition = new Vector3(-bounds.size.x/2, -bounds.size.y/2, 0);
        groundCheckPoints[1].localPosition = new Vector3(0, -bounds.size.y/2, 0);
        groundCheckPoints[2].localPosition = new Vector3(bounds.size.x/2, -bounds.size.y/2, 0);
    }
    
    // 调试绘制
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform checkPoint in groundCheckPoints)
            {
                if (checkPoint != null)
                {
                    Gizmos.DrawWireSphere(checkPoint.position, groundCheckRadius);
                }
            }
        }
    }
    
    // Public method for AI control
    public void SetHorizontalInput(float input)
    {
        horizontalInput = input;
    }
}