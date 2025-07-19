  using UnityEngine;
  using System.Collections.Generic;

  public class ComboInputController : MonoBehaviour
  {
      [Header("连击输入设置")]
      [Tooltip("是否启用连击输入")]
      public bool enableComboInput = true;
      [Tooltip("连击输入敏感度")]
      [Range(0.1f, 2f)]
      public float inputSensitivity = 1f;

      [Header("特殊连击输入")]
      [Tooltip("方向键连击")]
      public bool enableDirectionalCombos = true;
      [Tooltip("同时按键连击")]
      public bool enableSimultaneousInput = true;
      [Tooltip("同时按键窗口时间")]
      public float simultaneousInputWindow = 0.1f;

      [Header("连击提示")]
      [Tooltip("显示连击提示")]
      public bool showComboHints = true;
      [Tooltip("连击提示UI")]
      public GameObject comboHintUI;

      private ComboSystem comboSystem;
      private AttackInputController attackInputController;
      private Dictionary<AttackType, KeyCode> attackKeys;
      private List<InputEvent> recentInputs = new List<InputEvent>();

      [System.Serializable]
      public struct InputEvent
      {
          public AttackType attackType;
          public Vector2 direction;
          public float timestamp;
          public bool isDirectional;
      }

      // 特殊连击输入模式
      public enum ComboInputMode
      {
          标准模式,
          格斗游戏模式,
          动作游戏模式
      }

      [Header("输入模式")]
      public ComboInputMode inputMode = ComboInputMode.标准模式;

      void Start()
      {
          InitializeComponents();
          InitializeInputKeys();
      }

      void Update()
      {
          if (!enableComboInput) return;

          HandleComboInput();
          CleanRecentInputs();
          UpdateComboHints();
      }

      /// <summary>
      /// 初始化组件
      /// </summary>
      void InitializeComponents()
      {
          comboSystem = GetComponent<ComboSystem>();
          attackInputController = GetComponent<AttackInputController>();

          if (comboSystem == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 ComboSystem 组件");
          }

          if (attackInputController == null)
          {
              Debug.LogWarning($"{gameObject.name} 缺少 AttackInputController 组件");
          }
      }

      /// <summary>
      /// 初始化攻击按键映射
      /// </summary>
      void InitializeInputKeys()
      {
          attackKeys = new Dictionary<AttackType, KeyCode>();

          if (attackInputController != null)
          {
              attackKeys[AttackType.轻拳] = attackInputController.lightPunchKey;
              attackKeys[AttackType.重拳] = attackInputController.heavyPunchKey;
              attackKeys[AttackType.轻腿] = attackInputController.lightKickKey;
              attackKeys[AttackType.重腿] = attackInputController.heavyKickKey;
              attackKeys[AttackType.特殊技能] = attackInputController.specialKey;
          }
      }

      /// <summary>
      /// 处理连击输入
      /// </summary>
      void HandleComboInput()
      {
          switch (inputMode)
          {
              case ComboInputMode.标准模式:
                  HandleStandardInput();
                  break;
              case ComboInputMode.格斗游戏模式:
                  HandleFightingGameInput();
                  break;
              case ComboInputMode.动作游戏模式:
                  HandleActionGameInput();
                  break;
          }
      }

      /// <summary>
      /// 处理标准输入模式
      /// </summary>
      void HandleStandardInput()
      {
          foreach (var keyPair in attackKeys)
          {
              if (Input.GetKeyDown(keyPair.Value))
              {
                  ProcessAttackInput(keyPair.Key);
              }
          }
      }

      /// <summary>
      /// 处理格斗游戏输入模式
      /// </summary>
      void HandleFightingGameInput()
      {
          // 检查方向输入
          Vector2 direction = GetDirectionInput();

          // 检查攻击输入
          foreach (var keyPair in attackKeys)
          {
              if (Input.GetKeyDown(keyPair.Value))
              {
                  ProcessDirectionalAttack(keyPair.Key, direction);
              }
          }

          // 检查特殊输入组合
          CheckSpecialInputCombinations();
      }

      /// <summary>
      /// 处理动作游戏输入模式
      /// </summary>
      void HandleActionGameInput()
      {
          // 检查同时按键
          if (enableSimultaneousInput)
          {
              CheckSimultaneousInput();
          }

          // 标准输入处理
          HandleStandardInput();
      }

      /// <summary>
      /// 处理攻击输入
      /// </summary>
      void ProcessAttackInput(AttackType attackType)
      {
          if (comboSystem == null) return;

          // 记录输入事件
          RecordInputEvent(attackType, Vector2.zero, false);

          // 发送到连击系统
          comboSystem.AddInputToBuffer(attackType);

          Debug.Log($"连击输入：{attackType}");
      }

      /// <summary>
      /// 处理方向攻击
      /// </summary>
      void ProcessDirectionalAttack(AttackType attackType, Vector2 direction)
      {
          if (comboSystem == null) return;

          // 记录方向输入事件
          RecordInputEvent(attackType, direction, true);

          // 根据方向修改攻击类型
          AttackType modifiedAttack = ModifyAttackWithDirection(attackType, direction);

          // 发送到连击系统
          comboSystem.AddInputToBuffer(modifiedAttack);

          Debug.Log($"方向连击输入：{attackType} + {direction} = {modifiedAttack}");
      }

      /// <summary>
      /// 根据方向修改攻击类型
      /// </summary>
      AttackType ModifyAttackWithDirection(AttackType baseAttack, Vector2 direction)
      {
          // 这里可以根据方向输入修改攻击类型
          // 例如：向下+轻拳 = 下段攻击

          if (direction.y < -0.5f) // 向下
          {
              // 可以返回特殊的下段攻击类型
              return baseAttack;
          }
          else if (direction.y > 0.5f) // 向上
          {
              // 可以返回特殊的上段攻击类型
              return baseAttack;
          }

          return baseAttack;
      }

      /// <summary>
      /// 获取方向输入
      /// </summary>
      Vector2 GetDirectionInput()
      {
          float horizontal = Input.GetAxisRaw("Horizontal");
          float vertical = Input.GetAxisRaw("Vertical");

          return new Vector2(horizontal, vertical);
      }

      /// <summary>
      /// 检查特殊输入组合
      /// </summary>
      void CheckSpecialInputCombinations()
      {
          // 检查常见的格斗游戏输入模式
          // 例如：↓↘→ + 拳 = 波动拳

          if (recentInputs.Count >= 3)
          {
              // 检查波动拳输入 (↓↘→)
              if (CheckHadokenInput())
              {
                  ProcessSpecialCombo("波动拳");
              }

              // 检查升龙拳输入 (→↓↘)
              if (CheckShoryukenInput())
              {
                  ProcessSpecialCombo("升龙拳");
              }
          }
      }

      /// <summary>
      /// 检查波动拳输入
      /// </summary>
      bool CheckHadokenInput()
      {
          // 简化的波动拳检测逻辑
          return false; // 实际实现需要检查具体的方向序列
      }

      /// <summary>
      /// 检查升龙拳输入
      /// </summary>
      bool CheckShoryukenInput()
      {
          // 简化的升龙拳检测逻辑
          return false; // 实际实现需要检查具体的方向序列
      }

      /// <summary>
      /// 处理特殊连击
      /// </summary>
      void ProcessSpecialCombo(string comboName)
      {
          Debug.Log($"检测到特殊连击：{comboName}");

          // 可以触发特殊的连击序列
          if (comboSystem != null)
          {
              comboSystem.AddInputToBuffer(AttackType.特殊技能);
          }
      }

      /// <summary>
      /// 检查同时按键输入
      /// </summary>
      void CheckSimultaneousInput()
      {
          List<AttackType> simultaneousInputs = new List<AttackType>();

          foreach (var keyPair in attackKeys)
          {
              if (Input.GetKeyDown(keyPair.Value))
              {
                  simultaneousInputs.Add(keyPair.Key);
              }
          }

          if (simultaneousInputs.Count > 1)
          {
              ProcessSimultaneousInput(simultaneousInputs);
          }
      }

      /// <summary>
      /// 处理同时按键输入
      /// </summary>
      void ProcessSimultaneousInput(List<AttackType> inputs)
      {
          Debug.Log($"同时按键输入：{string.Join(", ", inputs)}");

          // 可以根据组合创建特殊攻击
          if (inputs.Contains(AttackType.轻拳) && inputs.Contains(AttackType.轻腿))
          {
              // 投技
              ProcessSpecialCombo("投技");
          }
          else if (inputs.Contains(AttackType.重拳) && inputs.Contains(AttackType.重腿))
          {
              // 超必杀技
              ProcessSpecialCombo("超必杀技");
          }
      }

      /// <summary>
      /// 记录输入事件
      /// </summary>
      void RecordInputEvent(AttackType attackType, Vector2 direction, bool isDirectional)
      {
          InputEvent inputEvent = new InputEvent
          {
              attackType = attackType,
              direction = direction,
              timestamp = Time.time,
              isDirectional = isDirectional
          };

          recentInputs.Add(inputEvent);
      }

      /// <summary>
      /// 清理过期输入
      /// </summary>
      void CleanRecentInputs()
      {
          float currentTime = Time.time;
          recentInputs.RemoveAll(input => currentTime - input.timestamp > 2f);
      }

      /// <summary>
      /// 更新连击提示
      /// </summary>
      void UpdateComboHints()
      {
          if (!showComboHints || comboHintUI == null) return;

          // 根据当前连击状态显示提示
          bool shouldShowHint = comboSystem != null && comboSystem.IsInCombo();

          if (comboHintUI.activeSelf != shouldShowHint)
          {
              comboHintUI.SetActive(shouldShowHint);
          }
      }

      /// <summary>
      /// 设置输入模式
      /// </summary>
      public void SetInputMode(ComboInputMode mode)
      {
          inputMode = mode;
          Debug.Log($"连击输入模式设置为：{mode}");
      }

      /// <summary>
      /// 启用/禁用连击输入
      /// </summary>
      public void SetComboInputEnabled(bool enabled)
      {
          enableComboInput = enabled;

          if (!enabled)
          {
              recentInputs.Clear();
          }
      }

      /// <summary>
      /// 获取最近的输入历史
      /// </summary>
      public List<InputEvent> GetRecentInputs()
      {
          return new List<InputEvent>(recentInputs);
      }

      /// <summary>
      /// 清空输入历史
      /// </summary>
      public void ClearInputHistory()
      {
          recentInputs.Clear();
      }
  }