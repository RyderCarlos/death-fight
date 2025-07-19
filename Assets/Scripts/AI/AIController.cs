using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// AI控制器基类 - 所有AI角色的基础控制器
/// </summary>
public abstract class AIController : MonoBehaviour
{
    [Header("AI基础设置")]
    public AIData aiData;
    public Transform target;
    public LayerMask targetLayerMask = 1;
    
    [Header("调试信息")]
    public bool showDebugInfo = true;
    public AIState currentState;
    public float distanceToTarget;
    public bool canSeeTarget;
    
    // 核心组件
    protected AIStateMachine stateMachine;
    protected AIBehaviorTree behaviorTree;
    public AttackSystem attackSystem;
    public DefenseSystem defenseSystem;
    public HealthSystem healthSystem;
    public EnergySystem energySystem;
    protected PlayerController movement;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    
    // AI状态数据
    protected float lastActionTime;
    protected float lastTargetCheckTime;
    protected Vector2 lastKnownTargetPosition;
    protected bool targetLost;
    
    // 事件系统
    public event Action<Transform> OnTargetFound;
    public event Action OnTargetLost;
    public event Action<AIState, AIState> OnStateChanged;
    public event Action<string> OnBehaviorExecuted;
    
    // 受保护的事件触发方法
    public void TriggerBehaviorExecuted(string behavior)
    {
        OnBehaviorExecuted?.Invoke(behavior);
    }
    
    protected virtual void Awake()
    {
        InitializeComponents();
        InitializeAI();
    }
    
    protected virtual void Start()
    {
        SetupAI();
        
        if (target == null)
        {
            FindInitialTarget();
        }
    }
    
    protected virtual void Update()
    {
        if (aiData == null) return;
        
        UpdateTargetInfo();
        UpdateStateMachine();
        UpdateBehaviorTree();
        
        if (showDebugInfo)
        {
            DebugUpdate();
        }
    }
    
    #region 初始化
    
    protected virtual void InitializeComponents()
    {
        attackSystem = GetComponent<AttackSystem>();
        defenseSystem = GetComponent<DefenseSystem>();
        healthSystem = GetComponent<HealthSystem>();
        energySystem = GetComponent<EnergySystem>();
        movement = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    protected virtual void InitializeAI()
    {
        // 创建状态机
        stateMachine = new AIStateMachine(this);
        
        // 创建行为树
        behaviorTree = CreateBehaviorTree();
        
        // 设置初始状态
        currentState = AIState.Idle;
    }
    
    protected virtual void SetupAI()
    {
        // 订阅健康系统事件
        if (healthSystem != null)
        {
            healthSystem.OnTakeDamage += OnTakeDamage;
            healthSystem.OnDeath += OnDeath;
        }
        
        // 订阅攻击系统事件
        if (attackSystem != null)
        {
            attackSystem.OnAttackStart += OnAttackStart;
            attackSystem.OnHitTarget += OnAttackHit;
        }
    }
    
    #endregion
    
    #region 目标管理
    
    protected virtual void UpdateTargetInfo()
    {
        if (target == null)
        {
            if (Time.time - lastTargetCheckTime > aiData.targetSearchInterval)
            {
                FindTarget();
                lastTargetCheckTime = Time.time;
            }
            return;
        }
        
        // 计算距离
        distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // 检查视线
        canSeeTarget = CanSeeTarget();
        
        // 检查目标是否丢失
        if (distanceToTarget > aiData.maxDetectionRange || !canSeeTarget)
        {
            if (!targetLost)
            {
                targetLost = true;
                lastKnownTargetPosition = target.position;
                OnTargetLost?.Invoke();
            }
        }
        else
        {
            if (targetLost)
            {
                targetLost = false;
                OnTargetFound?.Invoke(target);
            }
            lastKnownTargetPosition = target.position;
        }
    }
    
    protected virtual void FindTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag("Player");
        
        float closestDistance = float.MaxValue;
        Transform closestTarget = null;
        
        foreach (GameObject obj in potentialTargets)
        {
            float distance = Vector2.Distance(transform.position, obj.transform.position);
            if (distance < aiData.maxDetectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = obj.transform;
            }
        }
        
        if (closestTarget != target)
        {
            target = closestTarget;
            if (target != null)
            {
                OnTargetFound?.Invoke(target);
            }
        }
    }
    
    protected virtual void FindInitialTarget()
    {
        FindTarget();
    }
    
    protected virtual bool CanSeeTarget()
    {
        if (target == null) return false;
        
        Vector2 direction = target.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            direction.normalized, 
            direction.magnitude,
            ~targetLayerMask
        );
        
