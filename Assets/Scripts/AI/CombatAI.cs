using System.Collections;
using UnityEngine;

/// <summary>
/// 战斗AI - 继承自AIController的具体实现
/// </summary>
public class CombatAI : AIController
{
    [Header("战斗AI特殊设置")]
    public bool enableAdvancedCombat = true;
    public float combatStyleChangeInterval = 5f;
    
    // 战斗风格
    private CombatStyle currentCombatStyle;
    private float lastStyleChangeTime;
    
    // 攻击相关
    private int consecutiveAttacks = 0;
    private float lastAttackTime;
    private bool isInCombo = false;
    
    // 防御相关
    private float lastDefenseTime;
    private int successfulBlocks = 0;
    
    // 移动相关
    private Vector2 currentMoveTarget;
    private bool isStrafing = false;
    private float strafeDirection = 1f;
    
    protected override void Start()
    {
        base.Start();
        
        // 初始化战斗风格
        currentCombatStyle = CombatStyle.平衡;
        lastStyleChangeTime = Time.time;
        
        // 根据AI数据调整难度
        if (aiData != null)
        {
            aiData.ApplyDifficultyModifier();
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 更新战斗风格
        UpdateCombatStyle();
        
        // 更新战斗状态
        UpdateCombatBehavior();
    }
    
    #region 战斗风格管理
    
    private void UpdateCombatStyle()
    {
        if (!enableAdvancedCombat) return;
        
        // 定期改变战斗风格
        if (Time.time - lastStyleChangeTime > combatStyleChangeInterval)
        {
            ChangeCombatStyle();
            lastStyleChangeTime = Time.time;
        }
        
        // 根据情况动态调整风格
        AdjustCombatStyleByContext();
    }
    
    private void ChangeCombatStyle()
    {
        // 根据AI类型和随机性选择新的战斗风格
        CombatStyle newStyle = currentCombatStyle;
        
        switch (aiData.aiType)
        {
            case AIType.攻击型:
                newStyle = Random.Range(0f, 1f) < 0.7f ? CombatStyle.激进 : CombatStyle.平衡;
                break;
            case AIType.防御型:
                newStyle = Random.Range(0f, 1f) < 0.7f ? CombatStyle.保守 : CombatStyle.平衡;
                break;
            case AIType.敏捷型:
                newStyle = Random.Range(0f, 1f) < 0.7f ? CombatStyle.游击 : CombatStyle.平衡;
                break;
            default:
                // 随机选择
                newStyle = (CombatStyle)Random.Range(0, System.Enum.GetValues(typeof(CombatStyle)).Length);
                break;
        }
        
        if (newStyle != currentCombatStyle)
        {
            currentCombatStyle = newStyle;
            TriggerBehaviorExecuted($"改变战斗风格为: {currentCombatStyle}");
        }
    }
    
    private void AdjustCombatStyleByContext()
    {
        if (healthSystem == null) return;
        
        float healthPercentage = healthSystem.GetHealthPercentage();
        
        // 血量低时变得保守
        if (healthPercentage < 0.3f && currentCombatStyle != CombatStyle.保守)
        {
            currentCombatStyle = CombatStyle.保守;
            TriggerBehaviorExecuted("血量过低，切换为保守风格");
        }
        // 血量充足且连击成功时变得激进
        else if (healthPercentage > 0.7f && consecutiveAttacks > 2 && currentCombatStyle != CombatStyle.激进)
        {
            currentCombatStyle = CombatStyle.激进;
            TriggerBehaviorExecuted("连击成功，切换为激进风格");
        }
    }
    
    #endregion
    
    #region 攻击行为增强
    
    public override bool TryAttack()
    {
        if (!base.TryAttack()) return false;
        
        consecutiveAttacks++;
        lastAttackTime = Time.time;
        
        // 根据战斗风格调整攻击行为
        switch (currentCombatStyle)
        {
            case CombatStyle.激进:
                // 激进风格：更快的攻击频率
                StartCoroutine(AggressiveAttackPattern());
                break;
                
            case CombatStyle.技巧:
                // 技巧风格：尝试使用特殊技能
                TryUseSpecialTechnique();
                break;
                
            case CombatStyle.游击:
                // 游击风格：攻击后立即移动
                StartCoroutine(HitAndRunPattern());
                break;
        }
        
        return true;
    }
    
    private IEnumerator AggressiveAttackPattern()
    {
        // 激进攻击模式：连续攻击
        float comboWindow = 0.8f;
        float elapsed = 0f;
        
        isInCombo = true;
        
        while (elapsed < comboWindow && target != null && distanceToTarget <= aiData.attackRange)
        {
            yield return new WaitForSeconds(0.3f);
            
            if (Random.Range(0f, 1f) < aiData.comboChance * 1.5f) // 激进风格提高连击概率
            {
                base.TryAttack();
                consecutiveAttacks++;
            }
            
            elapsed += 0.3f;
        }
        
        isInCombo = false;
    }
    
    private void TryUseSpecialTechnique()
    {
        if (energySystem != null && energySystem.CanUseSpecialSkill())
        {
            if (Random.Range(0f, 1f) < aiData.specialSkillChance * 2f) // 技巧风格提高特殊技能使用概率
            {
                SpecialSkillController specialSkill = GetComponent<SpecialSkillController>();
                if (specialSkill != null)
                {
                    specialSkill.TryUseSpecialSkill();
                    TriggerBehaviorExecuted("使用特殊技能");
                }
            }
        }
    }
    
    private IEnumerator HitAndRunPattern()
    {
        // 游击攻击模式：攻击后立即移动
        yield return new WaitForSeconds(0.2f);
        
        if (target != null)
        {
            // 侧向移动
            Vector2 toTarget = (target.position - transform.position).normalized;
            Vector2 strafeDirection = new Vector2(-toTarget.y, toTarget.x);
            
            if (Random.Range(0f, 1f) < 0.5f)
                strafeDirection = -strafeDirection;
            
            currentMoveTarget = (Vector2)transform.position + strafeDirection * 3f;
            isStrafing = true;
            
            TriggerBehaviorExecuted("游击移动");
        }
    }
    
    #endregion
    
    #region 防御行为增强
    
    public override bool TryDefend()
    {
        bool success = base.TryDefend();
        
        if (success)
        {
            lastDefenseTime = Time.time;
            successfulBlocks++;
            
            // 根据战斗风格调整防御后的行为
            switch (currentCombatStyle)
            {
                case CombatStyle.保守:
                    // 保守风格：防御后继续防御
                    StartCoroutine(ContinuousDefensePattern());
                    break;
                    
                case CombatStyle.反击:
                    // 反击风格：防御后立即反击
                    StartCoroutine(CounterAttackPattern());
                    break;
            }
        }
        
        return success;
    }
    
    private IEnumerator ContinuousDefensePattern()
    {
        // 连续防御模式
        float defenseWindow = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < defenseWindow && target != null)
        {
            yield return new WaitForSeconds(0.2f);
            
            if (IsIncomingAttack() && Random.Range(0f, 1f) < aiData.defenseSuccessRate * 1.2f)
            {
                base.TryDefend();
            }
            
            elapsed += 0.2f;
        }
    }
    
