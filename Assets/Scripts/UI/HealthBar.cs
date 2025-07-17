using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBar : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("血量条填充图像")]
    public Image healthFillImage;
    [Tooltip("血量条背景图像")]
    public Image healthBackgroundImage;
    [Tooltip("血量文本")]
    public Text healthText;
    [Tooltip("血量百分比文本")]
    public Text healthPercentageText;
    [Tooltip("血量状态文本")]
    public Text healthStatusText;

    [Header("血量条设置")]
    [Tooltip("血量条平滑变化速度")]
    public float smoothSpeed = 2f;
    [Tooltip("是否显示数值")]
    public bool showHealthNumbers = true;
    [Tooltip("是否显示百分比")]
    public bool showHealthPercentage = true;
    [Tooltip("是否显示状态")]
    public bool showHealthStatus = true;

    [Header("颜色设置")]
    [Tooltip("健康状态颜色")]
    public Color healthyColor = Color.green;
    [Tooltip("受伤状态颜色")]
    public Color injuredColor = Color.yellow;
    [Tooltip("危险状态颜色")]
    public Color dangerColor = Color.red;
    [Tooltip("死亡状态颜色")]
    public Color deathColor = Color.gray;

    [Header("动画效果")]
    [Tooltip("受伤闪烁效果")]
    public bool enableHurtFlash = true;
    [Tooltip("受伤闪烁颜色")]
    public Color hurtFlashColor = Color.white;
    [Tooltip("受伤闪烁时间")]
    public float hurtFlashDuration = 0.2f;
    [Tooltip("低血量闪烁效果")]
    public bool enableLowHealthFlash = true;
    [Tooltip("低血量闪烁阈值")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.25f;

    [Header("血量条样式")]
    [Tooltip("血量条形状")]
    public HealthBarStyle barStyle = HealthBarStyle.水平;
    [Tooltip("血量条分段数")]
    public int segmentCount = 1;
    [Tooltip("分段间距")]
    public float segmentSpacing = 2f;

    private HealthSystem targetHealthSystem;
    private float currentDisplayHealth;
    private float targetDisplayHealth;
    private Coroutine smoothUpdateCoroutine;
    private Coroutine hurtFlashCoroutine;
    private Coroutine lowHealthFlashCoroutine;
    private bool isFlashing = false;

    // 分段血量条
    private Image[] segmentImages;
    private float segmentHealthValue;

    public enum HealthBarStyle
    {
        水平,
        垂直,
        分段,
        圆形
    }

    void Start()
    {
        InitializeHealthBar();
        FindTargetHealthSystem();
    }

    void Update()
    {
        if (targetHealthSystem == null)
        {
            FindTargetHealthSystem();
        }

        UpdateHealthBar();
        UpdateLowHealthFlash();
    }

    /// <summary>
    /// 初始化血量条
    /// </summary>
    void InitializeHealthBar()
    {
        if (healthFillImage == null)
        {
            healthFillImage = GetComponent<Image>();
        }

        if (segmentCount > 1)
        {
            CreateSegmentedHealthBar();
        }

        // 设置初始颜色
        if (healthFillImage != null)
        {
            healthFillImage.color = healthyColor;
        }
    }

    /// <summary>
    /// 创建分段血量条
    /// </summary>
    void CreateSegmentedHealthBar()
    {
        if (healthFillImage == null) return;

        segmentImages = new Image[segmentCount];
        segmentHealthValue = 1f / segmentCount;

        GameObject parentObject = healthFillImage.transform.parent.gameObject;
        
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segmentObj = Instantiate(healthFillImage.gameObject, parentObject.transform);
            segmentObj.name = $"HealthSegment_{i}";
            
            Image segmentImage = segmentObj.GetComponent<Image>();
            segmentImages[i] = segmentImage;

            // 设置分段位置
            RectTransform segmentRect = segmentObj.GetComponent<RectTransform>();
            float segmentWidth = (segmentRect.rect.width - (segmentCount - 1) * segmentSpacing) / segmentCount;
            
            segmentRect.sizeDelta = new Vector2(segmentWidth, segmentRect.sizeDelta.y);
            segmentRect.anchoredPosition = new Vector2(
                i * (segmentWidth + segmentSpacing), 
                segmentRect.anchoredPosition.y
            );
        }

        // 隐藏原始血量条
        healthFillImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 查找目标血量系统
    /// </summary>
    void FindTargetHealthSystem()
    {
        if (targetHealthSystem == null)
        {
            targetHealthSystem = FindObjectOfType<HealthSystem>();
            
            if (targetHealthSystem != null)
            {
                SubscribeToHealthEvents();
                currentDisplayHealth = targetHealthSystem.GetHealthPercentage();
                targetDisplayHealth = currentDisplayHealth;
            }
        }
    }

    /// <summary>
    /// 订阅血量系统事件
    /// </summary>
    void SubscribeToHealthEvents()
    {
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnHealthChanged += OnHealthChanged;
            targetHealthSystem.OnTakeDamage += OnTakeDamage;
            targetHealthSystem.OnDeath += OnDeath;
            targetHealthSystem.OnRevive += OnRevive;
        }
    }

    /// <summary>
    /// 设置目标血量系统
    /// </summary>
    public void SetTargetHealthSystem(HealthSystem healthSystem)
    {
        // 取消之前的订阅
        if (targetHealthSystem != null)
        {
            UnsubscribeFromHealthEvents();
        }

        targetHealthSystem = healthSystem;
        
        if (targetHealthSystem != null)
        {
            SubscribeToHealthEvents();
            currentDisplayHealth = targetHealthSystem.GetHealthPercentage();
            targetDisplayHealth = currentDisplayHealth;
            UpdateHealthBar();
        }
    }

    /// <summary>
    /// 取消血量系统事件订阅
    /// </summary>
    void UnsubscribeFromHealthEvents()
    {
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnHealthChanged -= OnHealthChanged;
            targetHealthSystem.OnTakeDamage -= OnTakeDamage;
            targetHealthSystem.OnDeath -= OnDeath;
            targetHealthSystem.OnRevive -= OnRevive;
        }
    }

    /// <summary>
    /// 血量变化事件处理
    /// </summary>
    void OnHealthChanged(int currentHealth, int maxHealth)
    {
        targetDisplayHealth = (float)currentHealth / maxHealth;
        StartSmoothUpdate();
    }

    /// <summary>
    /// 受伤事件处理
    /// </summary>
    void OnTakeDamage(DamageInfo damageInfo)
    {
        if (enableHurtFlash)
        {
            StartHurtFlash();
        }
    }

    /// <summary>
    /// 死亡事件处理
    /// </summary>
    void OnDeath()
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = deathColor;
        }

        StopAllFlashEffects();
    }

    /// <summary>
    /// 复活事件处理
    /// </summary>
    void OnRevive(int currentHealth)
    {
        UpdateHealthBarColor();
    }

    /// <summary>
    /// 开始平滑更新
    /// </summary>
    void StartSmoothUpdate()
    {
        if (smoothUpdateCoroutine != null)
        {
            StopCoroutine(smoothUpdateCoroutine);
        }
        smoothUpdateCoroutine = StartCoroutine(SmoothUpdateCoroutine());
    }

    /// <summary>
    /// 平滑更新协程
    /// </summary>
    IEnumerator SmoothUpdateCoroutine()
    {
        while (Mathf.Abs(currentDisplayHealth - targetDisplayHealth) > 0.01f)
        {
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetDisplayHealth, smoothSpeed * Time.deltaTime);
            UpdateHealthBarDisplay();
            yield return null;
        }

        currentDisplayHealth = targetDisplayHealth;
        UpdateHealthBarDisplay();
    }

    /// <summary>
    /// 更新血量条显示
    /// </summary>
    void UpdateHealthBar()
    {
        if (targetHealthSystem == null) return;

        UpdateHealthBarDisplay();
        UpdateHealthText();
        UpdateHealthBarColor();
    }

    /// <summary>
    /// 更新血量条显示
    /// </summary>
    void UpdateHealthBarDisplay()
    {
        if (segmentCount > 1 && segmentImages != null)
        {
            UpdateSegmentedHealthBar();
        }
        else if (healthFillImage != null)
        {
            switch (barStyle)
            {
                case HealthBarStyle.水平:
                case HealthBarStyle.垂直:
                    healthFillImage.fillAmount = currentDisplayHealth;
                    break;
                case HealthBarStyle.圆形:
                    healthFillImage.fillAmount = currentDisplayHealth;
                    break;
            }
        }
    }

    /// <summary>
    /// 更新分段血量条
    /// </summary>
    void UpdateSegmentedHealthBar()
    {
        for (int i = 0; i < segmentImages.Length; i++)
        {
            float segmentThreshold = (i + 1) * segmentHealthValue;
            
            if (currentDisplayHealth >= segmentThreshold)
            {
                segmentImages[i].fillAmount = 1f;
            }
            else if (currentDisplayHealth > i * segmentHealthValue)
            {
                float segmentProgress = (currentDisplayHealth - i * segmentHealthValue) / segmentHealthValue;
                segmentImages[i].fillAmount = segmentProgress;
            }
            else
            {
                segmentImages[i].fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// 更新血量文本
    /// </summary>
    void UpdateHealthText()
    {
        if (targetHealthSystem == null) return;

        // 更新数值文本
        if (showHealthNumbers && healthText != null)
        {
            healthText.text = $"{targetHealthSystem.CurrentHealth}/{targetHealthSystem.MaxHealth}";
        }

        // 更新百分比文本
        if (showHealthPercentage && healthPercentageText != null)
        {
            float percentage = targetHealthSystem.GetHealthPercentage() * 100f;
            healthPercentageText.text = $"{percentage:F0}%";
        }

        // 更新状态文本
        if (showHealthStatus && healthStatusText != null)
        {
            HealthStatus status = targetHealthSystem.GetHealthStatus();
            healthStatusText.text = GetHealthStatusText(status);
        }
    }

    /// <summary>
    /// 获取血量状态文本
    /// </summary>
    string GetHealthStatusText(HealthStatus status)
    {
        switch (status)
        {
            case HealthStatus.健康: return "健康";
            case HealthStatus.良好: return "良好";
            case HealthStatus.受伤: return "受伤";
            case HealthStatus.危险: return "危险";
            case HealthStatus.死亡: return "死亡";
            default: return "未知";
        }
    }

    /// <summary>
    /// 更新血量条颜色
    /// </summary>
    void UpdateHealthBarColor()
    {
        if (targetHealthSystem == null || isFlashing) return;

        Color targetColor = GetHealthColor();
        
        if (segmentCount > 1 && segmentImages != null)
        {
            foreach (var segment in segmentImages)
            {
                segment.color = targetColor;
            }
        }
        else if (healthFillImage != null)
        {
            healthFillImage.color = targetColor;
        }
    }

    /// <summary>
    /// 获取血量对应的颜色
    /// </summary>
    Color GetHealthColor()
    {
        if (targetHealthSystem == null) return healthyColor;

        HealthStatus status = targetHealthSystem.GetHealthStatus();
        
        switch (status)
        {
            case HealthStatus.健康:
            case HealthStatus.良好:
                return healthyColor;
            case HealthStatus.受伤:
                return injuredColor;
            case HealthStatus.危险:
                return dangerColor;
            case HealthStatus.死亡:
                return deathColor;
            default:
                return healthyColor;
        }
    }

    /// <summary>
    /// 开始受伤闪烁
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
        isFlashing = true;
        Color originalColor = GetHealthColor();

        // 设置闪烁颜色
        SetHealthBarColor(hurtFlashColor);

        yield return new WaitForSeconds(hurtFlashDuration);

        // 恢复原色
        SetHealthBarColor(originalColor);
        isFlashing = false;
    }

    /// <summary>
    /// 设置血量条颜色
    /// </summary>
    void SetHealthBarColor(Color color)
    {
        if (segmentCount > 1 && segmentImages != null)
        {
            foreach (var segment in segmentImages)
            {
                segment.color = color;
            }
        }
        else if (healthFillImage != null)
        {
            healthFillImage.color = color;
        }
    }

    /// <summary>
    /// 更新低血量闪烁
    /// </summary>
    void UpdateLowHealthFlash()
    {
        if (!enableLowHealthFlash || targetHealthSystem == null) return;

        bool shouldFlash = targetHealthSystem.GetHealthPercentage() <= lowHealthThreshold && 
                          !targetHealthSystem.IsDead;

        if (shouldFlash && lowHealthFlashCoroutine == null)
        {
            lowHealthFlashCoroutine = StartCoroutine(LowHealthFlashCoroutine());
        }
        else if (!shouldFlash && lowHealthFlashCoroutine != null)
        {
            StopCoroutine(lowHealthFlashCoroutine);
            lowHealthFlashCoroutine = null;
        }
    }

    /// <summary>
    /// 低血量闪烁协程
    /// </summary>
    IEnumerator LowHealthFlashCoroutine()
    {
        while (true)
        {
            if (!isFlashing)
            {
                SetHealthBarColor(dangerColor);
                yield return new WaitForSeconds(0.5f);
                
                SetHealthBarColor(GetHealthColor());
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// 停止所有闪烁效果
    /// </summary>
    void StopAllFlashEffects()
    {
        if (hurtFlashCoroutine != null)
        {
            StopCoroutine(hurtFlashCoroutine);
            hurtFlashCoroutine = null;
        }

        if (lowHealthFlashCoroutine != null)
        {
            StopCoroutine(lowHealthFlashCoroutine);
            lowHealthFlashCoroutine = null;
        }

        isFlashing = false;
    }

    /// <summary>
    /// 设置血量条样式
    /// </summary>
    public void SetHealthBarStyle(HealthBarStyle style)
    {
        barStyle = style;
        
        if (healthFillImage != null)
        {
            switch (style)
            {
                case HealthBarStyle.水平:
                    healthFillImage.type = Image.Type.Filled;
                    healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                    break;
                case HealthBarStyle.垂直:
                    healthFillImage.type = Image.Type.Filled;
                    healthFillImage.fillMethod = Image.FillMethod.Vertical;
                    break;
                case HealthBarStyle.圆形:
                    healthFillImage.type = Image.Type.Filled;
                    healthFillImage.fillMethod = Image.FillMethod.Radial360;
                    break;
            }
        }
    }

    void OnDestroy()
    {
        UnsubscribeFromHealthEvents();
        StopAllFlashEffects();
    }
}