        return hit.collider == null || hit.collider.transform == target;
    }
    
    #endregion
    
    #region 状态机和行为树
    
    protected virtual void UpdateStateMachine()
    {
        if (stateMachine != null)
        {
            AIState newState = stateMachine.Update();
            if (newState != currentState)
            {
                OnStateChanged?.Invoke(currentState, newState);
                currentState = newState;
            }
        }
    }
    
    protected virtual void UpdateBehaviorTree()
    {
        if (behaviorTree != null)
        {
            behaviorTree.Evaluate();
        }
    }
    
    protected abstract AIBehaviorTree CreateBehaviorTree();
    
    #endregion
    
    #region 行为方法
    
    public virtual void MoveToTarget()
    {
        if (target == null || movement == null) return;
        
        Vector2 direction = (target.position - transform.position).normalized;
        
        // 简单的移动逻辑
        if (direction.x > 0.1f)
        {
            movement.SetHorizontalInput(1f);
        }
        else if (direction.x < -0.1f)
        {
            movement.SetHorizontalInput(-1f);
        }
        else
        {
            movement.SetHorizontalInput(0f);
        }
    }
    
    public virtual void MoveToPosition(Vector2 position)
    {
        if (movement == null) return;
        
        Vector2 direction = (position - (Vector2)transform.position).normalized;
        
        if (direction.x > 0.1f)
        {
            movement.SetHorizontalInput(1f);
        }
        else if (direction.x < -0.1f)
        {
            movement.SetHorizontalInput(-1f);
        }
        else
        {
            movement.SetHorizontalInput(0f);
        }
    }
    
    public virtual bool TryAttack()
    {
        if (attackSystem == null || target == null) return false;
        
        // 检查是否在攻击范围内
        if (distanceToTarget > aiData.attackRange) return false;
        
        // 检查攻击冷却
        if (Time.time - lastActionTime < aiData.attackCooldown) return false;
        
        // 根据难度调整反应时间
        if (Time.time - lastActionTime < aiData.reactionTime) return false;
        
        // 选择攻击类型
        AttackType attackType = ChooseAttackType();
        
        bool success = attackSystem.TryAttack(attackType);
        if (success)
        {
            lastActionTime = Time.time;
            OnBehaviorExecuted?.Invoke($"攻击: {attackType}");
        }
        
        return success;
    }
    
    public virtual bool TryDefend()
    {
        if (defenseSystem == null) return false;
        
        // 根据难度调整防御成功率
        if (UnityEngine.Random.Range(0f, 1f) > aiData.defenseSuccessRate)
        {
            return false;
        }
        
        // 检查是否需要防御
        if (IsIncomingAttack())
        {
            bool success = defenseSystem.TryDefend();
            if (success)
            {
                TriggerBehaviorExecuted("防御");
            }
            return success;
        }
        
        return false;
    }
    
    protected virtual AttackType ChooseAttackType()
    {
        // 根据距离和情况选择攻击类型
        if (distanceToTarget < 1.5f)
        {
            return UnityEngine.Random.Range(0f, 1f) < 0.6f ? AttackType.轻拳 : AttackType.重拳;
        }
        else
        {
            return UnityEngine.Random.Range(0f, 1f) < 0.7f ? AttackType.轻腿 : AttackType.重腿;
        }
    }
    
    protected virtual bool IsIncomingAttack()
    {
        // 检测是否有即将到来的攻击
        if (target == null) return false;
        
        AttackSystem targetAttack = target.GetComponent<AttackSystem>();
        if (targetAttack != null)
        {
            // 简单的预判逻辑
            return distanceToTarget < 2f && UnityEngine.Random.Range(0f, 1f) < 0.3f;
        }
        
        return false;
    }
    
    #endregion
    
    #region 事件处理
    
    protected virtual void OnTakeDamage(DamageInfo damageInfo)
    {
        // 受到伤害时的反应
        if (damageInfo.attacker != null)
        {
            target = damageInfo.attacker.transform;
        }
        
        // 可能触发反击
        if (UnityEngine.Random.Range(0f, 1f) < aiData.counterAttackChance)
        {
            StartCoroutine(DelayedCounterAttack());
        }
    }
    
    protected virtual void OnDeath()
    {
        // 死亡时的处理
        enabled = false;
    }
    
    protected virtual void OnAttackStart(AttackData attackData)
    {
        // 攻击开始时的处理
    }
    
    protected virtual void OnAttackHit(GameObject target, AttackData attackData)
    {
        // 攻击命中时的处理
    }
    
    protected virtual IEnumerator DelayedCounterAttack()
    {
        yield return new WaitForSeconds(aiData.counterAttackDelay);
        TryAttack();
    }
    
    #endregion
    
    #region 调试
    
    protected virtual void DebugUpdate()
    {
        if (target != null)
        {
            Debug.DrawLine(transform.position, target.position, 
                canSeeTarget ? Color.green : Color.red);
        }
    }
    
    public virtual string GetDebugInfo()
    {
        return $"状态: {currentState}\n" +
               $"目标距离: {distanceToTarget:F2}\n" +
               $"可见目标: {canSeeTarget}\n" +
               $"目标丢失: {targetLost}";
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (aiData == null) return;
        
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aiData.maxDetectionRange);
        
        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aiData.attackRange);
        
        // 绘制视线
        if (target != null)
        {
            Gizmos.color = canSeeTarget ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
    
    #endregion
    
    protected virtual void OnDestroy()
    {
        // 清理事件订阅
        if (healthSystem != null)
        {
            healthSystem.OnTakeDamage -= OnTakeDamage;
            healthSystem.OnDeath -= OnDeath;
        }
        
        if (attackSystem != null)
        {
            attackSystem.OnAttackStart -= OnAttackStart;
            attackSystem.OnHitTarget -= OnAttackHit;
        }
    }
}

/// <summary>
/// AI状态枚举
/// </summary>
public enum AIState
{
    Idle,           // 空闲
    Patrolling,     // 巡逻
    Chasing,        // 追击
    Attacking,      // 攻击
    Defending,      // 防御
    Retreating,     // 撤退
    Stunned,        // 眩晕
    Dead            // 死亡
}