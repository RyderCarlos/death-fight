  using UnityEngine;
  using System.Collections;

  public class DefenseSystem : MonoBehaviour
  {
      [Header("格挡设置")]
      [Tooltip("格挡伤害减免比例（0-1）")]
      [Range(0f, 1f)]
      public float blockDamageReduction = 0.5f;
      [Tooltip("格挡消耗的能量")]
      public int blockEnergyCost = 5;
      [Tooltip("成功格挡获得的能量")]
      public int blockEnergyGain = 8;

      [Header("闪避设置")]
      [Tooltip("闪避窗口时间")]
      public float dodgeWindow = 0.2f;
      [Tooltip("闪避无敌时间")]
      public float dodgeInvincibilityTime = 0.3f;
      [Tooltip("闪避冷却时间")]
      public float dodgeCooldown = 1f;
      [Tooltip("闪避消耗的能量")]
      public int dodgeEnergyCost = 20;
      [Tooltip("闪避距离")]
      public float dodgeDistance = 3f;
      [Tooltip("闪避速度")]
      public float dodgeSpeed = 10f;

      [Header("反击设置")]
      [Tooltip("格挡后反击窗口时间")]
      public float counterAttackWindow = 0.5f;
      [Tooltip("反击伤害倍数")]
      public float counterAttackMultiplier = 1.5f;

      [Header("当前状态")]
      [SerializeField] private bool isBlocking = false;
      [SerializeField] private bool isDodging = false;
      [SerializeField] private bool isInvincible = false;
      [SerializeField] private bool canDodge = true;
      [SerializeField] private bool canCounterAttack = false;

      [Header("组件引用")]
      private Animator animator;
      private Rigidbody2D rb;
      private EnergySystem energySystem;

      [Header("调试信息")]
      [SerializeField] private int totalBlocks = 0;
      [SerializeField] private int totalDodges = 0;
      [SerializeField] private int successfulCounters = 0;

      // 事件系统
      public System.Action<DamageInfo> OnBlock;
      public System.Action<DamageInfo> OnFailedBlock;
      public System.Action OnDodgeStart;
      public System.Action OnDodgeEnd;
      public System.Action<AttackData> OnCounterAttack;

      private Coroutine blockStunCoroutine;
      private Coroutine dodgeCoroutine;
      private Coroutine counterWindowCoroutine;

      void Start()
      {
          InitializeComponents();
      }

      void Update()
      {
          HandleDefenseInput();
      }

      /// <summary>
      /// 初始化组件引用
      /// </summary>
      void InitializeComponents()
      {
          animator = GetComponent<Animator>();
          rb = GetComponent<Rigidbody2D>();
          energySystem = GetComponent<EnergySystem>();

          if (rb == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 Rigidbody2D 组件，闪避功能可能无法正常工作");
          }
      }

      /// <summary>
      /// 处理防御输入
      /// </summary>
      void HandleDefenseInput()
      {
          // 格挡输入
          if (Input.GetKey(KeyCode.K))
          {
              TryStartBlock();
          }
          else
          {
              StopBlock();
          }

          // 闪避输入
          if (Input.GetKeyDown(KeyCode.L))
          {
              TryStartDodge();
          }
      }

      /// <summary>
      /// 尝试开始格挡
      /// </summary>
      public bool TryStartBlock()
      {
          if (isDodging) return false;

          // 检查能量是否足够
          if (energySystem != null && !energySystem.HasEnergy(blockEnergyCost))
          {
              Debug.Log("能量不足，无法格挡");
              return false;
          }

          StartBlock();
          return true;
      }

      /// <summary>
      /// 开始格挡
      /// </summary>
      void StartBlock()
      {
          if (isBlocking) return;

          isBlocking = true;

          // 播放格挡动画
          if (animator != null)
          {
              animator.SetBool("IsBlocking", true);
          }

          Debug.Log("开始格挡");
      }

      /// <summary>
      /// 停止格挡
      /// </summary>
      public void StopBlock()
      {
          if (!isBlocking) return;

          isBlocking = false;

          // 停止格挡动画
          if (animator != null)
          {
              animator.SetBool("IsBlocking", false);
          }

          Debug.Log("停止格挡");
      }

      /// <summary>
      /// 尝试开始闪避
      /// </summary>
      public bool TryStartDodge()
      {
          if (!canDodge || isDodging) return false;

          // 检查能量是否足够
          if (energySystem != null && !energySystem.HasEnergy(dodgeEnergyCost))
          {
              Debug.Log("能量不足，无法闪避");
              return false;
          }

          StartDodge();
          return true;
      }

      /// <summary>
      /// 开始闪避
      /// </summary>
      void StartDodge()
      {
          isDodging = true;
          isInvincible = true;
          canDodge = false;
          totalDodges++;

          // 停止格挡
          StopBlock();

          // 消耗能量
          if (energySystem != null)
          {
              energySystem.ConsumeEnergy(dodgeEnergyCost);
          }

          // 播放闪避动画
          if (animator != null)
          {
              animator.SetTrigger("Dodge");
          }

          OnDodgeStart?.Invoke();

          Debug.Log("开始闪避");

          if (dodgeCoroutine != null)
          {
              StopCoroutine(dodgeCoroutine);
          }
          dodgeCoroutine = StartCoroutine(DodgeSequence());
      }

      /// <summary>
      /// 闪避序列协程
      /// </summary>
      IEnumerator DodgeSequence()
      {
          // 执行闪避移动
          PerformDodgeMovement();

          // 闪避无敌时间
          yield return new WaitForSeconds(dodgeInvincibilityTime);

          isInvincible = false;

          // 闪避动作完成
          yield return new WaitForSeconds(dodgeWindow - dodgeInvincibilityTime);

          isDodging = false;
          OnDodgeEnd?.Invoke();

          Debug.Log("闪避动作完成");

          // 闪避冷却
          yield return new WaitForSeconds(dodgeCooldown);

          canDodge = true;

          Debug.Log("闪避冷却完成");
      }

      /// <summary>
      /// 执行闪避移动
      /// </summary>
      void PerformDodgeMovement()
      {
          if (rb == null) return;

          // 获取移动方向（这里简化为向左移动，实际应该根据输入方向）
          Vector2 dodgeDirection = GetDodgeDirection();

          // 应用闪避力
          rb.velocity = dodgeDirection * dodgeSpeed;

          // 在闪避结束时停止移动
          StartCoroutine(StopDodgeMovement());
      }

      /// <summary>
      /// 获取闪避方向
      /// </summary>
      Vector2 GetDodgeDirection()
      {
          // 简化版本：根据水平输入决定方向
          float horizontalInput = Input.GetAxisRaw("Horizontal");

          if (Mathf.Abs(horizontalInput) > 0.1f)
          {
              return new Vector2(horizontalInput, 0).normalized;
          }
          else
          {
              // 默认向后闪避
              return new Vector2(-transform.localScale.x, 0).normalized;
          }
      }

      /// <summary>
      /// 停止闪避移动
      /// </summary>
      IEnumerator StopDodgeMovement()
      {
          yield return new WaitForSeconds(dodgeWindow);

          if (rb != null)
          {
              rb.velocity = Vector2.zero;
          }
      }

      /// <summary>
      /// 处理格挡
      /// </summary>
      public void HandleBlock(DamageInfo damageInfo)
      {
          if (!IsBlocking())
          {
              OnFailedBlock?.Invoke(damageInfo);
              return;
          }

          totalBlocks++;

          // 消耗格挡能量
          if (energySystem != null)
          {
              energySystem.ConsumeEnergy(blockEnergyCost);
          }

          // 计算格挡后的伤害
          damageInfo.finalDamage = damageInfo.damage * blockDamageReduction;
          damageInfo.isBlocked = true;

          // 播放格挡特效
          PlayBlockEffect(damageInfo);

          // 格挡硬直
          StartBlockStun(damageInfo.attackData.blockstun);

          // 获得格挡能量
          if (energySystem != null)
          {
              energySystem.GainEnergy(blockEnergyGain);
          }

          // 开启反击窗口
          StartCounterAttackWindow();

          OnBlock?.Invoke(damageInfo);

          Debug.Log($"成功格挡攻击，减少伤害至 {damageInfo.finalDamage}");
      }

      /// <summary>
      /// 播放格挡特效
      /// </summary>
      void PlayBlockEffect(DamageInfo damageInfo)
      {
          if (damageInfo.attackData.blockEffect != null)
          {
              GameObject effect = Instantiate(
                  damageInfo.attackData.blockEffect,
                  damageInfo.hitPosition,
                  Quaternion.identity
              );
              Destroy(effect, 2f);
          }
      }

      /// <summary>
      /// 开始格挡硬直
      /// </summary>
      void StartBlockStun(float stunTime)
      {
          if (blockStunCoroutine != null)
          {
              StopCoroutine(blockStunCoroutine);
          }
          blockStunCoroutine = StartCoroutine(BlockStunCoroutine(stunTime));
      }

      /// <summary>
      /// 格挡硬直协程
      /// </summary>
      IEnumerator BlockStunCoroutine(float stunTime)
      {
          // 在硬直期间限制行动
          PlayerController controller = GetComponent<PlayerController>();
          bool wasEnabled = controller != null ? controller.enabled : false;

          if (controller != null)
          {
              controller.enabled = false;
          }

          yield return new WaitForSeconds(stunTime);

          if (controller != null)
          {
              controller.enabled = wasEnabled;
          }

          Debug.Log("格挡硬直结束");
      }

      /// <summary>
      /// 开始反击窗口
      /// </summary>
      void StartCounterAttackWindow()
      {
          canCounterAttack = true;

          if (counterWindowCoroutine != null)
          {
              StopCoroutine(counterWindowCoroutine);
          }
          counterWindowCoroutine = StartCoroutine(CounterAttackWindowCoroutine());
      }

      /// <summary>
      /// 反击窗口协程
      /// </summary>
      IEnumerator CounterAttackWindowCoroutine()
      {
          yield return new WaitForSeconds(counterAttackWindow);
          canCounterAttack = false;

          Debug.Log("反击窗口结束");
      }

      /// <summary>
      /// 尝试反击
      /// </summary>
      public bool TryCounterAttack(AttackType attackType)
      {
          if (!canCounterAttack) return false;

          AttackSystem attackSystem = GetComponent<AttackSystem>();
          if (attackSystem != null)
          {
              bool success = attackSystem.TryAttack(attackType);
              if (success)
              {
                  successfulCounters++;
                  canCounterAttack = false;

                  // 这里可以添加反击伤害加成逻辑
                  OnCounterAttack?.Invoke(attackSystem.CurrentAttack);

                  Debug.Log("成功发动反击！");
              }
              return success;
          }

          return false;
      }

      // 状态查询方法
      public bool IsBlocking() => isBlocking && !isDodging;
      public bool IsInvincible() => isInvincible;
      public bool IsDodging() => isDodging;
      public bool CanCounterAttack() => canCounterAttack;

      /// <summary>
      /// 获取防御统计信息
      /// </summary>
      public (int blocks, int dodges, int counters) GetDefenseStats()
      {
          return (totalBlocks, totalDodges, successfulCounters);
      }

      /// <summary>
      /// 重置防御统计
      /// </summary>
      public void ResetDefenseStats()
      {
          totalBlocks = 0;
          totalDodges = 0;
          successfulCounters = 0;
      }

      /// <summary>
      /// 强制停止所有防御动作
      /// </summary>
      public void ForceStopAllDefense()
      {
          StopBlock();

          if (isDodging)
          {
              StopAllCoroutines();
              isDodging = false;
              isInvincible = false;
              canDodge = true;

              if (rb != null)
              {
                  rb.velocity = Vector2.zero;
              }
          }

          canCounterAttack = false;
      }

      void OnDestroy()
      {
          StopAllCoroutines();
      }
  }