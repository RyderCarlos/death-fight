  using UnityEngine;

  [CreateAssetMenu(fileName = "New Attack", menuName = "战斗游戏/攻击数据")]
  public class AttackData : ScriptableObject
  {
      [Header("基础攻击属性")]
      [Tooltip("攻击名称")]
      public string attackName;
      [Tooltip("攻击类型")]
      public AttackType attackType;
      [Tooltip("基础伤害")]
      public int damage = 10;
      [Tooltip("攻击范围")]
      public float range = 1.5f;
      [Tooltip("攻击前摇时间（秒）")]
      public float startupTime = 0.1f;
      [Tooltip("攻击判定持续时间（秒）")]
      public float activeTime = 0.2f;
      [Tooltip("攻击后摇时间（秒）")]
      public float recoveryTime = 0.3f;

      [Header("攻击效果")]
      [Tooltip("击退力度")]
      public float knockbackForce = 5f;
      [Tooltip("击中硬直时间（秒）")]
      public float hitstun = 0.3f;
      [Tooltip("格挡硬直时间（秒）")]
      public float blockstun = 0.2f;
      [Tooltip("攻击获得的能量")]
      public int energyGain = 5;
      [Tooltip("消耗的能量")]
      public int energyCost = 0;

      [Header("连击属性")]
      [Tooltip("是否可以连击")]
      public bool canCombo = true;
      [Tooltip("连击窗口时间（秒）")]
      public float comboWindow = 0.5f;
      [Tooltip("可以连接的攻击")]
      public AttackData[] comboOptions;

      [Header("音效和特效")]
      [Tooltip("攻击音效")]
      public AudioClip attackSound;
      [Tooltip("击中特效")]
      public GameObject hitEffect;
      [Tooltip("格挡特效")]
      public GameObject blockEffect;

      [Header("动画设置")]
      [Tooltip("动画触发器名称")]
      public string animationTrigger = "Attack";
      [Tooltip("动画状态名称")]
      public string animationStateName;
  }

  public enum AttackType
  {
      轻拳 = 0,
      重拳 = 1,
      轻腿 = 2,
      重腿 = 3,
      特殊技能 = 4
  }