    private IEnumerator CounterAttackPattern()
    {
        // 反击模式
        yield return new WaitForSeconds(aiData.counterAttackDelay * 0.7f); // 更快的反击
        
        if (target != null && distanceToTarget <= aiData.attackRange)
        {
            // 选择重攻击进行反击
            AttackType counterAttackType = Random.Range(0f, 1f) < 0.6f ? AttackType.重拳 : AttackType.重腿;
            
            if (attackSystem != null)
            {
                attackSystem.TryAttack(counterAttackType);
                TriggerBehaviorExecuted($"反击: {counterAttackType}");
            }
        }
    }
    
    #endregion
    
    #region 移动行为增强
    
    public override void MoveToTarget()
    {
        if (isStrafing)
        {
            // 执行侧移
            MoveToPosition(currentMoveTarget);
            
            // 检查是否到达目标位置
            if (Vector2.Distance(transform.position, currentMoveTarget) < 0.5f)
            {
                isStrafing = false;
            }
            return;
        }
        
        // 根据战斗风格调整移动方式
        switch (currentCombatStyle)
        {
            case CombatStyle.游击:
                ExecuteGuerrillaMovement();
                break;
                
            case CombatStyle.保守:
                ExecuteDefensiveMovement();
                break;
                
            default:
                base.MoveToTarget();
                break;
        }
    }
    
