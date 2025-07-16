 using UnityEngine;

  public class DefenseInputController : MonoBehaviour
  {
      [Header("防御按键设置")]
      [Tooltip("格挡按键")]
      public KeyCode blockKey = KeyCode.K;
      [Tooltip("闪避按键")]
      public KeyCode dodgeKey = KeyCode.L;
      [Tooltip("反击按键")]
      public KeyCode counterKey = KeyCode.Semicolon;

      [Header("输入设置")]
      [Tooltip("是否启用长按格挡")]
      public bool enableHoldToBlock = true;
      [Tooltip("反击输入窗口时间")]
      public float counterInputWindow = 0.3f;

      private DefenseSystem defenseSystem;
      private AttackSystem attackSystem;

      void Start()
      {
          defenseSystem = GetComponent<DefenseSystem>();
          attackSystem = GetComponent<AttackSystem>();

          if (defenseSystem == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 DefenseSystem 组件");
          }
      }

      void Update()
      {
          HandleBlockInput();
          HandleDodgeInput();
          HandleCounterInput();
      }

      /// <summary>
      /// 处理格挡输入
      /// </summary>
      void HandleBlockInput()
      {
          if (defenseSystem == null) return;

          if (enableHoldToBlock)
          {
              // 长按格挡模式
              if (Input.GetKey(blockKey))
              {
                  defenseSystem.TryStartBlock();
              }
              else
              {
                  defenseSystem.StopBlock();
              }
          }
          else
          {
              // 点击切换模式
              if (Input.GetKeyDown(blockKey))
              {
                  if (defenseSystem.IsBlocking())
                  {
                      defenseSystem.StopBlock();
                  }
                  else
                  {
                      defenseSystem.TryStartBlock();
                  }
              }
          }
      }

      /// <summary>
      /// 处理闪避输入
      /// </summary>
      void HandleDodgeInput()
      {
          if (defenseSystem == null) return;

          if (Input.GetKeyDown(dodgeKey))
          {
              defenseSystem.TryStartDodge();
          }
      }

      /// <summary>
      /// 处理反击输入
      /// </summary>
      void HandleCounterInput()
      {
          if (defenseSystem == null || !defenseSystem.CanCounterAttack()) return;

          if (Input.GetKeyDown(counterKey))
          {
              // 执行反击（这里使用轻拳作为默认反击）
              defenseSystem.TryCounterAttack(AttackType.轻拳);
          }
      }

      /// <summary>
      /// 设置按键绑定
      /// </summary>
      public void SetKeyBinding(string action, KeyCode keyCode)
      {
          switch (action.ToLower())
          {
              case "block":
              case "格挡":
                  blockKey = keyCode;
                  break;
              case "dodge":
              case "闪避":
                  dodgeKey = keyCode;
                  break;
              case "counter":
              case "反击":
                  counterKey = keyCode;
                  break;
          }
      }
  }