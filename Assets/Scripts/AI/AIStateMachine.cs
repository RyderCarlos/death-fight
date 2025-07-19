using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// AI状态机 - 管理AI的状态转换和行为
/// </summary>
public class AIStateMachine
{
    private AIController controller;
    private Dictionary<AIState, IAIState> states;
    private AIState currentState;
    private AIState previousState;
    private float stateTime;
    private float stateChangeTime;
    
    public AIState CurrentState => currentState;
    public AIState PreviousState => previousState;
    public float StateTime => stateTime;
    
    public event Action<AIState, AIState> OnStateChanged;
    
    public AIStateMachine(AIController controller)
    {
        this.controller = controller;
        InitializeStates();
        currentState = AIState.Idle;
        stateChangeTime = Time.time;
    }
    
    private void InitializeStates()
    {
        states = new Dictionary<AIState, IAIState>();
        
        // 创建所有状态
        states[AIState.Idle] = new IdleState(controller);
        states[AIState.Patrolling] = new PatrollingState(controller);
        states[AIState.Chasing] = new ChasingState(controller);
        states[AIState.Attacking] = new AttackingState(controller);
        states[AIState.Defending] = new DefendingState(controller);
        states[AIState.Retreating] = new RetreatingState(controller);
        states[AIState.Stunned] = new StunnedState(controller);
        states[AIState.Dead] = new DeadState(controller);
    }
    
    public AIState Update()
    {
        stateTime = Time.time - stateChangeTime;
        
        // 获取当前状态
        IAIState state = states[currentState];
        
        // 更新当前状态
        state.Update();
        
        // 检查状态转换
        AIState newState = state.CheckTransitions();
        
        if (newState != currentState)
        {
            ChangeState(newState);
        }
        
        return currentState;
    }
    
    public void ChangeState(AIState newState)
    {
        if (newState == currentState) return;
        
        // 退出当前状态
        if (states.ContainsKey(currentState))
        {
            states[currentState].Exit();
        }
        
        previousState = currentState;
        currentState = newState;
        stateChangeTime = Time.time;
        stateTime = 0f;
        
        // 进入新状态
        if (states.ContainsKey(currentState))
        {
            states[currentState].Enter();
        }
        
        OnStateChanged?.Invoke(previousState, currentState);
        
        if (controller.aiData.enableDebug)
        {
            Debug.Log($"[AI] {controller.name}: {previousState} → {currentState}");
        }
    }
    
    public void ForceState(AIState state)
    {
        ChangeState(state);
    }
    
    public T GetState<T>() where T : class, IAIState
    {
        foreach (var state in states.Values)
        {
            if (state is T)
                return state as T;
        }
        return null;
    }
}

/// <summary>
/// AI状态接口
/// </summary>
public interface IAIState
{
    void Enter();
    void Update();
    void Exit();
    AIState CheckTransitions();
}

/// <summary>
/// AI状态基类
/// </summary>
public abstract class AIStateBase : IAIState
{
    protected AIController controller;
    protected float stateTimer;
    
    public AIStateBase(AIController controller)
    {
        this.controller = controller;
    }
    
    public virtual void Enter()
    {
        stateTimer = 0f;
        if (controller.aiData.showThinkingProcess)
        {
            Debug.Log($"[AI思考] {controller.name} 进入状态: {GetType().Name}");
        }
    }
    
    public virtual void Update()
    {
        stateTimer += Time.deltaTime;
    }
    
    public virtual void Exit()
    {
        // 基类实现为空
    }
    
    public abstract AIState CheckTransitions();
    
    protected bool IsTargetInRange(float range)
    {
        return controller.target != null && controller.distanceToTarget <= range;
    }
    
    protected bool CanSeeTarget()
    {
        return controller.canSeeTarget;
    }
    
    protected bool IsHealthLow()
    {
        return controller.healthSystem != null && 
               controller.healthSystem.GetHealthPercentage() < 0.3f;
    }
}

#region 具体状态实现

/// <summary>
/// 空闲状态
/// </summary>
public class IdleState : AIStateBase
{
    public IdleState(AIController controller) : base(controller) { }
    
    public override void Update()
    {
        base.Update();
        
        // 在空闲状态中寻找目标
        if (controller.target == null)
        {
            // 执行巡逻或等待
        }
    }
    
    public override AIState CheckTransitions()
    {
        // 如果发现目标，开始追击
        if (controller.target != null && CanSeeTarget())
        {
            if (IsTargetInRange(controller.aiData.attackRange))
            {
                return AIState.Attacking;
            }
            else
            {
                return AIState.Chasing;
            }
        }
        
        // 如果没有目标，开始巡逻
        if (stateTimer > 2f)
        {
            return AIState.Patrolling;
        }
        
        return AIState.Idle;
    }
}

/// <summary>
/// 巡逻状态
/// </summary>
public class PatrollingState : AIStateBase
{
    private Vector2 patrolTarget;
    private float patrolRadius = 5f;
    
    public PatrollingState(AIController controller) : base(controller) { }
    
    public override void Enter()
    {
        base.Enter();
        SetNewPatrolTarget();
    }
    