    private void ExecuteGuerrillaMovement()
    {
        if (target == null) return;
        
        // 游击移动：保持距离，不断变换位置
        float optimalDistance = aiData.attackRange * 0.8f;
        
        if (distanceToTarget < optimalDistance)
        {
            // 距离太近，后退
            Vector2 awayDirection = (transform.position - target.position).normalized;
            currentMoveTarget = (Vector2)transform.position + awayDirection * 1.5f;
        }
        else if (distanceToTarget > aiData.attackRange)
        {
            // 距离太远，接近
            base.MoveToTarget();
        }
        else
        {
            // 距离合适，侧移
            Vector2 toTarget = (target.position - transform.position).normalized;
            Vector2 strafeDir = new Vector2(-toTarget.y, toTarget.x) * strafeDirection;
            currentMoveTarget = (Vector2)transform.position + strafeDir * 2f;
            
            // 随机改变侧移方向
            if (Random.Range(0f, 1f) < 0.1f)
            {
                strafeDirection *= -1f;
            }
        }
        
        MoveToPosition(currentMoveTarget);
    }
    
    private void ExecuteDefensiveMovement()
    {
        if (target == null) return;
        
        // 防御移动：保持安全距离
        float safeDistance = aiData.followDistance * 1.2f;
        
        if (distanceToTarget < safeDistance)
        {
            // 保持距离
            Vector2 awayDirection = (transform.position - target.position).normalized;
            currentMoveTarget = (Vector2)transform.position + awayDirection * 1f;
            MoveToPosition(currentMoveTarget);
        }
        else if (distanceToTarget > aiData.maxDetectionRange * 0.8f)
        {
            // 不要跟丢目标
            base.MoveToTarget();
        }
    }
    
    #endregion
    
    #region 行为树创建
    
    protected override AIBehaviorTree CreateBehaviorTree()
    {
        return new CombatAIBehaviorTree(this);
    }
    
    #endregion
    
    #region 随机行为
    
    private void UpdateCombatBehavior()
    {
        // 随机行为变化
        if (Random.Range(0f, 1f) < aiData.behaviorChangeChance * Time.deltaTime)
        {
            ExecuteRandomBehavior();
        }
        
        // 重置连击计数
        if (Time.time - lastAttackTime > 2f)
        {
            consecutiveAttacks = 0;
        }
    }
    
    private void ExecuteRandomBehavior()
    {
        if (target == null) return;
        
        float roll = Random.Range(0f, 1f);
        
        if (roll < 0.3f)
        {
            // 突然改变移动方向
            strafeDirection *= -1f;
            TriggerBehaviorExecuted("改变移动方向");
        }
        else if (roll < 0.6f)
        {
            // 假攻击（开始攻击但立即取消）
            StartCoroutine(FakeAttack());
        }
        else if (roll < 0.8f)
        {
            // 短暂后退
            StartCoroutine(TacticalRetreat());
        }
        else
        {
            // 激进推进
            StartCoroutine(AggressiveAdvance());
        }
    }
    
    private IEnumerator FakeAttack()
    {
        TriggerBehaviorExecuted("假攻击");
        
        // 播放攻击动画但不实际攻击
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 立即取消
        if (attackSystem != null)
        {
            attackSystem.CancelCurrentAttack();
        }
    }
    
