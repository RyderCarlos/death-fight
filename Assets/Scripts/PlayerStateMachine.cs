using UnityEngine;
public enum PlayerState
{
    Idle,
    Walking,
    Jumping,
    Falling,
    Crouching,
    Attacking
}

public class PlayerStateMachine : MonoBehaviour
{
    [Header("当前状态")]
    public PlayerState currentState = PlayerState.Idle;
    private PlayerState previousState;
    
    private PlayerController playerController;
    private Animator animator;
    
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        CheckStateTransitions();
        
        if (currentState != previousState)
        {
            OnStateEnter(currentState);
            previousState = currentState;
        }
    }
    
    void CheckStateTransitions()
    {
        // 根据玩家状态切换
        if (playerController.isGrounded)
        {
            if (playerController.crouchInput)
            {
                ChangeState(PlayerState.Crouching);
            }
            else if (Mathf.Abs(playerController.horizontalInput) > 0.1f)
            {
                ChangeState(PlayerState.Walking);
            }
            else
            {
                ChangeState(PlayerState.Idle);
            }
        }
        else
        {
            if (playerController.rb.velocity.y > 0.1f)
            {
                ChangeState(PlayerState.Jumping);
            }
            else if (playerController.rb.velocity.y < -0.1f)
            {
                ChangeState(PlayerState.Falling);
            }
        }
    }
    
    void ChangeState(PlayerState newState)
    {
        if (currentState != newState)
        {
            OnStateExit(currentState);
            currentState = newState;
        }
    }
    
    void OnStateEnter(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                animator.SetTrigger("Idle");
                break;
            case PlayerState.Walking:
                animator.SetTrigger("Walk");
                break;
            case PlayerState.Jumping:
                animator.SetTrigger("Jump");
                break;
            case PlayerState.Falling:
                animator.SetTrigger("Fall");
                break;
            case PlayerState.Crouching:
                animator.SetTrigger("Crouch");
                break;
        }
    }
    
    void OnStateExit(PlayerState state)
    {
        // 处理状态退出逻辑
    }
}