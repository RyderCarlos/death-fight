  using UnityEngine;
  using System.Collections.Generic;

  public class AttackInputController : MonoBehaviour
  {
      [Header("按键设置")]
      [Tooltip("轻拳按键")]
      public KeyCode lightPunchKey = KeyCode.J;
      [Tooltip("重拳按键")]
      public KeyCode heavyPunchKey = KeyCode.U;
      [Tooltip("轻腿按键")]
      public KeyCode lightKickKey = KeyCode.I;
      [Tooltip("重腿按键")]
      public KeyCode heavyKickKey = KeyCode.O;
      [Tooltip("特殊技能按键")]
      public KeyCode specialKey = KeyCode.P;

      [Header("输入设置")]
      [Tooltip("是否启用输入缓冲")]
      public bool enableInputBuffer = true;
      [Tooltip("输入缓冲时间（秒）")]
      public float inputBufferTime = 0.2f;

      private AttackSystem attackSystem;
      private Dictionary<KeyCode, AttackType> keyBindings;
      private List<BufferedInput> inputBuffer = new List<BufferedInput>();

      [System.Serializable]
      public struct BufferedInput
      {
          public AttackType attackType;
          public float inputTime;
          public bool processed;
      }

      void Start()
      {
          attackSystem = GetComponent<AttackSystem>();
          InitializeKeyBindings();
      }

      void Update()
      {
          HandleAttackInput();
          ProcessInputBuffer();
          CleanInputBuffer();
      }

      /// <summary>
      /// 初始化按键绑定
      /// </summary>
      void InitializeKeyBindings()
      {
          keyBindings = new Dictionary<KeyCode, AttackType>
          {
              { lightPunchKey, AttackType.轻拳 },
              { heavyPunchKey, AttackType.重拳 },
              { lightKickKey, AttackType.轻腿 },
              { heavyKickKey, AttackType.重腿 },
              { specialKey, AttackType.特殊技能 }
          };
      }

      /// <summary>
      /// 处理攻击输入
      /// </summary>
      void HandleAttackInput()
      {
          foreach (var binding in keyBindings)
          {
              if (Input.GetKeyDown(binding.Key))
              {
                  ProcessAttackInput(binding.Value);
              }
          }
      }

      /// <summary>
      /// 处理攻击输入
      /// </summary>
      void ProcessAttackInput(AttackType attackType)
      {
          if (enableInputBuffer)
          {
              AddToInputBuffer(attackType);
          }
          else
          {
              ExecuteAttack(attackType);
          }
      }

      /// <summary>
      /// 添加输入到缓冲区
      /// </summary>
      void AddToInputBuffer(AttackType attackType)
      {
          BufferedInput input = new BufferedInput
          {
              attackType = attackType,
              inputTime = Time.time,
              processed = false
          };

          inputBuffer.Add(input);
      }

      /// <summary>
      /// 处理输入缓冲
      /// </summary>
      void ProcessInputBuffer()
      {
          for (int i = 0; i < inputBuffer.Count; i++)
          {
              var input = inputBuffer[i];

              if (!input.processed && attackSystem != null && attackSystem.CanAttack)
              {
                  if (ExecuteAttack(input.attackType))
                  {
                      input.processed = true;
                      inputBuffer[i] = input;
                      break; // 一次只处理一个输入
                  }
              }
          }
      }

      /// <summary>
      /// 执行攻击
      /// </summary>
      bool ExecuteAttack(AttackType attackType)
      {
          if (attackSystem != null)
          {
              return attackSystem.TryAttack(attackType);
          }
          return false;
      }

      /// <summary>
      /// 清理过期的输入缓冲
      /// </summary>
      void CleanInputBuffer()
      {
          inputBuffer.RemoveAll(input =>
              input.processed || Time.time - input.inputTime > inputBufferTime);
      }

      /// <summary>
      /// 设置按键绑定
      /// </summary>
      public void SetKeyBinding(AttackType attackType, KeyCode keyCode)
      {
          // 移除旧的绑定
          var keysToRemove = new List<KeyCode>();
          foreach (var binding in keyBindings)
          {
              if (binding.Value == attackType)
              {
                  keysToRemove.Add(binding.Key);
              }
          }

          foreach (var key in keysToRemove)
          {
              keyBindings.Remove(key);
          }

          // 添加新的绑定
          keyBindings[keyCode] = attackType;
      }

      /// <summary>
      /// 获取按键绑定
      /// </summary>
      public KeyCode GetKeyBinding(AttackType attackType)
      {
          foreach (var binding in keyBindings)
          {
              if (binding.Value == attackType)
                  return binding.Key;
          }
          return KeyCode.None;
      }

      /// <summary>
      /// 清空输入缓冲
      /// </summary>
      public void ClearInputBuffer()
      {
          inputBuffer.Clear();
      }
  }