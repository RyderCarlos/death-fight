using UnityEngine;

public class SpecialSkillController : MonoBehaviour
{
    [Header("特殊技能设置")]
    [Tooltip("特殊技能攻击数据")]
    public AttackData specialSkillData;
    [Tooltip("特殊技能输入键")]
    public KeyCode specialSkillKey = KeyCode.E;
    [Tooltip("组合键输入（例如 Shift+E）")]
    public KeyCode modifierKey = KeyCode.LeftShift;
    [Tooltip("是否需要组合键")]
    public bool requireModifier = false;
    [Tooltip("特殊技能冷却时间")]
    public float cooldownTime = 3f;
    
    [Header("输入缓冲")]
    [Tooltip("输入缓冲时间")]
    public float inputBufferTime = 0.2f;
    
    // 组件引用
    private AttackSystem attackSystem;
    private EnergySystem energySystem;
    private ComboSystem comboSystem;
    
    // 状态变量
    private float lastInputTime;
    private float cooldownTimer;
    private bool isOnCooldown;
    private bool hasBufferedInput;
    
    // 事件
    public System.Action OnSpecialSkillUsed;
    public System.Action OnSpecialSkillFailed;
    public System.Action<float> OnCooldownChanged;
    
    void Start()
    {
        InitializeComponents();
        ValidateSetup();
    }
    
    void Update()
    {
        HandleInput();
        UpdateCooldown();
        ProcessBufferedInput();
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    void InitializeComponents()
    {
        attackSystem = GetComponent<AttackSystem>();
        energySystem = GetComponent<EnergySystem>();
        comboSystem = GetComponent<ComboSystem>();
        
        if (attackSystem == null)
        {
            Debug.LogError("SpecialSkillController: 缺少 AttackSystem 组件");
        }
        
        if (energySystem == null)
        {
            Debug.LogError("SpecialSkillController: 缺少 EnergySystem 组件");
        }
    }
    
    /// <summary>
    /// 验证设置
    /// </summary>
    void ValidateSetup()
    {
        if (specialSkillData == null)
        {
            Debug.LogWarning("SpecialSkillController: 未设置特殊技能数据");
        }
        else if (specialSkillData.attackType != AttackType.特殊技能)
        {
            Debug.LogWarning($"SpecialSkillController: 攻击数据 {specialSkillData.name} 的类型不是特殊技能");
        }
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        bool specialKeyPressed = Input.GetKeyDown(specialSkillKey);
        bool modifierPressed = !requireModifier || Input.GetKey(modifierKey);
        
        if (specialKeyPressed && modifierPressed)
        {
            TryUseSpecialSkill();
        }
    }
    
    /// <summary>
    /// 尝试使用特殊技能
    /// </summary>
    public bool TryUseSpecialSkill()
    {
        // 检查冷却时间
        if (isOnCooldown)
        {
            Debug.Log($"特殊技能冷却中，剩余时间：{cooldownTimer:F1}秒");
            BufferInput();
            OnSpecialSkillFailed?.Invoke();
            return false;
        }
        
        // 检查能量是否足够
        if (energySystem != null && !energySystem.CanUseSpecialSkill())
        {
            Debug.Log("能量不足，无法使用特殊技能");
            OnSpecialSkillFailed?.Invoke();
            return false;
        }
        
        // 检查是否有攻击数据
        if (specialSkillData == null)
        {
            Debug.LogError("未设置特殊技能数据");
            OnSpecialSkillFailed?.Invoke();
            return false;
        }
        
        // 尝试执行攻击
        if (attackSystem != null)
        {
            bool success = attackSystem.TryAttack(AttackType.特殊技能);
            if (success)
            {
                StartCooldown();
                
                // 添加到连击系统
                if (comboSystem != null)
                {
                    comboSystem.AddInputToBuffer(AttackType.特殊技能);
                }
                
                OnSpecialSkillUsed?.Invoke();
                Debug.Log($"使用特殊技能：{specialSkillData.attackName}");
                return true;
            }
            else
            {
                OnSpecialSkillFailed?.Invoke();
                return false;
            }
        }
        
        OnSpecialSkillFailed?.Invoke();
        return false;
    }
    
    /// <summary>
    /// 缓冲输入
    /// </summary>
    void BufferInput()
    {
        lastInputTime = Time.time;
        hasBufferedInput = true;
    }
    
    /// <summary>
    /// 处理缓冲的输入
    /// </summary>
    void ProcessBufferedInput()
    {
        if (!hasBufferedInput) return;
        
        // 检查缓冲是否过期
        if (Time.time - lastInputTime > inputBufferTime)
        {
            hasBufferedInput = false;
            return;
        }
        
        // 如果冷却结束，尝试执行缓冲的技能
        if (!isOnCooldown)
        {
            hasBufferedInput = false;
            TryUseSpecialSkill();
        }
    }
    
    /// <summary>
    /// 开始冷却
    /// </summary>
    void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
    }
    
    /// <summary>
    /// 更新冷却时间
    /// </summary>
    void UpdateCooldown()
    {
        if (!isOnCooldown) return;
        
        cooldownTimer -= Time.deltaTime;
        OnCooldownChanged?.Invoke(cooldownTimer);
        
        if (cooldownTimer <= 0)
        {
            isOnCooldown = false;
            cooldownTimer = 0;
            Debug.Log("特殊技能冷却完成");
        }
    }
    
    /// <summary>
    /// 强制重置冷却时间
    /// </summary>
    public void ResetCooldown()
    {
        isOnCooldown = false;
        cooldownTimer = 0;
        OnCooldownChanged?.Invoke(cooldownTimer);
    }
    
    /// <summary>
    /// 设置特殊技能数据
    /// </summary>
    public void SetSpecialSkillData(AttackData newSkillData)
    {
        if (newSkillData != null && newSkillData.attackType == AttackType.特殊技能)
        {
            specialSkillData = newSkillData;
            Debug.Log($"设置新的特殊技能：{newSkillData.attackName}");
        }
        else
        {
            Debug.LogWarning("提供的攻击数据不是特殊技能类型");
        }
    }
    
    /// <summary>
    /// 检查特殊技能是否可用
    /// </summary>
    public bool IsSpecialSkillAvailable()
    {
        if (isOnCooldown) return false;
        if (energySystem != null && !energySystem.CanUseSpecialSkill()) return false;
        if (specialSkillData == null) return false;
        return true;
    }
    
    /// <summary>
    /// 获取冷却进度（0-1）
    /// </summary>
    public float GetCooldownProgress()
    {
        if (!isOnCooldown) return 1f;
        return 1f - (cooldownTimer / cooldownTime);
    }
    
    /// <summary>
    /// 获取剩余冷却时间
    /// </summary>
    public float GetRemainingCooldown()
    {
        return isOnCooldown ? cooldownTimer : 0f;
    }
    
    // 属性访问器
    public bool IsOnCooldown => isOnCooldown;
    public AttackData SpecialSkillData => specialSkillData;
    public float CooldownTime => cooldownTime;
}