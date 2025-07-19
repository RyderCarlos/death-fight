using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// AI行为树系统 - 实现复杂的AI决策逻辑
/// </summary>
public class AIBehaviorTree
{
    protected AIController controller;
    protected BehaviorNode rootNode;
    protected Dictionary<string, object> blackboard;
    
    public AIBehaviorTree(AIController controller)
    {
        this.controller = controller;
        blackboard = new Dictionary<string, object>();
        BuildTree();
    }
    
    public NodeStatus Evaluate()
    {
        if (rootNode != null)
        {
            return rootNode.Evaluate();
        }
        return NodeStatus.Failure;
    }
    
    protected virtual void BuildTree()
    {
        // 创建根节点 - 选择器（优先级从高到低）
        rootNode = new SelectorNode("Root")
        {
            Children = new List<BehaviorNode>
            {
                CreateCombatBehavior(),      // 战斗行为（最高优先级）
                CreateMovementBehavior(),    // 移动行为
                CreateIdleBehavior()         // 空闲行为（最低优先级）
            }
        };
        
        // 设置上下文
        SetContext(rootNode);
    }
    
    protected void SetContext(BehaviorNode node)
    {
        node.SetContext(controller, blackboard);
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                SetContext(child);
            }
        }
    }
    
    #region 行为构建
    
    protected virtual BehaviorNode CreateCombatBehavior()
    {
        return new SelectorNode("Combat")
        {
            Children = new List<BehaviorNode>
            {
                // 防御行为（最高优先级）
                new SequenceNode("Defense")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("InDanger", () => IsInDanger()),
                        new ActionNode("Defend", () => controller.TryDefend() ? NodeStatus.Success : NodeStatus.Failure)
                    }
                },
                
                // 攻击行为
                new SequenceNode("Attack")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("CanAttack", () => CanAttack()),
                        new SelectorNode("AttackChoice")
                        {
                            Children = new List<BehaviorNode>
                            {
                                // 连击攻击
                                new SequenceNode("ComboAttack")
                                {
                                    Children = new List<BehaviorNode>
                                    {
                                        new ConditionNode("ShouldCombo", () => ShouldCombo()),
                                        new ActionNode("ComboAttack", () => ExecuteComboAttack())
                                    }
                                },
                                
                                // 普通攻击
                                new ActionNode("NormalAttack", () => controller.TryAttack() ? NodeStatus.Success : NodeStatus.Failure)
                            }
                        }
                    }
                },
                
                // 特殊技能
                new SequenceNode("SpecialSkill")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("CanUseSpecial", () => CanUseSpecialSkill()),
                        new ActionNode("UseSpecial", () => UseSpecialSkill())
                    }
                }
            }
        };
    }
    
    protected virtual BehaviorNode CreateMovementBehavior()
    {
        return new SelectorNode("Movement")
        {
            Children = new List<BehaviorNode>
            {
                // 追击目标
                new SequenceNode("Chase")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("HasTarget", () => HasTarget()),
                        new ConditionNode("TargetOutOfRange", () => IsTargetOutOfRange()),
                        new ActionNode("MoveToTarget", () => 
                        {
                            controller.MoveToTarget();
                            return NodeStatus.Success;
                        })
                    }
                },
                
                // 保持距离
                new SequenceNode("MaintainDistance")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("TargetTooClose", () => IsTargetTooClose()),
                        new ActionNode("MoveAway", () => MoveAwayFromTarget())
                    }
                },
                
                // 侧移
                new SequenceNode("Strafe")
                {
                    Children = new List<BehaviorNode>
                    {
                        new ConditionNode("ShouldStrafe", () => ShouldStrafe()),
                        new ActionNode("StrafeMove", () => ExecuteStrafe())
                    }
                }
            }
        };
    }
    
    protected virtual BehaviorNode CreateIdleBehavior()
    {
        return new SelectorNode("Idle")
        {
            Children = new List<BehaviorNode>
            {
                // 搜索目标
                new ActionNode("SearchTarget", () => 
                {
                    // 搜索目标的逻辑在AIController中处理
                    return NodeStatus.Success;
                }),
                
                // 等待
                new ActionNode("Wait", () => NodeStatus.Success)
            }
        };
    }
    
    #endregion
    
    #region 条件检查
    
    private bool IsInDanger()
    {
        // 检查是否处于危险中（即将受到攻击）
        if (controller.target == null) return false;
        
        AttackSystem targetAttack = controller.target.GetComponent<AttackSystem>();
        if (targetAttack != null)
        {
            // 简单的危险判断
            return controller.distanceToTarget < 2.5f && UnityEngine.Random.Range(0f, 1f) < 0.4f;
        }
        
        return false;
    }
    
    private bool CanAttack()
    {
        return controller.target != null && 
               controller.distanceToTarget <= controller.aiData.attackRange &&
               controller.canSeeTarget;
    }
    
    private bool ShouldCombo()
    {
        return UnityEngine.Random.Range(0f, 1f) < controller.aiData.comboChance;
    }
    
    private bool CanUseSpecialSkill()
    {
        if (controller.energySystem == null) return false;
        
        return controller.energySystem.CanUseSpecialSkill() &&
               UnityEngine.Random.Range(0f, 1f) < controller.aiData.specialSkillChance;
    }
    
    private bool HasTarget()
    {
        return controller.target != null;
    }
    
    private bool IsTargetOutOfRange()
    {
        return controller.distanceToTarget > controller.aiData.attackRange;
    }
    
    private bool IsTargetTooClose()
    {
        return controller.distanceToTarget < controller.aiData.retreatDistance;
    }
    
    private bool ShouldStrafe()
    {
        // 根据AI的移动偏好决定是否侧移
        return controller.target != null &&
               controller.distanceToTarget < controller.aiData.followDistance * 1.5f &&
               UnityEngine.Random.Range(0f, 1f) < controller.aiData.mobility * 0.3f;
    }
    
    #endregion
    
    #region 行为执行
    
    private NodeStatus ExecuteComboAttack()
    {
        // 执行连击攻击
        bool success = controller.TryAttack();
        if (success)
        {
            // 设置连击标记
            SetBlackboardValue("ComboActive", true);
            SetBlackboardValue("ComboStartTime", Time.time);
        }
        return success ? NodeStatus.Success : NodeStatus.Failure;
    }
    
    private NodeStatus UseSpecialSkill()
    {
        // 使用特殊技能的逻辑
        SpecialSkillController specialSkill = controller.GetComponent<SpecialSkillController>();
        if (specialSkill != null)
        {
            bool success = specialSkill.TryUseSpecialSkill();
            return success ? NodeStatus.Success : NodeStatus.Failure;
        }
        return NodeStatus.Failure;
    }
    
    private NodeStatus MoveAwayFromTarget()
    {
        if (controller.target != null)
        {
            Vector2 direction = (controller.transform.position - controller.target.position).normalized;
            Vector2 movePosition = (Vector2)controller.transform.position + direction * 1.5f;
            controller.MoveToPosition(movePosition);
            return NodeStatus.Success;
        }
        return NodeStatus.Failure;
    }
    
    private NodeStatus ExecuteStrafe()
    {
        if (controller.target != null)
        {
            // 计算侧移方向
            Vector2 toTarget = (controller.target.position - controller.transform.position).normalized;
            Vector2 strafeDirection = new Vector2(-toTarget.y, toTarget.x); // 垂直方向
            
            // 随机选择左右
            if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
            {
                strafeDirection = -strafeDirection;
            }
            
            Vector2 strafePosition = (Vector2)controller.transform.position + strafeDirection * 2f;
            controller.MoveToPosition(strafePosition);
            return NodeStatus.Success;
        }
        return NodeStatus.Failure;
    }
    
    #endregion
    
    #region 黑板操作
    
    public void SetBlackboardValue(string key, object value)
    {
        blackboard[key] = value;
    }
    
    public T GetBlackboardValue<T>(string key, T defaultValue = default(T))
    {
        if (blackboard.ContainsKey(key) && blackboard[key] is T)
        {
            return (T)blackboard[key];
        }
        return defaultValue;
    }
    
    public bool HasBlackboardValue(string key)
    {
        return blackboard.ContainsKey(key);
    }
    
    #endregion
}

