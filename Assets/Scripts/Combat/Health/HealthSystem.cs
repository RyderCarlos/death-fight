  using UnityEngine;
  using System.Collections;

  public class HealthSystem : MonoBehaviour
  {
      [Header("血量设置")]
      [Tooltip("最大血量")]
      public int maxHealth = 100;
      [Tooltip("当前血量")]
      [SerializeField] private int currentHealth;
      [Tooltip("是否已死亡")]
      [SerializeField] private bool isDead = false;

      [Header("受伤设置")]
      [Tooltip("无敌时间（秒）")]
      public float invincibilityTime = 0.5f;
      [Tooltip("击中硬直时间（秒）")]
      public float hitstunTime = 0.3f;
      [Tooltip("是否有无敌帧")]
      [SerializeField] private bool isInvincible = false;

      [Header("视觉反馈")]
      [Tooltip("受伤时的颜色")]
      public Color hurtColor = Color.red;
      [Tooltip("受伤闪烁持续时间")]
      public float hurtFlashDuration = 0.1f;
      [Tooltip("死亡时的颜色")]
      public Color deathColor = Color.gray;

      [Header("生命恢复")]
      [Tooltip("是否自动恢复血量")]
      public bool autoRegeneration = false;
      [Tooltip("血量恢复速度（每秒）")]
      public float regenRate = 2f;
      [Tooltip("受伤后多久开始恢复（秒）")]
      public float regenDelay = 3f;
      [Tooltip("血量恢复上限百分比")]
      [Range(0f, 1f)]
      public float regenLimit = 1f;

      [Header("暴击系统")]
      [Tooltip("暴击概率（0-1）")]
      [Range(0f, 1f)]
      public float criticalChance = 0.1f;
      [Tooltip("暴击伤害倍数")]
      public float criticalMultiplier = 1.5f;

      [Header("组件引用")]
      private SpriteRenderer spriteRenderer;
      private Animator animator;
      private Rigidbody2D rb;
      private Collider2D col;
      private Color originalColor;

      [Header("调试信息")]
      [SerializeField] private int totalDamageTaken = 0;
      [SerializeField] private int totalDamageDealt = 0;
      [SerializeField] private int timesHealed = 0;
      [SerializeField] private float lastDamageTime = 0f;

      // 协程引用
      private Coroutine regenCoroutine;
      private Coroutine hurtFlashCoroutine;
      private Coroutine invincibilityCoroutine;
      private Coroutine hitstunCoroutine;

      // 事件系统
      public System.Action<int, int> OnHealthChanged; // current, max
      public System.Action<DamageInfo> OnTakeDamage;
      public System.Action<int> OnHeal;
      public System.Action OnDeath;
      public System.Action<int> OnRevive;
      public System.Action<DamageInfo> OnCriticalHit;
      public System.Action OnInvincibilityStart;
      public System.Action OnInvincibilityEnd;

      void Start()
      {
          InitializeComponents();
          InitializeHealth();
          StartAutoRegeneration();
      }

      /// <summary>
      /// 初始化组件引用
      /// </summary>
      void InitializeComponents()
      {
          spriteRenderer = GetComponent<SpriteRenderer>();
          animator = GetComponent<Animator>();
          rb = GetComponent<Rigidbody2D>();
          col = GetComponent<Collider2D>();

          if (spriteRenderer != null)
          {
              originalColor = spriteRenderer.color;
          }
      }

      /// <summary>
      /// 初始化血量
      /// </summary>
      void InitializeHealth()
      {
          currentHealth = maxHealth;
          isDead = false;
          OnHealthChanged?.Invoke(currentHealth, maxHealth);

          Debug.Log($"{gameObject.name} 血量系统初始化：{currentHealth}/{maxHealth}");
      }

      /// <summary>
      /// 开始自动血量恢复
      /// </summary>
      void StartAutoRegeneration()
      {
          if (autoRegeneration)
          {
              if (regenCoroutine == null)
              {
                  regenCoroutine = StartCoroutine(RegenerationCoroutine());
              }
          }
      }

      /// <summary>
      /// 血量恢复协程
      /// </summary>
      IEnumerator RegenerationCoroutine()
      {
          while (true)
          {
              yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次

              if (!isDead && currentHealth < GetRegenLimit() &&
                  Time.time - lastDamageTime >= regenDelay)
              {
                  int regenAmount = Mathf.RoundToInt(regenRate * 0.1f);
                  if (regenAmount > 0)
                  {
                      Heal(regenAmount, true);
                  }
              }
          }
      }

      /// <summary>
      /// 获取血量恢复上限
      /// </summary>
      int GetRegenLimit()
      {
          return Mathf.RoundToInt(maxHealth * regenLimit);
      }

      /// <summary>
      /// 受到伤害
      /// </summary>
      public void TakeDamage(DamageInfo damageInfo)
      {
          if (isDead || isInvincible)
          {
              Debug.Log($"{gameObject.name} 无法受到伤害：" + (isDead ? "已死亡" : "无敌状态"));
              return;
          }

          // 检查闪避无敌
          DefenseSystem defense = GetComponent<DefenseSystem>();
          if (defense != null && defense.IsInvincible())
          {
              Debug.Log($"{gameObject.name} 闪避无敌，免疫伤害");
              return;
          }

          // 计算最终伤害
          CalculateFinalDamage(damageInfo);

          // 检查暴击
          CheckCriticalHit(damageInfo);

          int finalDamage = Mathf.RoundToInt(damageInfo.finalDamage);

          // 扣除血量
          int oldHealth = currentHealth;
          currentHealth = Mathf.Max(0, currentHealth - finalDamage);
          totalDamageTaken += finalDamage;
          lastDamageTime = Time.time;

          // 触发事件
          OnTakeDamage?.Invoke(damageInfo);
          OnHealthChanged?.Invoke(currentHealth, maxHealth);

          // 暴击事件
          if (damageInfo.isCritical)
          {
              OnCriticalHit?.Invoke(damageInfo);
          }

          Debug.Log($"{gameObject.name} 受到 {finalDamage} 点伤害" +
                   (damageInfo.isCritical ? "（暴击！）" : "") +
                   $"，剩余血量：{currentHealth}/{maxHealth}");

          // 视觉反馈
          StartHurtFlash();

          // 击退效果
          ApplyKnockback(damageInfo);

          // 硬直效果
          if (damageInfo.attackData != null)
          {
              ApplyHitstun(damageInfo.attackData.hitstun);
          }

          // 无敌时间
          StartInvincibilityFrames();

          // 检查死亡
          if (currentHealth <= 0)
          {
              Die(damageInfo);
          }
          else
          {
              // 播放受伤动画
              PlayHurtAnimation();
          }
      }

      /// <summary>
      /// 计算最终伤害
      /// </summary>
      void CalculateFinalDamage(DamageInfo damageInfo)
      {
          float damage = damageInfo.damage;

          // 连击伤害加成
          ComboSystem comboSystem = damageInfo.attacker.GetComponent<ComboSystem>();
          if (comboSystem != null)
          {
              damage *= comboSystem.GetComboMultiplier();
          }

          // 格挡减伤在DefenseSystem中已处理
          if (damageInfo.isBlocked)
          {
              // 格挡减伤已经计算过了
              damage = damageInfo.finalDamage;
          }

          damageInfo.finalDamage = damage;
      }

      /// <summary>
      /// 检查暴击
      /// </summary>
      void CheckCriticalHit(DamageInfo damageInfo)
      {
          if (!damageInfo.isBlocked && Random.value <= criticalChance)
          {
              damageInfo.isCritical = true;
              damageInfo.finalDamage *= criticalMultiplier;
          }
      }

      /// <summary>
      /// 播放受伤闪烁效果
      /// </summary>
      void StartHurtFlash()
      {
          if (hurtFlashCoroutine != null)
          {
              StopCoroutine(hurtFlashCoroutine);
          }
          hurtFlashCoroutine = StartCoroutine(HurtFlashCoroutine());
      }

      /// <summary>
      /// 受伤闪烁协程
      /// </summary>
      IEnumerator HurtFlashCoroutine()
      {
          if (spriteRenderer != null)
          {
              spriteRenderer.color = hurtColor;
              yield return new WaitForSeconds(hurtFlashDuration);

              if (!isDead) // 如果没死亡才恢复原色
              {
                  spriteRenderer.color = originalColor;
              }
          }
      }

      /// <summary>
      /// 应用击退效果
      /// </summary>
      void ApplyKnockback(DamageInfo damageInfo)
      {
          if (rb != null && damageInfo.attackData != null && damageInfo.attackData.knockbackForce > 0)
          {
              Vector2 knockbackDirection = (transform.position - damageInfo.attacker.transform.position).normalized;
              Vector2 knockbackForce = knockbackDirection * damageInfo.attackData.knockbackForce;

              rb.AddForce(knockbackForce, ForceMode2D.Impulse);

              Debug.Log($"应用击退力：{knockbackForce}");
          }
      }

      /// <summary>
      /// 应用硬直效果
      /// </summary>
      void ApplyHitstun(float duration)
      {
          if (duration <= 0) return;

          if (hitstunCoroutine != null)
          {
              StopCoroutine(hitstunCoroutine);
          }
          hitstunCoroutine = StartCoroutine(HitstunCoroutine(duration));
      }

      /// <summary>
      /// 硬直协程
      /// </summary>
      IEnumerator HitstunCoroutine(float duration)
      {
          PlayerController controller = GetComponent<PlayerController>();
          AttackSystem attackSystem = GetComponent<AttackSystem>();

          bool controllerWasEnabled = controller != null ? controller.enabled : false;
          bool attackWasEnabled = attackSystem != null ? attackSystem.enabled : false;

          // 禁用控制和攻击
          if (controller != null) controller.enabled = false;
          if (attackSystem != null) attackSystem.enabled = false;

          Debug.Log($"开始硬直：{duration}秒");

          yield return new WaitForSeconds(duration);

          // 恢复控制
          if (controller != null) controller.enabled = controllerWasEnabled;
          if (attackSystem != null) attackSystem.enabled = attackWasEnabled;

          Debug.Log("硬直结束");
      }

      /// <summary>
      /// 开始无敌时间
      /// </summary>
      void StartInvincibilityFrames()
      {
          if (invincibilityCoroutine != null)
          {
              StopCoroutine(invincibilityCoroutine);
          }
          invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine());
      }

      /// <summary>
      /// 无敌时间协程
      /// </summary>
      IEnumerator InvincibilityCoroutine()
      {
          isInvincible = true;
          OnInvincibilityStart?.Invoke();

          Debug.Log("开始无敌时间");

          // 闪烁效果
          float blinkInterval = 0.1f;
          float elapsed = 0f;

          while (elapsed < invincibilityTime)
          {
              if (spriteRenderer != null && !isDead)
              {
                  Color blinkColor = originalColor;
                  blinkColor.a = 0.5f;
                  spriteRenderer.color = blinkColor;
              }
              yield return new WaitForSeconds(blinkInterval);

              if (spriteRenderer != null && !isDead)
              {
                  spriteRenderer.color = originalColor;
              }
              yield return new WaitForSeconds(blinkInterval);

              elapsed += blinkInterval * 2;
          }

          isInvincible = false;
          OnInvincibilityEnd?.Invoke();

          Debug.Log("无敌时间结束");
      }

      /// <summary>
      /// 播放受伤动画
      /// </summary>
      void PlayHurtAnimation()
      {
          if (animator != null)
          {
              animator.SetTrigger("Hurt");
          }
      }

      /// <summary>
      /// 治疗
      /// </summary>
      public void Heal(int amount, bool isRegeneration = false)
      {
          if (isDead || amount <= 0) return;

          int oldHealth = currentHealth;
          currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

          if (currentHealth != oldHealth)
          {
              if (!isRegeneration)
              {
                  timesHealed++;
              }

              OnHeal?.Invoke(amount);
              OnHealthChanged?.Invoke(currentHealth, maxHealth);

              Debug.Log($"{gameObject.name} 恢复了 {amount} 点血量，当前血量：{currentHealth}/{maxHealth}");
          }
      }

      /// <summary>
      /// 死亡
      /// </summary>
      void Die(DamageInfo damageInfo = null)
      {
          if (isDead) return;

          isDead = true;

          Debug.Log($"{gameObject.name} 死亡");

          // 停止所有协程
          StopAllHealthCoroutines();

          // 改变颜色为死亡色
          if (spriteRenderer != null)
          {
              spriteRenderer.color = deathColor;
          }

          // 播放死亡动画
          if (animator != null)
          {
              animator.SetTrigger("Death");
              animator.SetBool("IsDead", true);
          }

          // 禁用控制
          DisableComponents();

          OnDeath?.Invoke();
      }

      /// <summary>
      /// 复活
      /// </summary>
      public void Revive(int healthAmount = -1)
      {
          if (!isDead) return;

          isDead = false;
          currentHealth = healthAmount > 0 ? Mathf.Min(healthAmount, maxHealth) : maxHealth;

          Debug.Log($"{gameObject.name} 复活，血量：{currentHealth}/{maxHealth}");

          // 恢复颜色
          if (spriteRenderer != null)
          {
              spriteRenderer.color = originalColor;
          }

          // 重新启用组件
          EnableComponents();

          // 播放复活动画
          if (animator != null)
          {
              animator.SetTrigger("Revive");
              animator.SetBool("IsDead", false);
          }

          // 重新开始自动恢复
          StartAutoRegeneration();

          OnHealthChanged?.Invoke(currentHealth, maxHealth);
          OnRevive?.Invoke(currentHealth);
      }

      /// <summary>
      /// 停止所有血量相关协程
      /// </summary>
      void StopAllHealthCoroutines()
      {
          if (regenCoroutine != null)
          {
              StopCoroutine(regenCoroutine);
              regenCoroutine = null;
          }

          if (hurtFlashCoroutine != null)
          {
              StopCoroutine(hurtFlashCoroutine);
          }

          if (invincibilityCoroutine != null)
          {
              StopCoroutine(invincibilityCoroutine);
          }

          if (hitstunCoroutine != null)
          {
              StopCoroutine(hitstunCoroutine);
          }
      }

      /// <summary>
      /// 禁用组件
      /// </summary>
      void DisableComponents()
      {
          PlayerController controller = GetComponent<PlayerController>();
          if (controller != null) controller.enabled = false;

          AttackSystem attackSystem = GetComponent<AttackSystem>();
          if (attackSystem != null) attackSystem.enabled = false;

          DefenseSystem defenseSystem = GetComponent<DefenseSystem>();
          if (defenseSystem != null) defenseSystem.enabled = false;

          // 可以选择禁用碰撞体
          if (col != null)
          {
              col.enabled = false;
          }
      }

      /// <summary>
      /// 启用组件
      /// </summary>
      void EnableComponents()
      {
          PlayerController controller = GetComponent<PlayerController>();
          if (controller != null) controller.enabled = true;

          AttackSystem attackSystem = GetComponent<AttackSystem>();
          if (attackSystem != null) attackSystem.enabled = true;

          DefenseSystem defenseSystem = GetComponent<DefenseSystem>();
          if (defenseSystem != null) defenseSystem.enabled = true;

          if (col != null)
          {
              col.enabled = true;
          }
      }

      /// <summary>
      /// 设置最大血量
      /// </summary>
      public void SetMaxHealth(int newMaxHealth, bool healToFull = false)
      {
          maxHealth = newMaxHealth;

          if (healToFull)
          {
              currentHealth = maxHealth;
          }
          else
          {
              currentHealth = Mathf.Min(currentHealth, maxHealth);
          }

          OnHealthChanged?.Invoke(currentHealth, maxHealth);

          Debug.Log($"{gameObject.name} 最大血量设置为：{maxHealth}");
      }

      /// <summary>
      /// 获取血量百分比
      /// </summary>
      public float GetHealthPercentage()
      {
          return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
      }

      /// <summary>
      /// 获取血量状态
      /// </summary>
      public HealthStatus GetHealthStatus()
      {
          if (isDead) return HealthStatus.死亡;

          float percentage = GetHealthPercentage();
          if (percentage >= 0.75f) return HealthStatus.健康;
          if (percentage >= 0.5f) return HealthStatus.良好;
          if (percentage >= 0.25f) return HealthStatus.受伤;
          return HealthStatus.危险;
      }

      /// <summary>
      /// 获取健康统计信息
      /// </summary>
      public HealthStats GetHealthStats()
      {
          return new HealthStats
          {
              totalDamageTaken = totalDamageTaken,
              totalDamageDealt = totalDamageDealt,
              timesHealed = timesHealed,
              currentHealth = currentHealth,
              maxHealth = maxHealth,
              isDead = isDead
          };
      }

      /// <summary>
      /// 重置健康统计
      /// </summary>
      public void ResetHealthStats()
      {
          totalDamageTaken = 0;
          totalDamageDealt = 0;
          timesHealed = 0;
      }

      // 属性访问器
      public int CurrentHealth => currentHealth;
      public int MaxHealth => maxHealth;
      public bool IsDead => isDead;
      public bool IsAlive => !isDead;
      public bool IsInvincible => isInvincible;

      void OnDestroy()
      {
          StopAllHealthCoroutines();
      }
  }

  /// <summary>
  /// 血量状态枚举
  /// </summary>
  public enum HealthStatus
  {
      健康,
      良好,
      受伤,
      危险,
      死亡
  }

  /// <summary>
  /// 健康统计信息结构
  /// </summary>
  [System.Serializable]
  public struct HealthStats
  {
      public int totalDamageTaken;
      public int totalDamageDealt;
      public int timesHealed;
      public int currentHealth;
      public int maxHealth;
      public bool isDead;
  }