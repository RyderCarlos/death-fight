using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatHUD : MonoBehaviour
{
    [Header("HUD组件")]
    [Tooltip("血量条")]
    public HealthBar healthBar;
    [Tooltip("能量条")]
    public EnergyBar energyBar;
    [Tooltip("连击显示")]
    public ComboDisplay comboDisplay;
    [Tooltip("特殊技能冷却显示")]
    public Image specialSkillIcon;
    [Tooltip("特殊技能冷却遮罩")]
    public Image specialSkillCooldownMask;
    [Tooltip("特殊技能冷却文本")]
    public Text specialSkillCooldownText;

    [Header("状态显示")]
    [Tooltip("格挡状态图标")]
    public Image blockStatusIcon;
    [Tooltip("闪避状态图标")]
    public Image dodgeStatusIcon;
    [Tooltip("无敌状态图标")]
    public Image invincibilityIcon;
    [Tooltip("连击状态图标")]
    public Image comboStatusIcon;

    [Header("伤害显示")]
    [Tooltip("伤害数字预制体")]
    public GameObject damageNumberPrefab;
    [Tooltip("伤害数字显示时间")]
    public float damageNumberDuration = 1f;
    [Tooltip("伤害数字移动速度")]
    public float damageNumberSpeed = 2f;

    [Header("屏幕效果")]
    [Tooltip("受伤屏幕效果")]
    public Image hurtScreenEffect;
    [Tooltip("低血量屏幕效果")]
    public Image lowHealthScreenEffect;
    [Tooltip("死亡屏幕效果")]
    public Image deathScreenEffect;

    [Header("连击特效")]
    [Tooltip("连击特效容器")]
    public Transform comboEffectContainer;
    [Tooltip("连击特效预制体")]
    public GameObject[] comboEffectPrefabs;

    [Header("UI动画")]
    [Tooltip("UI进入动画")]
    public bool enableEnterAnimation = true;
    [Tooltip("UI退出动画")]
    public bool enableExitAnimation = true;
    [Tooltip("动画持续时间")]
    public float animationDuration = 0.5f;

    // 组件引用
    private HealthSystem playerHealthSystem;
    private EnergySystem playerEnergySystem;
    private ComboSystem playerComboSystem;
    private DefenseSystem playerDefenseSystem;
    private SpecialSkillController playerSpecialSkillController;

    // 状态跟踪
    private bool isInitialized = false;
    private Canvas hudCanvas;
    private CanvasGroup hudCanvasGroup;

    // 协程引用
    private Coroutine screenEffectCoroutine;
    private Coroutine statusUpdateCoroutine;

    void Start()
    {
        InitializeHUD();
        FindPlayerSystems();
        StartStatusUpdateCoroutine();
    }

    void Update()
    {
        if (!isInitialized) return;

        UpdateSpecialSkillDisplay();
        UpdateStatusIcons();
    }

    /// <summary>
    /// 初始化HUD
    /// </summary>
    void InitializeHUD()
    {
        hudCanvas = GetComponent<Canvas>();
        hudCanvasGroup = GetComponent<CanvasGroup>();

        if (hudCanvasGroup == null)
        {
            hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 设置初始状态
        SetScreenEffectsActive(false);
        
        if (enableEnterAnimation)
        {
            PlayEnterAnimation();
        }

        Debug.Log("战斗HUD初始化完成");
    }

    /// <summary>
    /// 查找玩家系统
    /// </summary>
    void FindPlayerSystems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>()?.gameObject;
        }

        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
            playerEnergySystem = player.GetComponent<EnergySystem>();
            playerComboSystem = player.GetComponent<ComboSystem>();
            playerDefenseSystem = player.GetComponent<DefenseSystem>();
            playerSpecialSkillController = player.GetComponent<SpecialSkillController>();

            SubscribeToPlayerEvents();
            InitializeSubSystems();
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("未找到玩家对象，HUD可能无法正常工作");
        }
    }

    /// <summary>
    /// 订阅玩家事件
    /// </summary>
    void SubscribeToPlayerEvents()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnTakeDamage += OnPlayerTakeDamage;
            playerHealthSystem.OnDeath += OnPlayerDeath;
            playerHealthSystem.OnRevive += OnPlayerRevive;
        }

        if (playerComboSystem != null)
        {
            playerComboSystem.OnComboStart += OnComboStart;
            playerComboSystem.OnComboExtend += OnComboExtend;
            playerComboSystem.OnComboComplete += OnComboComplete;
            playerComboSystem.OnComboSequenceComplete += OnComboSequenceComplete;
        }

        if (playerDefenseSystem != null)
        {
            playerDefenseSystem.OnBlock += OnPlayerBlock;
            playerDefenseSystem.OnPerfectBlock += OnPlayerPerfectBlock;
            playerDefenseSystem.OnDodgeStart += OnPlayerDodgeStart;
            playerDefenseSystem.OnDodgeEnd += OnPlayerDodgeEnd;
        }
    }

    /// <summary>
    /// 初始化子系统
    /// </summary>
    void InitializeSubSystems()
    {
        // 设置血量条目标
        if (healthBar != null && playerHealthSystem != null)
        {
            healthBar.SetTargetHealthSystem(playerHealthSystem);
        }

        // 设置能量条目标
        if (energyBar != null && playerEnergySystem != null)
        {
            energyBar.SetTargetEnergySystem(playerEnergySystem);
        }

        // 设置连击显示目标
        if (comboDisplay != null && playerComboSystem != null)
        {
            comboDisplay.SetTargetComboSystem(playerComboSystem);
        }
    }

    /// <summary>
    /// 开始状态更新协程
    /// </summary>
    void StartStatusUpdateCoroutine()
    {
        if (statusUpdateCoroutine != null)
        {
            StopCoroutine(statusUpdateCoroutine);
        }
        statusUpdateCoroutine = StartCoroutine(StatusUpdateCoroutine());
    }

    /// <summary>
    /// 状态更新协程
    /// </summary>
    IEnumerator StatusUpdateCoroutine()
    {
        while (true)
        {
            UpdateLowHealthScreenEffect();
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 更新特殊技能显示
    /// </summary>
    void UpdateSpecialSkillDisplay()
    {
        if (playerSpecialSkillController == null) return;

        // 更新冷却遮罩
        if (specialSkillCooldownMask != null)
        {
            float cooldownProgress = playerSpecialSkillController.GetCooldownProgress();
            specialSkillCooldownMask.fillAmount = 1f - cooldownProgress;
        }

        // 更新冷却文本
        if (specialSkillCooldownText != null)
        {
            float remainingTime = playerSpecialSkillController.GetRemainingCooldown();
            if (remainingTime > 0)
            {
                specialSkillCooldownText.text = remainingTime.ToString("F1");
                specialSkillCooldownText.gameObject.SetActive(true);
            }
            else
            {
                specialSkillCooldownText.gameObject.SetActive(false);
            }
        }

        // 更新图标透明度
        if (specialSkillIcon != null)
        {
            bool isAvailable = playerSpecialSkillController.IsSpecialSkillAvailable();
            Color iconColor = specialSkillIcon.color;
            iconColor.a = isAvailable ? 1f : 0.5f;
            specialSkillIcon.color = iconColor;
        }
    }

    /// <summary>
    /// 更新状态图标
    /// </summary>
    void UpdateStatusIcons()
    {
        if (playerDefenseSystem != null)
        {
            // 更新格挡状态图标
            if (blockStatusIcon != null)
            {
                blockStatusIcon.gameObject.SetActive(playerDefenseSystem.IsBlocking());
            }

            // 更新闪避状态图标
            if (dodgeStatusIcon != null)
            {
                dodgeStatusIcon.gameObject.SetActive(playerDefenseSystem.IsDodging());
            }

            // 更新无敌状态图标
            if (invincibilityIcon != null)
            {
                bool isInvincible = playerDefenseSystem.IsInvincible() || 
                                   (playerHealthSystem != null && playerHealthSystem.IsInvincible);
                invincibilityIcon.gameObject.SetActive(isInvincible);
            }
        }

        // 更新连击状态图标
        if (comboStatusIcon != null && playerComboSystem != null)
        {
            comboStatusIcon.gameObject.SetActive(playerComboSystem.IsInCombo);
        }
    }

    /// <summary>
    /// 玩家受伤事件处理
    /// </summary>
    void OnPlayerTakeDamage(DamageInfo damageInfo)
    {
        // 显示伤害数字
        ShowDamageNumber(damageInfo);

        // 播放受伤屏幕效果
        PlayHurtScreenEffect();
    }

    /// <summary>
    /// 玩家死亡事件处理
    /// </summary>
    void OnPlayerDeath()
    {
        PlayDeathScreenEffect();
    }

    /// <summary>
    /// 玩家复活事件处理
    /// </summary>
    void OnPlayerRevive(int currentHealth)
    {
        StopDeathScreenEffect();
    }

    /// <summary>
    /// 连击开始事件处理
    /// </summary>
    void OnComboStart(int comboCount)
    {
        PlayComboEffect(0);
    }

    /// <summary>
    /// 连击扩展事件处理
    /// </summary>
    void OnComboExtend(int comboCount, float multiplier)
    {
        int effectIndex = Mathf.Min(comboCount - 1, comboEffectPrefabs.Length - 1);
        PlayComboEffect(effectIndex);
    }

    /// <summary>
    /// 连击完成事件处理
    /// </summary>
    void OnComboComplete(int comboCount, ComboData comboData)
    {
        PlayComboEffect(comboEffectPrefabs.Length - 1);
    }

    /// <summary>
    /// 连击序列完成事件处理
    /// </summary>
    void OnComboSequenceComplete(ComboData comboData, int comboCount)
    {
        PlaySpecialComboEffect(comboData);
    }

    /// <summary>
    /// 玩家格挡事件处理
    /// </summary>
    void OnPlayerBlock(DamageInfo damageInfo)
    {
        ShowBlockEffect();
    }

    /// <summary>
    /// 玩家完美格挡事件处理
    /// </summary>
    void OnPlayerPerfectBlock(DamageInfo damageInfo)
    {
        ShowPerfectBlockEffect();
    }

    /// <summary>
    /// 玩家闪避开始事件处理
    /// </summary>
    void OnPlayerDodgeStart()
    {
        ShowDodgeEffect();
    }

    /// <summary>
    /// 玩家闪避结束事件处理
    /// </summary>
    void OnPlayerDodgeEnd()
    {
        // 可以添加闪避结束效果
    }

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    void ShowDamageNumber(DamageInfo damageInfo)
    {
        if (damageNumberPrefab == null) return;

        GameObject damageNumberObj = Instantiate(damageNumberPrefab, transform);
        
        // 设置位置
        RectTransform rectTransform = damageNumberObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(damageInfo.hitPosition);
            rectTransform.position = screenPos;
        }

        // 设置伤害数字
        Text damageText = damageNumberObj.GetComponent<Text>();
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damageInfo.finalDamage).ToString();
            damageText.color = damageInfo.isCritical ? Color.red : Color.white;
        }

        // 启动动画
        StartCoroutine(DamageNumberAnimation(damageNumberObj));
    }

    /// <summary>
    /// 伤害数字动画
    /// </summary>
    IEnumerator DamageNumberAnimation(GameObject damageNumberObj)
    {
        RectTransform rectTransform = damageNumberObj.GetComponent<RectTransform>();
        Text damageText = damageNumberObj.GetComponent<Text>();
        
        Vector3 startPos = rectTransform.position;
        Vector3 endPos = startPos + Vector3.up * 100f;
        
        float elapsed = 0f;
        
        while (elapsed < damageNumberDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / damageNumberDuration;
            
            // 位置动画
            rectTransform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // 透明度动画
            if (damageText != null)
            {
                Color color = damageText.color;
                color.a = Mathf.Lerp(1f, 0f, progress);
                damageText.color = color;
            }
            
            yield return null;
        }
        
        Destroy(damageNumberObj);
    }

    /// <summary>
    /// 播放受伤屏幕效果
    /// </summary>
    void PlayHurtScreenEffect()
    {
        if (hurtScreenEffect == null) return;

        if (screenEffectCoroutine != null)
        {
            StopCoroutine(screenEffectCoroutine);
        }
        
        screenEffectCoroutine = StartCoroutine(HurtScreenEffectCoroutine());
    }

    /// <summary>
    /// 受伤屏幕效果协程
    /// </summary>
    IEnumerator HurtScreenEffectCoroutine()
    {
        hurtScreenEffect.gameObject.SetActive(true);
        
        Color color = hurtScreenEffect.color;
        color.a = 0.3f;
        hurtScreenEffect.color = color;
        
        yield return new WaitForSeconds(0.1f);
        
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.3f, 0f, elapsed / 0.5f);
            hurtScreenEffect.color = color;
            yield return null;
        }
        
        hurtScreenEffect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新低血量屏幕效果
    /// </summary>
    void UpdateLowHealthScreenEffect()
    {
        if (lowHealthScreenEffect == null || playerHealthSystem == null) return;

        bool shouldShowEffect = playerHealthSystem.GetHealthPercentage() <= 0.25f && 
                               !playerHealthSystem.IsDead;

        if (shouldShowEffect != lowHealthScreenEffect.gameObject.activeSelf)
        {
            lowHealthScreenEffect.gameObject.SetActive(shouldShowEffect);
        }
    }

    /// <summary>
    /// 播放死亡屏幕效果
    /// </summary>
    void PlayDeathScreenEffect()
    {
        if (deathScreenEffect == null) return;

        deathScreenEffect.gameObject.SetActive(true);
        StartCoroutine(DeathScreenEffectCoroutine());
    }

    /// <summary>
    /// 死亡屏幕效果协程
    /// </summary>
    IEnumerator DeathScreenEffectCoroutine()
    {
        Color color = deathScreenEffect.color;
        color.a = 0f;
        deathScreenEffect.color = color;
        
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 0.8f, elapsed);
            deathScreenEffect.color = color;
            yield return null;
        }
    }

    /// <summary>
    /// 停止死亡屏幕效果
    /// </summary>
    void StopDeathScreenEffect()
    {
        if (deathScreenEffect != null)
        {
            deathScreenEffect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 播放连击特效
    /// </summary>
    void PlayComboEffect(int effectIndex)
    {
        if (comboEffectContainer == null || comboEffectPrefabs == null) return;
        if (effectIndex < 0 || effectIndex >= comboEffectPrefabs.Length) return;

        GameObject effect = Instantiate(comboEffectPrefabs[effectIndex], comboEffectContainer);
        Destroy(effect, 2f);
    }

    /// <summary>
    /// 播放特殊连击特效
    /// </summary>
    void PlaySpecialComboEffect(ComboData comboData)
    {
        if (comboData.comboEffect != null && comboEffectContainer != null)
        {
            GameObject effect = Instantiate(comboData.comboEffect, comboEffectContainer);
            Destroy(effect, 3f);
        }
    }

    /// <summary>
    /// 显示格挡效果
    /// </summary>
    void ShowBlockEffect()
    {
        Debug.Log("显示格挡效果");
        // 可以添加格挡特效
    }

    /// <summary>
    /// 显示完美格挡效果
    /// </summary>
    void ShowPerfectBlockEffect()
    {
        Debug.Log("显示完美格挡效果");
        // 可以添加完美格挡特效
    }

    /// <summary>
    /// 显示闪避效果
    /// </summary>
    void ShowDodgeEffect()
    {
        Debug.Log("显示闪避效果");
        // 可以添加闪避特效
    }

    /// <summary>
    /// 设置屏幕效果激活状态
    /// </summary>
    void SetScreenEffectsActive(bool active)
    {
        if (hurtScreenEffect != null)
            hurtScreenEffect.gameObject.SetActive(false);
        
        if (lowHealthScreenEffect != null)
            lowHealthScreenEffect.gameObject.SetActive(false);
        
        if (deathScreenEffect != null)
            deathScreenEffect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 播放进入动画
    /// </summary>
    void PlayEnterAnimation()
    {
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            StartCoroutine(FadeInCoroutine());
        }
    }

    /// <summary>
    /// 淡入协程
    /// </summary>
    IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            hudCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
            yield return null;
        }
        hudCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 播放退出动画
    /// </summary>
    public void PlayExitAnimation()
    {
        if (enableExitAnimation && hudCanvasGroup != null)
        {
            StartCoroutine(FadeOutCoroutine());
        }
    }

    /// <summary>
    /// 淡出协程
    /// </summary>
    IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            hudCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / animationDuration);
            yield return null;
        }
        hudCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 设置HUD可见性
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// 重置HUD
    /// </summary>
    public void ResetHUD()
    {
        StopAllCoroutines();
        SetScreenEffectsActive(false);
        
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 1f;
        }
    }

    void OnDestroy()
    {
        // 取消事件订阅
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnTakeDamage -= OnPlayerTakeDamage;
            playerHealthSystem.OnDeath -= OnPlayerDeath;
            playerHealthSystem.OnRevive -= OnPlayerRevive;
        }

        if (playerComboSystem != null)
        {
            playerComboSystem.OnComboStart -= OnComboStart;
            playerComboSystem.OnComboExtend -= OnComboExtend;
            playerComboSystem.OnComboComplete -= OnComboComplete;
            playerComboSystem.OnComboSequenceComplete -= OnComboSequenceComplete;
        }

        if (playerDefenseSystem != null)
        {
            playerDefenseSystem.OnBlock -= OnPlayerBlock;
            playerDefenseSystem.OnPerfectBlock -= OnPlayerPerfectBlock;
            playerDefenseSystem.OnDodgeStart -= OnPlayerDodgeStart;
            playerDefenseSystem.OnDodgeEnd -= OnPlayerDodgeEnd;
        }
    }
}