    public override void Update()
    {
        base.Update();
        
        // 移动到巡逻点
        controller.MoveToPosition(patrolTarget);
        
        // 如果到达巡逻点，设置新的巡逻点
        if (Vector2.Distance(controller.transform.position, patrolTarget) < 0.5f)
        {
            SetNewPatrolTarget();
        }
    }
    
    private void SetNewPatrolTarget()
    {
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle * patrolRadius;
        patrolTarget = (Vector2)controller.transform.position + randomDirection;
    }
    
    public override AIState CheckTransitions()
    {
        // 发现目标
        if (controller.target != null && CanSeeTarget())
        {
            return AIState.Chasing;
        }
        
        // 巡逻时间过长，回到空闲
        if (stateTimer > 10f)
        {
            return AIState.Idle;
        }
        
        return AIState.Patrolling;
    }
}

/// <summary>
/// 追击状态
/// </summary>
public class ChasingState : AIStateBase
{
    public ChasingState(AIController controller) : base(controller) { }
    
    public override void Update()
    {
        base.Update();
        
        if (controller.target != null)
        {
            // 移动到目标
            controller.MoveToTarget();
        }
    }
    
    public override AIState CheckTransitions()
    {
        // 丢失目标
        if (controller.target == null || !CanSeeTarget())
        {
            return AIState.Idle;
        }
        
        // 进入攻击范围
        if (IsTargetInRange(controller.aiData.attackRange))
        {
            return AIState.Attacking;
        }
        
        // 血量过低，撤退
        if (IsHealthLow() && controller.aiData.defensiveness > 0.7f)
        {
            return AIState.Retreating;
        }
        
        return AIState.Chasing;
    }
}

/// <summary>
/// 攻击状态
/// </summary>
public class AttackingState : AIStateBase
{
    private float lastAttackTime;
    
    public AttackingState(AIController controller) : base(controller) { }
    
    public override void Update()
    {
        base.Update();
        
        // 尝试攻击
        if (Time.time - lastAttackTime > controller.aiData.attackCooldown)
        {
            if (controller.TryAttack())
            {
                lastAttackTime = Time.time;
            }
        }
    }
    
    public override AIState CheckTransitions()
    {
        // 丢失目标
        if (controller.target == null || !CanSeeTarget())
        {
            return AIState.Idle;
        }
        
        // 目标超出攻击范围
        if (!IsTargetInRange(controller.aiData.attackRange))
        {
            return AIState.Chasing;
        }
        
        // 需要防御
        if (ShouldDefend())
        {
            return AIState.Defending;
        }
        
        // 血量过低，撤退
        if (IsHealthLow())
        {
            return AIState.Retreating;
        }
        
        return AIState.Attacking;
    }
    
    private bool ShouldDefend()
    {
        // 简单的防御判断逻辑
        return UnityEngine.Random.Range(0f, 1f) < controller.aiData.defensiveness * 0.3f;
    }
}

/// <summary>
/// 防御状态
/// </summary>
public class DefendingState : AIStateBase
{
    public DefendingState(AIController controller) : base(controller) { }
    
    public override void Update()
    {
        base.Update();
        
        // 尝试防御
        controller.TryDefend();
    }
    
    public override AIState CheckTransitions()
    {
        // 防御时间结束
        if (stateTimer > 1f)
        {
            // 根据情况决定下一个状态
            if (IsTargetInRange(controller.aiData.attackRange))
            {
                return AIState.Attacking;
            }
            else
            {
                return AIState.Chasing;
            }
        }
        
        return AIState.Defending;
    }
}

/// <summary>
/// 撤退状态
/// </summary>
public class RetreatingState : AIStateBase
{
    public RetreatingState(AIController controller) : base(controller) { }
    
    public override void Update()
    {
        base.Update();
        
        if (controller.target != null)
        {
            // 远离目标
            Vector2 direction = (controller.transform.position - controller.target.position).normalized;
            Vector2 retreatPosition = (Vector2)controller.transform.position + direction * 2f;
            controller.MoveToPosition(retreatPosition);
        }
    }
    
    public override AIState CheckTransitions()
    {
        // 血量恢复或距离足够远
        if (!IsHealthLow() || controller.distanceToTarget > controller.aiData.maxDetectionRange)
        {
            return AIState.Idle;
        }
        
        // 撤退时间过长
        if (stateTimer > 5f)
        {
            return AIState.Chasing;
        }
        
        return AIState.Retreating;
    }
}

/// <summary>
/// 眩晕状态
/// </summary>
public class StunnedState : AIStateBase
{
    private float stunDuration;
    
    public StunnedState(AIController controller) : base(controller) { }
    
    public override void Enter()
    {
        base.Enter();
        stunDuration = 2f; // 默认眩晕时间
    }
    
    public override AIState CheckTransitions()
    {
        if (stateTimer > stunDuration)
        {
            return AIState.Idle;
        }
        
        return AIState.Stunned;
    }
}

/// <summary>
/// 死亡状态
/// </summary>
public class DeadState : AIStateBase
{
    public DeadState(AIController controller) : base(controller) { }
    
    public override AIState CheckTransitions()
    {
        // 死亡状态不会转换
        return AIState.Dead;
    }
}

#endregion