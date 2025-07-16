  using UnityEngine;

  [System.Serializable]
  public class DamageInfo
  {
      [Header("伤害基础信息")]
      public int damage;                    // 原始伤害
      public GameObject attacker;           // 攻击者
      public AttackData attackData;         // 攻击数据
      public Vector3 hitPosition;           // 击中位置

      [Header("伤害处理结果")]
      public bool isBlocked = false;        // 是否被格挡
      public float finalDamage;             // 最终伤害
      public bool isCritical = false;       // 是否暴击
      public float damageMultiplier = 1f;   // 伤害倍数

      [Header("状态效果")]
      public float stunDuration = 0f;       // 眩晕持续时间
      public bool causesKnockdown = false;  // 是否击倒

      /// <summary>
      /// 计算最终伤害
      /// </summary>
      public void CalculateFinalDamage()
      {
          finalDamage = damage * damageMultiplier;

          if (isBlocked)
          {
              finalDamage *= 0.5f; // 格挡减伤50%
          }

          if (isCritical)
          {
              finalDamage *= 1.5f; // 暴击增伤50%
          }
      }
  }