  using UnityEngine;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;

  public class ComboSystem : MonoBehaviour
  {
      [Header("连击设置")]
      [Tooltip("连击重置时间")]
      public float comboResetTime = 2f;
      [Tooltip("输入缓冲时间")]
      public float inputBufferTime = 0.3f;
      [Tooltip("连击伤害递增率")]
      public float damageIncreaseRate = 0.1f;
      [Tooltip("最大连击倍数")]
      public float maxComboMultiplier = 3f;

      [Header("连击数据")]
      [Tooltip("预设连击序列")]
      public ComboData[] comboSequences;
      [Tooltip("当前连击数据")]
      [SerializeField] private ComboData currentComboData;

      [Header("连击状态")]
      [SerializeField] private int currentComboCount = 0;
      [SerializeField] private bool inCombo = false;
      [SerializeField] private float comboMultiplier = 1f;
      [SerializeField] private float comboTimer = 0f;
      [SerializeField] private int consecutiveHits = 0;
      [SerializeField] private int missedInputs = 0;

      [Header("输入缓冲")]
      private Queue<ComboInput> inputBuffer = new Queue<ComboInput>();
      private List<ComboInput> comboInputHistory = new List<ComboInput>();

      [Header("连击检测")]
      [SerializeField] private int currentComboStep = 0;
      [SerializeField] private float stepTimer = 0f;
      [SerializeField] private bool waitingForNextInput = false;

      [Header("组件引用")]
      private AttackSystem attackSystem;
      private AudioSource audioSource;

      // 协程引用
      private Coroutine comboResetCoroutine;
      private Coroutine comboTimeoutCoroutine;

      // 连击输入结构
      [System.Serializable]
      public struct ComboInput
      {
          public AttackType attackType;
          public float inputTime;
          public bool wasHit;
          public GameObject target;
      }

      // 事件系统
      public System.Action<int> OnComboStart;
      public System.Action<int, float> OnComboExtend;
      public System.Action<int, ComboData> OnComboComplete;
      public System.Action<int> OnComboReset;
      public System.Action<ComboData, int> OnComboSequenceComplete;
      public System.Action<AttackType, bool> OnComboInputProcessed;

      [Header("调试信息")]
      [SerializeField] private int totalCombosCompleted = 0;
      [SerializeField] private int longestCombo = 0;
      [SerializeField] private ComboData lastCompletedCombo;

      void Start()
      {
          InitializeComponents();
          InitializeComboSystem();
      }

      void Update()
      {
          UpdateComboTimer();
          ProcessInputBuffer();
          CheckComboTimeout();
      }

      /// <summary>
      /// 初始化组件
      /// </summary>
      void InitializeComponents()
      {
          attackSystem = GetComponent<AttackSystem>();
          audioSource = GetComponent<AudioSource>();

          if (attackSystem != null)
          {
              attackSystem.OnAttackStart += OnAttackStarted;
              attackSystem.OnHitTarget += OnTargetHit;
          }
      }

      /// <summary>
      /// 初始化连击系统
      /// </summary>
      void InitializeComboSystem()
      {
          ResetCombo();

          // 验证连击数据
          ValidateComboData();

          Debug.Log($"连击系统初始化完成，加载了 {comboSequences.Length} 个连击序列");
      }

      /// <summary>
      /// 验证连击数据
      /// </summary>
      void ValidateComboData()
      {
          foreach (var combo in comboSequences)
          {
              if (combo == null) continue;

              if (combo.attackSequence == null || combo.attackSequence.Length == 0)
              {
                  Debug.LogWarning($"连击 {combo.comboName} 没有设置攻击序列");
              }
          }
      }

      /// <summary>
      /// 更新连击计时器
      /// </summary>
      void UpdateComboTimer()
      {
          if (inCombo)
          {
              comboTimer += Time.deltaTime;

              if (waitingForNextInput)
              {
                  stepTimer += Time.deltaTime;
              }
          }
      }

      /// <summary>
      /// 处理输入缓冲
      /// </summary>
      void ProcessInputBuffer()
      {
          CleanInputBuffer();

          while (inputBuffer.Count > 0)
          {
              ComboInput input = inputBuffer.Dequeue();
              ProcessComboInput(input);
          }
      }

      /// <summary>
      /// 清理过期输入
      /// </summary>
      void CleanInputBuffer()
      {
          var tempList = new List<ComboInput>();

          while (inputBuffer.Count > 0)
          {
              ComboInput input = inputBuffer.Dequeue();
              if (Time.time - input.inputTime <= inputBufferTime)
              {
                  tempList.Add(input);
              }
          }

          foreach (var input in tempList)
          {
              inputBuffer.Enqueue(input);
          }
      }

      /// <summary>
      /// 检查连击超时
      /// </summary>
      void CheckComboTimeout()
      {
          if (currentComboData != null && inCombo)
          {
              if (comboTimer >= currentComboData.comboTimeLimit)
              {
                  Debug.Log("连击超时");
                  ResetCombo();
              }
          }
      }

      /// <summary>
      /// 添加输入到缓冲区
      /// </summary>
      public void AddInputToBuffer(AttackType attackType)
      {
          ComboInput input = new ComboInput
          {
              attackType = attackType,
              inputTime = Time.time,
              wasHit = false,
              target = null
          };

          inputBuffer.Enqueue(input);

          Debug.Log($"添加输入到缓冲区：{attackType} 在 {Time.time}");
      }

      /// <summary>
      /// 处理连击输入
      /// </summary>
      void ProcessComboInput(ComboInput input)
      {
          if (!inCombo)
          {
              StartCombo(input);
          }
          else
          {
              ProcessComboStep(input);
          }

          // 添加到历史记录
          comboInputHistory.Add(input);

          // 检查连击序列
          CheckComboSequences(input);
      }

      /// <summary>
      /// 开始连击
      /// </summary>
      void StartCombo(ComboInput input)
      {
          inCombo = true;
          currentComboCount = 1;
          comboMultiplier = 1f;
          comboTimer = 0f;
          consecutiveHits = 0;
          missedInputs = 0;
          currentComboStep = 0;

          // 寻找匹配的连击序列
          FindMatchingCombo(input);

          OnComboStart?.Invoke(currentComboCount);

          Debug.Log($"开始连击：{input.attackType}");

          ResetComboTimer();
      }

      /// <summary>
      /// 寻找匹配的连击序列
      /// </summary>
      void FindMatchingCombo(ComboInput input)
      {
          currentComboData = null;

          foreach (var combo in comboSequences)
          {
              if (combo != null && combo.attackSequence != null && combo.attackSequence.Length > 0)
              {
                  if (combo.attackSequence[0] == input.attackType)
                  {
                      currentComboData = combo;
                      currentComboStep = 0;
                      waitingForNextInput = true;
                      stepTimer = 0f;

                      Debug.Log($"找到匹配的连击序列：{combo.comboName}");
                      break;
                  }
              }
          }
      }

      /// <summary>
      /// 处理连击步骤
      /// </summary>
      void ProcessComboStep(ComboInput input)
      {
          ExtendCombo();

          // 如果有当前连击数据，检查是否匹配
          if (currentComboData != null)
          {
              CheckComboSequenceStep(input);
          }

          OnComboInputProcessed?.Invoke(input.attackType, input.wasHit);
      }

      /// <summary>
      /// 检查连击序列步骤
      /// </summary>
      void CheckComboSequenceStep(ComboInput input)
      {
          if (currentComboStep + 1 < currentComboData.attackSequence.Length)
          {
              AttackType expectedAttack = currentComboData.attackSequence[currentComboStep + 1];
              float timingWindow = currentComboData.GetTimingWindow(currentComboStep);

              if (input.attackType == expectedAttack && stepTimer <= timingWindow)
              {
                  // 正确的输入
                  currentComboStep++;
                  stepTimer = 0f;

                  Debug.Log($"连击步骤 {currentComboStep + 1}/{currentComboData.attackSequence.Length} 成功");

                  // 检查是否完成了整个序列
                  if (currentComboStep >= currentComboData.attackSequence.Length - 1)
                  {
                      CompleteComboSequence();
                  }
              }
              else
              {
                  // 错误的输入
                  HandleComboMistake(input);
              }
          }
      }

      /// <summary>
      /// 处理连击错误
      /// </summary>
      void HandleComboMistake(ComboInput input)
      {
          missedInputs++;

          if (currentComboData != null && missedInputs > currentComboData.allowedMisses)
          {
              Debug.Log($"连击失败：错误输入 {input.attackType}，期望 {currentComboData.attackSequence[currentComboStep + 1]}");

              // 重置连击序列，但保持普通连击
              currentComboData = null;
              currentComboStep = 0;
              waitingForNextInput = false;
          }
          else
          {
              Debug.Log($"连击错误，但在允许范围内：{missedInputs}/{currentComboData.allowedMisses}");
          }
      }

      /// <summary>
      /// 完成连击序列
      /// </summary>
      void CompleteComboSequence()
      {
          if (currentComboData == null) return;

          totalCombosCompleted++;
          lastCompletedCombo = currentComboData;

          // 应用连击效果
          ApplyComboEffects();

          OnComboSequenceComplete?.Invoke(currentComboData, currentComboCount);

          Debug.Log($"完成连击序列：{currentComboData.comboName}");

          // 播放完成特效
          PlayComboFinisher();

          // 重置连击序列状态
          currentComboData = null;
          currentComboStep = 0;
          waitingForNextInput = false;
      }

      /// <summary>
      /// 应用连击效果
      /// </summary>
      void ApplyComboEffects()
      {
          if (currentComboData == null) return;

          // 获得额外能量
          EnergySystem energySystem = GetComponent<EnergySystem>();
          if (energySystem != null)
          {
              energySystem.GainEnergy(currentComboData.energyBonus);
          }

          // 应用额外伤害（在下次攻击时生效）
          ApplyFinisherDamage();
      }

      /// <summary>
      /// 应用终结技伤害
      /// </summary>
      void ApplyFinisherDamage()
      {
          // 这里可以设置一个标记，在下次攻击时应用额外伤害
          // 或者直接对最后击中的目标造成伤害

          Debug.Log($"应用终结技伤害：{currentComboData.finisherDamage}");
      }

      /// <summary>
      /// 播放连击终结特效
      /// </summary>
      void PlayComboFinisher()
      {
          if (currentComboData == null) return;

          // 播放终结特效
          if (currentComboData.finisherEffect != null)
          {
              GameObject effect = Instantiate(currentComboData.finisherEffect, transform.position, Quaternion.identity);
              Destroy(effect, 3f);
          }

          // 播放终结音效
          if (audioSource != null && currentComboData.finisherSound != null)
          {
              audioSource.PlayOneShot(currentComboData.finisherSound);
          }
      }

      /// <summary>
      /// 扩展连击
      /// </summary>
      void ExtendCombo()
      {
          currentComboCount++;

          if (currentComboCount > longestCombo)
          {
              longestCombo = currentComboCount;
          }

          // 计算连击伤害倍数
          float multiplierIncrease = damageIncreaseRate * (currentComboCount - 1);
          comboMultiplier = Mathf.Min(1f + multiplierIncrease, maxComboMultiplier);

          OnComboExtend?.Invoke(currentComboCount, comboMultiplier);

          Debug.Log($"连击扩展：{currentComboCount} 倍数：{comboMultiplier:F2}");

          ResetComboTimer();
      }

      /// <summary>
      /// 检查连击序列
      /// </summary>
      void CheckComboSequences(ComboInput input)
      {
          foreach (var combo in comboSequences)
          {
              if (combo != null && IsComboSequenceMatched(combo))
              {
                  if (combo != currentComboData) // 防止重复触发
                  {
                      CompleteComboSequence();
                  }
                  break;
              }
          }
      }

      /// <summary>
      /// 检查连击序列是否匹配
      /// </summary>
      bool IsComboSequenceMatched(ComboData combo)
      {
          if (combo.attackSequence == null || combo.attackSequence.Length == 0)
              return false;

          if (comboInputHistory.Count < combo.attackSequence.Length)
              return false;

          // 检查最近的输入是否匹配连击序列
          int startIndex = comboInputHistory.Count - combo.attackSequence.Length;

          for (int i = 0; i < combo.attackSequence.Length; i++)
          {
              if (comboInputHistory[startIndex + i].attackType != combo.attackSequence[i])
              {
                  return false;
              }

              // 检查时间窗口
              if (i > 0)
              {
                  float timeDiff = comboInputHistory[startIndex + i].inputTime -
                                  comboInputHistory[startIndex + i - 1].inputTime;
                  float allowedWindow = combo.GetTimingWindow(i - 1);

                  if (timeDiff > allowedWindow)
                  {
                      return false;
                  }
              }
          }

          return true;
      }

      /// <summary>
      /// 重置连击计时器
      /// </summary>
      void ResetComboTimer()
      {
          if (comboResetCoroutine != null)
          {
              StopCoroutine(comboResetCoroutine);
          }
          comboResetCoroutine = StartCoroutine(ComboResetCoroutine());
      }

      /// <summary>
      /// 连击重置协程
      /// </summary>
      IEnumerator ComboResetCoroutine()
      {
          yield return new WaitForSeconds(comboResetTime);
          ResetCombo();
      }

      /// <summary>
      /// 重置连击
      /// </summary>
      void ResetCombo()
      {
          int oldComboCount = currentComboCount;

          inCombo = false;
          currentComboCount = 0;
          comboMultiplier = 1f;
          comboTimer = 0f;
          consecutiveHits = 0;
          missedInputs = 0;
          currentComboStep = 0;
          stepTimer = 0f;
          waitingForNextInput = false;

          currentComboData = null;
          inputBuffer.Clear();
          comboInputHistory.Clear();

          if (oldComboCount > 0)
          {
              OnComboReset?.Invoke(oldComboCount);
              Debug.Log($"连击重置，最终连击数：{oldComboCount}");
          }
      }

      /// <summary>
      /// 攻击开始事件处理
      /// </summary>
      void OnAttackStarted(AttackData attackData)
      {
          // 攻击开始时的处理
          Debug.Log($"攻击开始：{attackData.attackType}");
      }

      /// <summary>
      /// 击中目标事件处理
      /// </summary>
      void OnTargetHit(GameObject target, AttackData attackData)
      {
          consecutiveHits++;

          // 更新输入历史中的命中信息
          if (comboInputHistory.Count > 0)
          {
              var lastInput = comboInputHistory[comboInputHistory.Count - 1];
              lastInput.wasHit = true;
              lastInput.target = target;
              comboInputHistory[comboInputHistory.Count - 1] = lastInput;
          }

          Debug.Log($"击中目标：{target.name}，连续命中：{consecutiveHits}");
      }

      /// <summary>
      /// 强制完成连击
      /// </summary>
      public void ForceCompleteCombo()
      {
          if (inCombo)
          {
              OnComboComplete?.Invoke(currentComboCount, currentComboData);
              ResetCombo();
          }
      }

      /// <summary>
      /// 获取连击倍数
      /// </summary>
      public float GetComboMultiplier()
      {
          return comboMultiplier;
      }

      /// <summary>
      /// 获取连击统计
      /// </summary>
      public ComboStats GetComboStats()
      {
          return new ComboStats
          {
              currentComboCount = currentComboCount,
              longestCombo = longestCombo,
              totalCombosCompleted = totalCombosCompleted,
              currentMultiplier = comboMultiplier,
              consecutiveHits = consecutiveHits,
              isInCombo = inCombo
          };
      }

      /// <summary>
      /// 重置连击统计
      /// </summary>
      public void ResetComboStats()
      {
          longestCombo = 0;
          totalCombosCompleted = 0;
          lastCompletedCombo = null;
      }

      // 属性访问器
      public int CurrentComboCount => currentComboCount;
      public bool IsInCombo => inCombo;
      public float ComboMultiplier => comboMultiplier;
      public ComboData CurrentComboData => currentComboData;

      void OnDestroy()
      {
          if (attackSystem != null)
          {
              attackSystem.OnAttackStart -= OnAttackStarted;
              attackSystem.OnHitTarget -= OnTargetHit;
          }
      }
  }

  /// <summary>
  /// 连击统计信息
  /// </summary>
  [System.Serializable]
  public struct ComboStats
  {
      public int currentComboCount;
      public int longestCombo;
      public int totalCombosCompleted;
      public float currentMultiplier;
      public int consecutiveHits;
      public bool isInCombo;
  }