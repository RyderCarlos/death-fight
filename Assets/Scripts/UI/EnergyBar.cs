using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnergyBar : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("能量槽填充图像")]
    public Image energyFillImage;
    [Tooltip("能量槽背景")]
    public Image energyBackgroundImage;
    [Tooltip("特殊技能阈值指示器")]
    public Image specialThresholdIndicator;
    [Tooltip("能量数值文本")]
    public TextMeshProUGUI energyText;
    [Tooltip("特殊技能可用指示器")]
    public GameObject specialSkillAvailableIndicator;
    
    [Header("视觉效果")]
    [Tooltip("能量槽颜色")]
    public Color normalEnergyColor = Color.blue;
    [Tooltip("特殊技能可用时的颜色")]
    public Color specialAvailableColor = Color.green;
    [Tooltip("能量不足时的颜色")]
    public Color lowEnergyColor = Color.red;
    [Tooltip("低能量阈值")]
    public float lowEnergyThreshold = 0.2f;
    
    [Header("动画设置")]
    [Tooltip("填充动画速度")]
    public float fillAnimationSpeed = 2f;
    [Tooltip("脉冲动画速度")]
    public float pulseSpeed = 2f;
    [Tooltip("脉冲强度")]
    public float pulseIntensity = 0.2f;
    [Tooltip("特殊技能可用时是否发光")]
    public bool glowWhenSpecialAvailable = true;
    
    [Header("音效")]
    [Tooltip("能量增加音效")]
    public AudioClip energyGainSound;
    [Tooltip("能量消耗音效")]
    public AudioClip energyConsumeSound;
    [Tooltip("特殊技能可用音效")]
    public AudioClip specialAvailableSound;
    
    // 私有变量
    private EnergySystem energySystem;
    private AudioSource audioSource;
    private float targetFillAmount;
    private float currentFillAmount;
    private bool isSpecialAvailable;
    private Coroutine pulseCoroutine;
    
    // 特效和动画
    private Color originalBackgroundColor;
    private Vector3 originalScale;
    
    void Start()
    {
        InitializeComponents();
        SetupEnergySystem();
        InitializeUI();
    }
    
    void Update()
    {
        UpdateFillAnimation();
        UpdateVisualEffects();
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 保存原始值
        if (energyBackgroundImage != null)
            originalBackgroundColor = energyBackgroundImage.color;
        
        originalScale = transform.localScale;
        
        // 验证必要组件
        if (energyFillImage == null)
        {
            Debug.LogError("EnergyBar: energyFillImage 未设置！");
        }
    }
    
    /// <summary>
    /// 设置能量系统
    /// </summary>
    void SetupEnergySystem()
    {
        // 查找玩家的能量系统
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            energySystem = player.GetComponent<EnergySystem>();
            if (energySystem != null)
            {
                // 订阅事件
                energySystem.OnEnergyChanged += OnEnergyChanged;
                energySystem.OnEnergyFull += OnEnergyFull;
                energySystem.OnEnergyEmpty += OnEnergyEmpty;
                energySystem.OnSpecialSkillAvailable += OnSpecialSkillAvailable;
                
                Debug.Log("能量槽UI已连接到能量系统");
            }
            else
            {
                Debug.LogWarning("在玩家对象上找不到 EnergySystem 组件");
            }
        }
        else
        {
            Debug.LogWarning("找不到标签为 'Player' 的游戏对象");
        }
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    void InitializeUI()
    {
        if (energySystem != null)
        {
            UpdateEnergyDisplay(energySystem.CurrentEnergy, energySystem.MaxEnergy);
            UpdateSpecialThresholdIndicator();
        }
        else
        {
            // 默认显示
            UpdateEnergyDisplay(0, 100);
        }
        
        if (specialSkillAvailableIndicator != null)
        {
            specialSkillAvailableIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// 更新填充动画
    /// </summary>
    void UpdateFillAnimation()
    {
        if (energyFillImage == null) return;
        
        // 平滑插值到目标填充量
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, 
            fillAnimationSpeed * Time.deltaTime);
        
        energyFillImage.fillAmount = currentFillAmount;
    }
    
    /// <summary>
    /// 更新视觉效果
    /// </summary>
    void UpdateVisualEffects()
    {
        if (energyFillImage == null) return;
        
        // 根据能量状态设置颜色
        Color targetColor;
        
        if (isSpecialAvailable)
        {
            targetColor = specialAvailableColor;
        }
        else if (targetFillAmount <= lowEnergyThreshold)
        {
            targetColor = lowEnergyColor;
        }
        else
        {
            targetColor = normalEnergyColor;
        }
        
        energyFillImage.color = Color.Lerp(energyFillImage.color, targetColor, 
            Time.deltaTime * 3f);
    }
    
    /// <summary>
    /// 能量改变事件处理
    /// </summary>
    void OnEnergyChanged(int currentEnergy, int maxEnergy)
    {
        UpdateEnergyDisplay(currentEnergy, maxEnergy);
        
        // 播放音效
        if (currentEnergy > energySystem.CurrentEnergy && energyGainSound != null)
        {
            PlaySound(energyGainSound);
        }
        else if (currentEnergy < energySystem.CurrentEnergy && energyConsumeSound != null)
        {
            PlaySound(energyConsumeSound);
        }
    }
    
    /// <summary>
    /// 更新能量显示
    /// </summary>
    void UpdateEnergyDisplay(int currentEnergy, int maxEnergy)
    {
        // 更新填充量
        targetFillAmount = (float)currentEnergy / maxEnergy;
        
        // 更新文本
        if (energyText != null)
        {
            energyText.text = $"{currentEnergy}/{maxEnergy}";
        }
    }
    
    /// <summary>
    /// 更新特殊技能阈值指示器
    /// </summary>
    void UpdateSpecialThresholdIndicator()
    {
        if (specialThresholdIndicator == null || energySystem == null) return;
        
        float thresholdPosition = (float)energySystem.SpecialSkillThreshold / energySystem.MaxEnergy;
        
        // 设置阈值指示器位置
        RectTransform rectTransform = specialThresholdIndicator.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            anchoredPosition.x = thresholdPosition * rectTransform.parent.GetComponent<RectTransform>().rect.width;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
    
    /// <summary>
    /// 能量满事件处理
    /// </summary>
    void OnEnergyFull()
    {
        StartCoroutine(EnergyFullEffect());
    }
    
    /// <summary>
    /// 能量空事件处理
    /// </summary>
    void OnEnergyEmpty()
    {
        StartCoroutine(EnergyEmptyEffect());
    }
    
    /// <summary>
    /// 特殊技能可用状态改变事件处理
    /// </summary>
    void OnSpecialSkillAvailable(bool available)
    {
        isSpecialAvailable = available;
        
        if (specialSkillAvailableIndicator != null)
        {
            specialSkillAvailableIndicator.SetActive(available);
        }
        
        if (available)
        {
            if (specialAvailableSound != null)
            {
                PlaySound(specialAvailableSound);
            }
            
            if (glowWhenSpecialAvailable)
            {
                StartGlowEffect();
            }
        }
        else
        {
            StopGlowEffect();
        }
    }
    
    /// <summary>
    /// 能量满特效
    /// </summary>
    IEnumerator EnergyFullEffect()
    {
        float duration = 0.5f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float intensity = Mathf.Sin(timer * 10f) * 0.5f + 0.5f;
            
            if (energyBackgroundImage != null)
            {
                Color glowColor = Color.Lerp(originalBackgroundColor, Color.white, intensity * 0.3f);
                energyBackgroundImage.color = glowColor;
            }
            
            yield return null;
        }
        
        if (energyBackgroundImage != null)
        {
            energyBackgroundImage.color = originalBackgroundColor;
        }
    }
    
    /// <summary>
    /// 能量空特效
    /// </summary>
    IEnumerator EnergyEmptyEffect()
    {
        float duration = 1f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float intensity = Mathf.Sin(timer * 8f) * 0.5f + 0.5f;
            
            transform.localScale = originalScale * (1f + intensity * 0.1f);
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 开始发光效果
    /// </summary>
    void StartGlowEffect()
    {
        StopGlowEffect();
        pulseCoroutine = StartCoroutine(PulseEffect());
    }
    
    /// <summary>
    /// 停止发光效果
    /// </summary>
    void StopGlowEffect()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // 重置效果
        transform.localScale = originalScale;
        if (energyBackgroundImage != null)
        {
            energyBackgroundImage.color = originalBackgroundColor;
        }
    }
    
    /// <summary>
    /// 脉冲效果
    /// </summary>
    IEnumerator PulseEffect()
    {
        while (true)
        {
            float pulseValue = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            
            // 缩放脉冲
            transform.localScale = originalScale * (1f + pulseValue);
            
            // 颜色脉冲
            if (energyBackgroundImage != null)
            {
                Color pulseColor = Color.Lerp(originalBackgroundColor, specialAvailableColor, 
                    Mathf.Abs(pulseValue));
                energyBackgroundImage.color = pulseColor;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 手动设置能量系统（用于测试或特殊情况）
    /// </summary>
    public void SetEnergySystem(EnergySystem newEnergySystem)
    {
        // 取消旧的订阅
        if (energySystem != null)
        {
            energySystem.OnEnergyChanged -= OnEnergyChanged;
            energySystem.OnEnergyFull -= OnEnergyFull;
            energySystem.OnEnergyEmpty -= OnEnergyEmpty;
            energySystem.OnSpecialSkillAvailable -= OnSpecialSkillAvailable;
        }
        
        energySystem = newEnergySystem;
        
        if (energySystem != null)
        {
            // 订阅新的事件
            energySystem.OnEnergyChanged += OnEnergyChanged;
            energySystem.OnEnergyFull += OnEnergyFull;
            energySystem.OnEnergyEmpty += OnEnergyEmpty;
            energySystem.OnSpecialSkillAvailable += OnSpecialSkillAvailable;
            
            // 更新显示
            UpdateEnergyDisplay(energySystem.CurrentEnergy, energySystem.MaxEnergy);
            UpdateSpecialThresholdIndicator();
        }
    }

    /// <summary>
    /// 设置目标能量系统（与CombatHUD兼容）
    /// </summary>
    public void SetTargetEnergySystem(EnergySystem targetEnergySystem)
    {
        SetEnergySystem(targetEnergySystem);
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (energySystem != null)
        {
            energySystem.OnEnergyChanged -= OnEnergyChanged;
            energySystem.OnEnergyFull -= OnEnergyFull;
            energySystem.OnEnergyEmpty -= OnEnergyEmpty;
            energySystem.OnSpecialSkillAvailable -= OnSpecialSkillAvailable;
        }
    }
}