#region 行为树节点

/// <summary>
/// 节点状态枚举
/// </summary>
public enum NodeStatus
{
    Success,    // 成功
    Failure,    // 失败
    Running     // 运行中
}

/// <summary>
/// 行为节点基类
/// </summary>
public abstract class BehaviorNode
{
    public string Name { get; set; }
    public List<BehaviorNode> Children { get; set; }
    
    protected AIController controller;
    protected Dictionary<string, object> blackboard;
    
    public BehaviorNode(string name)
    {
        Name = name;
        Children = new List<BehaviorNode>();
    }
    
    public virtual void SetContext(AIController controller, Dictionary<string, object> blackboard)
    {
        this.controller = controller;
        this.blackboard = blackboard;
    }
    
    public abstract NodeStatus Evaluate();
}

/// <summary>
/// 选择器节点 - 依次执行子节点直到有一个成功
/// </summary>
public class SelectorNode : BehaviorNode
{
    public SelectorNode(string name) : base(name) { }
    
    public override NodeStatus Evaluate()
    {
        foreach (var child in Children)
        {
            NodeStatus status = child.Evaluate();
            if (status == NodeStatus.Success || status == NodeStatus.Running)
            {
                return status;
            }
        }
        return NodeStatus.Failure;
    }
}

/// <summary>
/// 序列节点 - 依次执行子节点直到有一个失败
/// </summary>
public class SequenceNode : BehaviorNode
{
    public SequenceNode(string name) : base(name) { }
    
