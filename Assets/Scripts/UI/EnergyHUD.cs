using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyHUD : MonoBehaviour
{
    [Header("能量槽组件")]
    [Tooltip("主能量槽")]
    public EnergyBar mainEnergyBar;
    
    [Header("特殊技能冷却显示")]
    [Tooltip("特殊技能图标")]
    public Image specialSkillIcon;
    [Tooltip("冷却遮罩")]
    public Image cooldownMask;
    [Tooltip("冷却时间文本")]
    public TextMeshProUGUI cooldownText;
    [Tooltip("特殊技能按键提示")]
    public TextMeshProUGUI keyHintText;
    [Tooltip("技能不可用时的灰度效果")]
    public bool useGrayscaleWhenUnavailable = true;
    
    [Header("状态指示器")]
    [Tooltip("能量不足警告")]
    public GameObject lowEnergyWarning;
    [Tooltip("特殊技能就绪指示")]
    public GameObject specialReadyIndicator;
    [Tooltip("能量满指示")]
    public GameObject energyFullIndicator;
    
    [Header("动画设置")]
    [Tooltip("图标闪烁速度")]
    public float blinkSpeed = 2f;
    [Tooltip("警告闪烁速度")]
    public float warningBlinkSpeed = 4f;
    
    // 组件引用
    private EnergySystem energySystem;
    private SpecialSkillController specialSkillController;
    
    // 状态变量
    private bool isEnergyLow;
    private bool isSpecialReady;
    private bool isEnergyFull;
    private Material originalIconMaterial;
    private Material grayscaleMaterial;
    
    void Start()
    {
        InitializeComponents();
        SetupMaterials();
        UpdateDisplay();
    }
    
    void Update()
    {
        UpdateSpecialSkillCooldown();
        UpdateStatusIndicators();
        UpdateAnimations();
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    void InitializeComponents()
    {
        // 查找玩家组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            energySystem = player.GetComponent<EnergySystem>();
            specialSkillController = player.GetComponent<SpecialSkillController>();
            
            if (energySystem != null)
            {
                energySystem.OnEnergyChanged += OnEnergyChanged;
                energySystem.OnSpecialSkillAvailable += OnSpecialSkillAvailable;
                energySystem.OnEnergyFull += OnEnergyFull;
                energySystem.OnEnergyEmpty += OnEnergyEmpty;
            }
            
            if (specialSkillController != null)
            {
                specialSkillController.OnCooldownChanged += OnCooldownChanged;
                specialSkillController.OnSpecialSkillUsed += OnSpecialSkillUsed;
                specialSkillController.OnSpecialSkillFailed += OnSpecialSkillFailed;
            }
        }
        
        // 设置主能量槽
        if (mainEnergyBar != null && energySystem != null)
        {
            mainEnergyBar.Initialize(energySystem);
        }
        
        // 设置按键提示
        if (keyHintText != null && specialSkillController != null)
        {
            keyHintText.text = GetKeyHintText();
        }
    }
    
    /// <summary>
    /// 设置材质
    /// </summary>
    void SetupMaterials()
    {
        if (specialSkillIcon != null)
        {
            originalIconMaterial = specialSkillIcon.material;
            
            if (useGrayscaleWhenUnavailable)
            {
                // 创建灰度材质（这里简化处理，实际项目中可能需要shader）
                grayscaleMaterial = new Material(originalIconMaterial);
                grayscaleMaterial.color = Color.gray;
            }
        }
    }
    
    /// <summary>
    /// 获取按键提示文本
    /// </summary>
    string GetKeyHintText()
    {
        if (specialSkillController == null) return "";
        
        string hint = specialSkillController.specialSkillKey.ToString();
        if (specialSkillController.requireModifier)
        {
            hint = specialSkillController.modifierKey.ToString() + " + " + hint;
        }
        return hint;
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    void UpdateDisplay()
    {
        if (energySystem == null) return;
        
        // 更新状态
        float energyPercentage = energySystem.GetEnergyPercentage();
        isEnergyLow = energyPercentage < 0.3f;
        isSpecialReady = energySystem.CanUseSpecialSkill() && 
                        (specialSkillController == null || !specialSkillController.IsOnCooldown);
        isEnergyFull = energyPercentage >= 1f;
        
        // 更新特殊技能图标
        UpdateSpecialSkillIcon();
    }
    
    /// <summary>
    /// 更新特殊技能冷却显示
    /// </summary>
    void UpdateSpecialSkillCooldown()
    {
        if (specialSkillController == null) return;
        
        float cooldownProgress = specialSkillController.GetCooldownProgress();
        float remainingTime = specialSkillController.GetRemainingCooldown();
        
        // 更新冷却遮罩
        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 1f - cooldownProgress;
            cooldownMask.gameObject.SetActive(specialSkillController.IsOnCooldown);
        }
        
        // 更新冷却时间文本
        if (cooldownText != null)
        {
            if (specialSkillController.IsOnCooldown)
            {
                cooldownText.text = remainingTime.ToString("F1");
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 更新特殊技能图标
    /// </summary>
    void UpdateSpecialSkillIcon()
    {
        if (specialSkillIcon == null) return;
        
        bool isAvailable = isSpecialReady && !isEnergyLow;
        
        // 设置图标材质
        if (useGrayscaleWhenUnavailable)
        {
            specialSkillIcon.material = isAvailable ? originalIconMaterial : grayscaleMaterial;
        }
        
        // 设置图标透明度
        Color iconColor = specialSkillIcon.color;
        iconColor.a = isAvailable ? 1f : 0.5f;
        specialSkillIcon.color = iconColor;
        
        // 设置技能数据图标
        if (specialSkillController != null && specialSkillController.SpecialSkillData != null)
        {
            // 这里可以设置技能图标，如果AttackData中有图标字段的话
            // specialSkillIcon.sprite = specialSkillController.SpecialSkillData.skillIcon;
        }
    }
    
    /// <summary>
    /// 更新状态指示器
    /// </summary>
    void UpdateStatusIndicators()
    {
        // 低能量警告
        if (lowEnergyWarning != null)
        {
            lowEnergyWarning.SetActive(isEnergyLow);
        }
        
        // 特殊技能就绪指示
        if (specialReadyIndicator != null)
        {
            specialReadyIndicator.SetActive(isSpecialReady);
        }
        
        // 能量满指示
        if (energyFullIndicator != null)
        {
            energyFullIndicator.SetActive(isEnergyFull);
        }
    }
    
    /// <summary>
    /// 更新动画效果
    /// </summary>
    void UpdateAnimations()
    {
        // 特殊技能就绪时的闪烁效果
        if (isSpecialReady && specialSkillIcon != null)
        {
            float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * blinkSpeed);
            Color color = specialSkillIcon.color;
            color.a = alpha;
            specialSkillIcon.color = color;
        }
        
        // 低能量警告闪烁
        if (isEnergyLow && lowEnergyWarning != null)
        {
            float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * warningBlinkSpeed);
            CanvasGroup canvasGroup = lowEnergyWarning.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }
    }
    
    /// <summary>
    /// 能量改变事件处理
    /// </summary>
    void OnEnergyChanged(int currentEnergy, int maxEnergy)
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 特殊技能可用状态改变事件处理
    /// </summary>
    void OnSpecialSkillAvailable(bool available)
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 能量满事件处理
    /// </summary>
    void OnEnergyFull()
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 能量空事件处理
    /// </summary>
    void OnEnergyEmpty()
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 冷却时间改变事件处理
    /// </summary>
    void OnCooldownChanged(float remainingTime)
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// 特殊技能使用事件处理
    /// </summary>
    void OnSpecialSkillUsed()
    {
        // 可以在这里添加特殊技能使用的视觉反馈
        Debug.Log("特殊技能已使用");
    }
    
    /// <summary>
    /// 特殊技能失败事件处理
    /// </summary>
    void OnSpecialSkillFailed()
    {
        // 可以在这里添加技能使用失败的视觉反馈
        StartCoroutine(ShakeIcon());
    }
    
    /// <summary>
    /// 图标震动效果
    /// </summary>
    System.Collections.IEnumerator ShakeIcon()
    {
        if (specialSkillIcon == null) yield break;
        
        Vector3 originalPosition = specialSkillIcon.transform.localPosition;
        float shakeIntensity = 10f;
        float shakeDuration = 0.3f;
        float timer = 0f;
        
        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * (1f - timer / shakeDuration);
            specialSkillIcon.transform.localPosition = originalPosition + shakeOffset;
            yield return null;
        }
        
        specialSkillIcon.transform.localPosition = originalPosition;
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (energySystem != null)
        {
            energySystem.OnEnergyChanged -= OnEnergyChanged;
            energySystem.OnSpecialSkillAvailable -= OnSpecialSkillAvailable;
            energySystem.OnEnergyFull -= OnEnergyFull;
            energySystem.OnEnergyEmpty -= OnEnergyEmpty;
        }
        
        if (specialSkillController != null)
        {
            specialSkillController.OnCooldownChanged -= OnCooldownChanged;
            specialSkillController.OnSpecialSkillUsed -= OnSpecialSkillUsed;
            specialSkillController.OnSpecialSkillFailed -= OnSpecialSkillFailed;
        }
    }
}