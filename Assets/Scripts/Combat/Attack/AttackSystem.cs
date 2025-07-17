  using UnityEngine;
  using System.Collections;
  using System.Collections.Generic;

  public class AttackSystem : MonoBehaviour
  {
      [Header("攻击数据配置")]
      [Tooltip("可用的攻击数据数组")]
      public AttackData[] availableAttacks;
      [Tooltip("默认攻击数据")]
      public AttackData defaultAttack;

      [Header("组件引用")]
      private AttackDetector attackDetector;
      private Animator animator;
      private AudioSource audioSource;
      private Rigidbody2D rb;

      [Header("攻击状态")]
      [SerializeField] private bool isAttacking = false;
      [SerializeField] private bool canAttack = true;
      [SerializeField] private AttackData currentAttack;
      [SerializeField] private float attackCooldown = 0f;

      [Header("攻击设置")]
      [Tooltip("全局攻击冷却时间")]
      public float globalCooldown = 0.1f;
      [Tooltip("是否可以取消攻击")]
      public bool canCancelAttack = false;

      [Header("调试信息")]
      [SerializeField] private int totalAttacksPerformed = 0;
      [SerializeField] private int totalHitsLanded = 0;

      // 事件系统
      public System.Action<AttackData> OnAttackStart;
      public System.Action<AttackData> OnAttackEnd;
      public System.Action<AttackData> OnAttackCancel;
      public System.Action<GameObject, AttackData> OnHitTarget;

      // 攻击队列系统
      private Queue<AttackType> attackQueue = new Queue<AttackType>();
      private bool useAttackQueue = true;

      void Start()
      {
          InitializeComponents();
          SetupEventListeners();
      }

      void Update()
      {
          UpdateCooldowns();
          ProcessAttackQueue();
      }

      /// <summary>
      /// 初始化组件引用
      /// </summary>
      void InitializeComponents()
      {
          attackDetector = GetComponent<AttackDetector>();
          animator = GetComponent<Animator>();
          audioSource = GetComponent<AudioSource>();
          rb = GetComponent<Rigidbody2D>();

          if (attackDetector == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 AttackDetector 组件");
          }

          if (animator == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 Animator 组件");
          }
      }

      /// <summary>
      /// 设置事件监听
      /// </summary>
      void SetupEventListeners()
      {
          if (attackDetector != null)
          {
              attackDetector.OnHit += HandleHit;
          }
      }

      /// <summary>
      /// 更新冷却时间
      /// </summary>
      void UpdateCooldowns()
      {
          if (attackCooldown > 0)
          {
              attackCooldown -= Time.deltaTime;
              if (attackCooldown <= 0)
              {
                  canAttack = true;
              }
          }
      }

      /// <summary>
      /// 处理攻击队列
      /// </summary>
      void ProcessAttackQueue()
      {
          if (!useAttackQueue || attackQueue.Count == 0 || !canAttack || isAttacking)
              return;

          AttackType nextAttack = attackQueue.Dequeue();
          TryAttack(nextAttack);
      }

      /// <summary>
      /// 尝试执行攻击
      /// </summary>
      public bool TryAttack(AttackType attackType)
      {
          if (!CanPerformAttack()) return false;

          AttackData attackData = GetAttackData(attackType);
          if (attackData == null)
          {
              Debug.LogWarning($"找不到攻击类型 {attackType} 的数据");
              return false;
          }

          // 检查能量是否足够
          if (!HasSufficientEnergy(attackData)) return false;

          StartAttack(attackData);
          return true;
      }

      /// <summary>
      /// 添加攻击到队列
      /// </summary>
      public void QueueAttack(AttackType attackType)
      {
          if (useAttackQueue)
          {
              attackQueue.Enqueue(attackType);
          }
          else
          {
              TryAttack(attackType);
          }
      }

      /// <summary>
      /// 检查是否可以执行攻击
      /// </summary>
      bool CanPerformAttack()
      {
          if (!canAttack)
          {
              Debug.Log("当前无法攻击：冷却中");
              return false;
          }

          if (isAttacking && !canCancelAttack)
          {
              Debug.Log("当前无法攻击：正在攻击中");
              return false;
          }

          return true;
      }

      /// <summary>
      /// 获取攻击数据
      /// </summary>
      AttackData GetAttackData(AttackType attackType)
      {
          foreach (AttackData attack in availableAttacks)
          {
              if (attack.attackType == attackType)
                  return attack;
          }

          return defaultAttack;
      }

      /// <summary>
      /// 检查能量是否足够
      /// </summary>
      bool HasSufficientEnergy(AttackData attackData)
      {
          EnergySystem energySystem = GetComponent<EnergySystem>();
          if (energySystem == null) return true;
          
          // 如果是特殊技能，使用特殊检查
          if (attackData.attackType == AttackType.特殊技能)
          {
              if (!energySystem.CanUseSpecialSkill())
              {
                  Debug.Log($"特殊技能不可用：能量 {energySystem.CurrentEnergy}/{energySystem.SpecialSkillThreshold}");
                  return false;
              }
          }
          
          // 常规能量检查
          if (energySystem.CurrentEnergy < attackData.energyCost)
          {
              Debug.Log($"能量不足，需要 {attackData.energyCost}，当前 {energySystem.CurrentEnergy}");
              return false;
          }
          return true;
      }

      /// <summary>
      /// 开始攻击
      /// </summary>
      void StartAttack(AttackData attackData)
      {
          // 如果正在攻击且可以取消，先取消当前攻击
          if (isAttacking && canCancelAttack)
          {
              CancelCurrentAttack();
          }

          currentAttack = attackData;
          isAttacking = true;
          canAttack = false;
          totalAttacksPerformed++;

          // 播放动画
          PlayAttackAnimation(attackData);

          // 播放音效
          PlayAttackSound(attackData);

          // 消耗能量
          ConsumeEnergy(attackData);

          OnAttackStart?.Invoke(attackData);

          Debug.Log($"开始攻击：{attackData.attackName}");

          StartCoroutine(AttackSequence(attackData));
      }

      /// <summary>
      /// 攻击序列协程
      /// </summary>
      IEnumerator AttackSequence(AttackData attackData)
      {
          // 前摇时间
          yield return new WaitForSeconds(attackData.startupTime);

          // 开始攻击检测
          if (attackDetector != null)
          {
              attackDetector.StartDetection(attackData);
          }

          // 攻击持续时间
          yield return new WaitForSeconds(attackData.activeTime);

          // 停止检测
          if (attackDetector != null)
          {
              attackDetector.StopDetection();
          }

          // 后摇时间
          yield return new WaitForSeconds(attackData.recoveryTime);

          EndAttack();
      }

      /// <summary>
      /// 播放攻击动画
      /// </summary>
      void PlayAttackAnimation(AttackData attackData)
      {
          if (animator != null)
          {
              if (!string.IsNullOrEmpty(attackData.animationTrigger))
              {
                  animator.SetTrigger(attackData.animationTrigger);
              }
              else
              {
                  animator.SetTrigger("Attack");
              }

              animator.SetInteger("AttackType", (int)attackData.attackType);
          }
      }

      /// <summary>
      /// 播放攻击音效
      /// </summary>
      void PlayAttackSound(AttackData attackData)
      {
          if (audioSource != null && attackData.attackSound != null)
          {
              audioSource.PlayOneShot(attackData.attackSound);
          }
      }

      /// <summary>
      /// 消耗能量
      /// </summary>
      void ConsumeEnergy(AttackData attackData)
      {
          EnergySystem energySystem = GetComponent<EnergySystem>();
          if (energySystem != null)
          {
              // 如果是特殊技能，使用特殊技能消耗方法
              if (attackData.attackType == AttackType.特殊技能)
              {
                  energySystem.TryUseSpecialSkill(attackData.energyCost);
              }
              else
              {
                  energySystem.ConsumeEnergy(attackData.energyCost);
              }
          }
      }

      /// <summary>
      /// 结束攻击
      /// </summary>
      void EndAttack()
      {
          isAttacking = false;
          attackCooldown = globalCooldown;

          OnAttackEnd?.Invoke(currentAttack);

          Debug.Log($"攻击结束：{currentAttack.attackName}");

          currentAttack = null;
      }

      /// <summary>
      /// 取消当前攻击
      /// </summary>
      public void CancelCurrentAttack()
      {
          if (!isAttacking) return;

          StopAllCoroutines();

          if (attackDetector != null)
          {
              attackDetector.StopDetection();
          }

          OnAttackCancel?.Invoke(currentAttack);

          isAttacking = false;
          canAttack = true;

          Debug.Log($"取消攻击：{currentAttack.attackName}");

          currentAttack = null;
      }

      /// <summary>
      /// 处理击中目标
      /// </summary>
      void HandleHit(GameObject target, AttackData attackData)
      {
          totalHitsLanded++;

          // 创建伤害信息
          DamageInfo damageInfo = CreateDamageInfo(target, attackData);

          // 获取目标的健康系统和防御系统
          HealthSystem targetHealth = target.GetComponent<HealthSystem>();
          DefenseSystem targetDefense = target.GetComponent<DefenseSystem>();

          if (targetHealth != null)
          {
              // 检查是否被格挡
              if (targetDefense != null && targetDefense.IsBlocking())
              {
                  damageInfo.isBlocked = true;
                  targetDefense.HandleBlock(damageInfo);
              }
              else
              {
                  targetHealth.TakeDamage(damageInfo);
              }
          }

          // 获得能量
          GainEnergyFromHit(attackData);

          OnHitTarget?.Invoke(target, attackData);

          Debug.Log($"击中目标 {target.name}，造成伤害 {damageInfo.finalDamage}");
      }

      /// <summary>
      /// 创建伤害信息
      /// </summary>
      DamageInfo CreateDamageInfo(GameObject target, AttackData attackData)
      {
          DamageInfo damageInfo = new DamageInfo
          {
              damage = attackData.damage,
              attacker = gameObject,
              attackData = attackData,
              hitPosition = target.transform.position
          };

          damageInfo.CalculateFinalDamage();
          return damageInfo;
      }

      /// <summary>
      /// 从击中获得能量
      /// </summary>
      void GainEnergyFromHit(AttackData attackData)
      {
          EnergySystem energySystem = GetComponent<EnergySystem>();
          if (energySystem != null)
          {
              energySystem.GainEnergy(attackData.energyGain);
          }
      }

      /// <summary>
      /// 设置攻击队列开关
      /// </summary>
      public void SetUseAttackQueue(bool use)
      {
          useAttackQueue = use;
          if (!use)
          {
              attackQueue.Clear();
          }
      }

      /// <summary>
      /// 获取攻击统计信息
      /// </summary>
      public (int attacks, int hits) GetAttackStats()
      {
          return (totalAttacksPerformed, totalHitsLanded);
      }

      /// <summary>
      /// 重置攻击统计
      /// </summary>
      public void ResetAttackStats()
      {
          totalAttacksPerformed = 0;
          totalHitsLanded = 0;
      }

      // 属性访问器
      public bool IsAttacking => isAttacking;
      public bool CanAttack => canAttack;
      public AttackData CurrentAttack => currentAttack;

      void OnDestroy()
      {
          if (attackDetector != null)
          {
              attackDetector.OnHit -= HandleHit;
          }
      }
  }