    public override NodeStatus Evaluate()
    {
        foreach (var child in Children)
        {
            NodeStatus status = child.Evaluate();
            if (status == NodeStatus.Failure || status == NodeStatus.Running)
            {
                return status;
            }
        }
        return NodeStatus.Success;
    }
}

/// <summary>
/// 条件节点 - 检查条件
/// </summary>
public class ConditionNode : BehaviorNode
{
    private Func<bool> condition;
    
    public ConditionNode(string name, Func<bool> condition) : base(name)
    {
        this.condition = condition;
    }
    
    public override NodeStatus Evaluate()
    {
        return condition() ? NodeStatus.Success : NodeStatus.Failure;
    }
}

/// <summary>
/// 行动节点 - 执行具体行动
/// </summary>
public class ActionNode : BehaviorNode
{
    private Func<NodeStatus> action;
    
    public ActionNode(string name, Func<NodeStatus> action) : base(name)
    {
        this.action = action;
    }
    
    public override NodeStatus Evaluate()
    {
        return action();
    }
}

/// <summary>
/// 装饰节点 - 修改子节点的行为
/// </summary>
public abstract class DecoratorNode : BehaviorNode
{
    protected BehaviorNode child;
    
    public DecoratorNode(string name, BehaviorNode child) : base(name)
    {
        this.child = child;
        if (child != null)
        {
            Children.Add(child);
        }
    }
}

/// <summary>
/// 反转节点 - 反转子节点的结果
/// </summary>
public class InverterNode : DecoratorNode
{
    public InverterNode(string name, BehaviorNode child) : base(name, child) { }
    
    public override NodeStatus Evaluate()
    {
        if (child == null) return NodeStatus.Failure;
        
        NodeStatus status = child.Evaluate();
        switch (status)
        {
            case NodeStatus.Success:
                return NodeStatus.Failure;
            case NodeStatus.Failure:
                return NodeStatus.Success;
            default:
                return status;
        }
    }
}

/// <summary>
/// 重复节点 - 重复执行子节点
/// </summary>
public class RepeaterNode : DecoratorNode
{
    private int maxRepeats;
    private int currentRepeats;
    
    public RepeaterNode(string name, BehaviorNode child, int maxRepeats = -1) : base(name, child)
    {
        this.maxRepeats = maxRepeats;
        this.currentRepeats = 0;
    }
    
    public override NodeStatus Evaluate()
    {
        if (child == null) return NodeStatus.Failure;
        
        if (maxRepeats > 0 && currentRepeats >= maxRepeats)
        {
            currentRepeats = 0;
            return NodeStatus.Success;
        }
        
        NodeStatus status = child.Evaluate();
        if (status == NodeStatus.Success || status == NodeStatus.Failure)
        {
            currentRepeats++;
        }
        
        return NodeStatus.Running;
    }
}

#endregion