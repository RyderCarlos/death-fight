using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [Header("当前状态")]
    public PlayerState currentState = PlayerState.Idle;
    
    private Animator animator;
    private PlayerController playerController;
    
    // 状态枚举
    public enum PlayerState
    {
        Idle,
        Walking,
        Jumping,
        Falling,
        Crouching,
        Attacking,
        Blocking,
        Dodging,
        Hurt,
        Dead
    }
    
    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }
    
    void Update()
    {
        UpdateState();
    }
    
    void UpdateState()
    {
        PlayerState newState = DetermineNewState();
        
        if (newState != currentState)
        {
            ChangeState(newState);
        }
    }
    
    PlayerState DetermineNewState()
    {
        // 状态优先级判断
        if (currentState == PlayerState.Dead)
            return PlayerState.Dead;
            
        if (currentState == PlayerState.Hurt)
            return PlayerState.Hurt; // 由外部系统控制退出
            
        if (currentState == PlayerState.Attacking)
            return PlayerState.Attacking; // 由攻击系统控制退出
            
        // 其他状态判断逻辑...
        return PlayerState.Idle;
    }
    
    public void ChangeState(PlayerState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }
    
    void EnterState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                animator.SetTrigger("Idle");
                break;
            case PlayerState.Walking:
                break;
            case PlayerState.Jumping:
                animator.SetTrigger("Jump");
                break;
            case PlayerState.Attacking:
                break;
            // 其他状态...
        }
    }
    
    void ExitState(PlayerState state)
    {
        // 状态退出处理
    }
}