    private IEnumerator TacticalRetreat()
    {
        TriggerBehaviorExecuted("战术后退");
        
        Vector2 retreatDirection = (transform.position - target.position).normalized;
        Vector2 retreatTarget = (Vector2)transform.position + retreatDirection * 2f;
        
        float retreatTime = 1f;
        float elapsed = 0f;
        
        while (elapsed < retreatTime)
        {
            MoveToPosition(retreatTarget);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator AggressiveAdvance()
    {
        TriggerBehaviorExecuted("激进推进");
        
        float advanceTime = 0.8f;
        float elapsed = 0f;
        
        while (elapsed < advanceTime && target != null)
        {
            // 直接冲向目标
            base.MoveToTarget();
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    #endregion
    
    public override string GetDebugInfo()
    {
        string baseInfo = base.GetDebugInfo();
        return baseInfo + $"\n战斗风格: {currentCombatStyle}\n" +
               $"连续攻击: {consecutiveAttacks}\n" +
               $"成功格挡: {successfulBlocks}\n" +
               $"连击中: {isInCombo}";
    }
}

/// <summary>
/// 战斗风格枚举
/// </summary>
public enum CombatStyle
{
    平衡,     // 平衡的攻防
    激进,     // 主要攻击
    保守,     // 主要防御
    游击,     // 快速移动
    反击,     // 防御后反击
    技巧      // 使用特殊技能
}

/// <summary>
/// 战斗AI专用行为树
/// </summary>
public class CombatAIBehaviorTree : AIBehaviorTree
{
    private CombatAI combatAI;
    
    public CombatAIBehaviorTree(CombatAI controller) : base(controller)
    {
        this.combatAI = controller;
    }
    
    protected override void BuildTree()
    {
        // 为战斗AI构建更复杂的行为树
        rootNode = new SelectorNode("CombatRoot")
        {
            Children = new System.Collections.Generic.List<BehaviorNode>
            {
                CreateEmergencyBehavior(),   // 紧急行为
                CreateCombatBehavior(),      // 战斗行为
                CreateTacticalBehavior(),    // 战术行为
                CreateMovementBehavior(),    // 移动行为
                CreateIdleBehavior()         // 空闲行为
            }
        };
        
        SetContext(rootNode);
    }
    
    private BehaviorNode CreateEmergencyBehavior()
    {
        return new SelectorNode("Emergency")
        {
            Children = new System.Collections.Generic.List<BehaviorNode>
            {
                // 血量危急时的行为
                new SequenceNode("LowHealth")
                {
                    Children = new System.Collections.Generic.List<BehaviorNode>
                    {
                        new ConditionNode("HealthCritical", () => 
                            controller.healthSystem != null && 
                            controller.healthSystem.GetHealthPercentage() < 0.2f),
                        new ActionNode("EmergencyRetreat", () => 
                        {
                            // 紧急撤退逻辑
                            return NodeStatus.Success;
                        })
                    }
                }
            }
        };
    }
    
    private BehaviorNode CreateTacticalBehavior()
    {
        return new SelectorNode("Tactical")
        {
            Children = new System.Collections.Generic.List<BehaviorNode>
            {
                // 等待时机
                new SequenceNode("WaitForOpening")
                {
                    Children = new System.Collections.Generic.List<BehaviorNode>
                    {
                        new ConditionNode("ShouldWait", () => ShouldWaitForOpening()),
                        new ActionNode("Wait", () => NodeStatus.Success)
                    }
                },
                
                // 诱导攻击
                new SequenceNode("BaitAttack")
                {
                    Children = new System.Collections.Generic.List<BehaviorNode>
                    {
                        new ConditionNode("ShouldBait", () => ShouldBaitAttack()),
                        new ActionNode("Bait", () => ExecuteBaitTactic())
                    }
                }
            }
        };
    }
    
    private bool ShouldWaitForOpening()
    {
        // 根据AI的耐心程度决定是否等待
        return Random.Range(0f, 1f) < combatAI.aiData.defensiveness * 0.4f;
    }
    
    private bool ShouldBaitAttack()
    {
        // 在特定情况下诱导对手攻击
        return combatAI.target != null && 
               combatAI.distanceToTarget > combatAI.aiData.attackRange * 0.8f &&
               Random.Range(0f, 1f) < 0.2f;
    }
    
    private NodeStatus ExecuteBaitTactic()
    {
        // 执行诱导战术
        combatAI.TriggerBehaviorExecuted("诱导攻击");
        
        // 假装攻击然后后退
        combatAI.StartCoroutine(BaitSequence());
        return NodeStatus.Success;
    }
    
    private System.Collections.IEnumerator BaitSequence()
    {
        // 向前移动
        for (int i = 0; i < 5; i++)
        {
            if (combatAI.target != null)
            {
                combatAI.MoveToTarget();
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        // 突然后退
        yield return new WaitForSeconds(0.2f);
        
        if (combatAI.target != null)
        {
            Vector2 retreatDirection = (combatAI.transform.position - combatAI.target.position).normalized;
            Vector2 retreatTarget = (Vector2)combatAI.transform.position + retreatDirection * 2f;
            
            for (int i = 0; i < 8; i++)
            {
                combatAI.MoveToPosition(retreatTarget);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}