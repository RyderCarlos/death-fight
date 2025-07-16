  using UnityEngine;

  [CreateAssetMenu(fileName = "New Combo", menuName = "战斗游戏/连击数据")]
  public class ComboData : ScriptableObject
  {
      [Header("连击基础信息")]
      [Tooltip("连击名称")]
      public string comboName;
      [Tooltip("连击描述")]
      [TextArea(2, 4)]
      public string comboDescription;
      [Tooltip("连击难度")]
      public ComboDifficulty difficulty = ComboDifficulty.简单;

      [Header("连击序列")]
      [Tooltip("攻击序列")]
      public AttackType[] attackSequence;
      [Tooltip("每个攻击之间的时间窗口")]
      public float[] timingWindows;
      [Tooltip("连击总时间限制")]
      public float comboTimeLimit = 3f;

      [Header("连击效果")]
      [Tooltip("连击伤害加成")]
      public float damageBonus = 1.5f;
      [Tooltip("连击完成后的额外伤害")]
      public int finisherDamage = 25;
      [Tooltip("连击获得的额外能量")]
      public int energyBonus = 20;

      [Header("视觉效果")]
      [Tooltip("连击特效")]
      public GameObject comboEffect;
      [Tooltip("连击完成特效")]
      public GameObject finisherEffect;
      [Tooltip("连击音效")]
      public AudioClip comboSound;
      [Tooltip("连击完成音效")]
      public AudioClip finisherSound;

      [Header("连击条件")]
      [Tooltip("需要的最低连击数")]
      public int minimumComboCount = 2;
      [Tooltip("是否需要连续命中")]
      public bool requireContinuousHits = true;
      [Tooltip("允许的失误次数")]
      public int allowedMisses = 0;

      /// <summary>
      /// 验证连击数据
      /// </summary>
      void OnValidate()
      {
          if (attackSequence != null && timingWindows != null)
          {
              if (timingWindows.Length != attackSequence.Length - 1)
              {
                  System.Array.Resize(ref timingWindows, Mathf.Max(0, attackSequence.Length - 1));
              }
          }
      }

      /// <summary>
      /// 获取连击步骤数
      /// </summary>
      public int GetStepCount()
      {
          return attackSequence != null ? attackSequence.Length : 0;
      }

      /// <summary>
      /// 获取指定步骤的时间窗口
      /// </summary>
      public float GetTimingWindow(int step)
      {
          if (timingWindows != null && step >= 0 && step < timingWindows.Length)
          {
              return timingWindows[step];
          }
          return 0.5f; // 默认窗口时间
      }
  }

  public enum ComboDifficulty
  {
      简单,
      普通,
      困难,
      专家
  }