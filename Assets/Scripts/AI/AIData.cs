using UnityEngine;

/// <summary>
/// AI数据配置 - 使用ScriptableObject实现数据驱动的AI行为
/// </summary>
[CreateAssetMenu(fileName = "新AI数据", menuName = "战斗游戏/AI数据")]
public class AIData : ScriptableObject
{
    [Header("基础设置")]
    [Tooltip("AI名称")]
    public string aiName = "基础AI";
    
    [Tooltip("AI类型")]
    public AIType aiType = AIType.平衡型;
    
    [Header("检测设置")]
    [Tooltip("最大检测范围")]
    [Range(1f, 20f)]
    public float maxDetectionRange = 8f;
    
    [Tooltip("目标搜索间隔")]
    [Range(0.1f, 2f)]
    public float targetSearchInterval = 0.5f;
    
    [Tooltip("视野角度（度）")]
    [Range(30f, 360f)]
    public float viewAngle = 120f;
    
    [Header("移动设置")]
    [Tooltip("移动速度")]
    [Range(1f, 10f)]
    public float moveSpeed = 3f;
    
    [Tooltip("跟踪距离")]
    [Range(0.5f, 5f)]
    public float followDistance = 2f;
    
    [Tooltip("撤退距离")]
    [Range(0.5f, 3f)]
    public float retreatDistance = 1f;
    
    [Header("攻击设置")]
    [Tooltip("攻击范围")]
    [Range(0.5f, 5f)]
    public float attackRange = 2f;
    
    [Tooltip("攻击冷却时间")]
    [Range(0.1f, 3f)]
    public float attackCooldown = 1f;
    
    [Tooltip("攻击频率（每秒攻击次数）")]
    [Range(0.1f, 3f)]
    public float attackFrequency = 1f;
    
    [Tooltip("连击概率")]
    [Range(0f, 1f)]
    public float comboChance = 0.3f;
    
    [Header("防御设置")]
    [Tooltip("防御成功率")]
    [Range(0f, 1f)]
    public float defenseSuccessRate = 0.6f;
    
    [Tooltip("完美格挡概率")]
    [Range(0f, 1f)]
    public float perfectBlockChance = 0.1f;
    
    [Tooltip("闪避概率")]
    [Range(0f, 1f)]
    public float dodgeChance = 0.2f;
    
    [Tooltip("反击概率")]
    [Range(0f, 1f)]
    public float counterAttackChance = 0.3f;
    
    [Tooltip("反击延迟")]
    [Range(0f, 1f)]
    public float counterAttackDelay = 0.2f;
    
    [Header("反应设置")]
    [Tooltip("反应时间")]
    [Range(0f, 2f)]
    public float reactionTime = 0.3f;
    
    [Tooltip("决策间隔")]
    [Range(0.1f, 1f)]
    public float decisionInterval = 0.2f;
    
    [Tooltip("行为变化概率")]
    [Range(0f, 1f)]
    public float behaviorChangeChance = 0.1f;
    
    [Header("特殊技能")]
    [Tooltip("使用特殊技能概率")]
    [Range(0f, 1f)]
    public float specialSkillChance = 0.15f;
    
    [Tooltip("特殊技能冷却")]
    [Range(5f, 30f)]
    public float specialSkillCooldown = 10f;
    
    [Header("状态偏好")]
    [Tooltip("激进程度（更倾向于攻击）")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.5f;
    
    [Tooltip("防御偏好")]
    [Range(0f, 1f)]
    public float defensiveness = 0.5f;
    
    [Tooltip("移动偏好")]
    [Range(0f, 1f)]
    public float mobility = 0.5f;
    
    [Header("难度调节")]
    [Tooltip("AI难度等级")]
    public AIDifficulty difficulty = AIDifficulty.普通;
    
    [Header("行为权重")]
    [Tooltip("攻击行为权重")]
    [Range(0f, 10f)]
    public float attackWeight = 5f;
    
    [Tooltip("防御行为权重")]
    [Range(0f, 10f)]
    public float defenseWeight = 3f;
    
    [Tooltip("移动行为权重")]
    [Range(0f, 10f)]
    public float moveWeight = 4f;
    
    [Tooltip("等待行为权重")]
    [Range(0f, 10f)]
    public float waitWeight = 2f;
    
    [Header("调试设置")]
    [Tooltip("启用调试信息")]
    public bool enableDebug = false;
    
    [Tooltip("显示AI思考过程")]
    public bool showThinkingProcess = false;
    
    /// <summary>
    /// 根据难度调整AI数据
    /// </summary>
    public void ApplyDifficultyModifier()
    {
        switch (difficulty)
        {
            case AIDifficulty.简单:
                reactionTime *= 1.5f;
                defenseSuccessRate *= 0.7f;
                attackFrequency *= 0.7f;
                perfectBlockChance *= 0.5f;
                counterAttackChance *= 0.6f;
                break;
                
            case AIDifficulty.普通:
                // 使用默认值
                break;
                
            case AIDifficulty.困难:
                reactionTime *= 0.8f;
                defenseSuccessRate *= 1.2f;
                attackFrequency *= 1.3f;
                perfectBlockChance *= 1.5f;
                counterAttackChance *= 1.4f;
                specialSkillChance *= 1.3f;
                break;
                
            case AIDifficulty.专家:
                reactionTime *= 0.6f;
                defenseSuccessRate *= 1.4f;
                attackFrequency *= 1.6f;
                perfectBlockChance *= 2f;
                counterAttackChance *= 1.8f;
                specialSkillChance *= 1.6f;
                comboChance *= 1.5f;
                break;
                
            case AIDifficulty.自定义:
                // 不做修改，使用设定的值
                break;
        }
        
        // 确保数值在合理范围内
        ClampValues();
    }
    
    /// <summary>
    /// 限制数值范围
    /// </summary>
    private void ClampValues()
    {
        defenseSuccessRate = Mathf.Clamp01(defenseSuccessRate);
        perfectBlockChance = Mathf.Clamp01(perfectBlockChance);
        dodgeChance = Mathf.Clamp01(dodgeChance);
        counterAttackChance = Mathf.Clamp01(counterAttackChance);
        specialSkillChance = Mathf.Clamp01(specialSkillChance);
        comboChance = Mathf.Clamp01(comboChance);
        reactionTime = Mathf.Max(0f, reactionTime);
        attackFrequency = Mathf.Max(0.1f, attackFrequency);
    }
    
    /// <summary>
    /// 获取行为权重总和
    /// </summary>
    public float GetTotalBehaviorWeight()
    {
        return attackWeight + defenseWeight + moveWeight + waitWeight;
    }
    
    /// <summary>
    /// 获取标准化的行为权重
    /// </summary>
    public Vector4 GetNormalizedBehaviorWeights()
    {
        float total = GetTotalBehaviorWeight();
        if (total <= 0f) return Vector4.one * 0.25f;
        
        return new Vector4(
            attackWeight / total,
            defenseWeight / total,
            moveWeight / total,
            waitWeight / total
        );
    }
}

/// <summary>
/// AI类型枚举
/// </summary>
public enum AIType
{
    攻击型,       // 主要进行攻击
    防御型,       // 主要进行防御
    平衡型,       // 攻防平衡
    敏捷型,       // 移动为主
    技巧型        // 使用特殊技能
}

/// <summary>
/// AI难度枚举
/// </summary>
public enum AIDifficulty
{
    简单,
    普通,
    困难,
    专家,
    自定义
}