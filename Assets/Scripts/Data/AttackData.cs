using UnityEngine;

[CreateAssetMenu(fileName = "新攻击数据", menuName = "战斗游戏/攻击数据")]
public class AttackData : ScriptableObject
{
    [Header("基础属性")]
    public string attackName = "默认攻击";
    public AttackType attackType = AttackType.轻拳;
    public float damage = 10f;
    public float energyCost = 0f;
    public float energyGain = 10f;
    
    [Header("攻击范围")]
    public float attackRange = 1f;
    public float attackWidth = 0.5f;
    public bool useCircleDetection = true;
    
    [Header("时间设置")]
    public float startupTime = 0.1f;    // 前摇时间
    public float activeTime = 0.1f;     // 判定时间
    public float recoveryTime = 0.3f;   // 后摇时间
    
    [Header("效果")]
    public float knockbackForce = 2f;
    public float hitstunTime = 0.2f;
    public float blockstunTime = 0.15f;
    
    [Header("动画")]
    public string animationTrigger = "Attack";
    
    [Header("音效")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    
    // Compatibility properties
    public float range => attackRange;
}

public enum AttackType
{
    轻拳 = 0,
    重拳 = 1,
    轻腿 = 2,
    重腿 = 3,
    特殊技能 = 4
}