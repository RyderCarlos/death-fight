  using UnityEngine;
  using UnityEngine.UI;
  using TMPro;
  using System.Collections;

  public class ComboDisplay : MonoBehaviour
  {
      [Header("UI组件")]
      [Tooltip("连击数字文本")]
      public TextMeshProUGUI comboCountText;
      [Tooltip("连击倍数文本")]
      public TextMeshProUGUI comboMultiplierText;
      [Tooltip("连击名称文本")]
      public TextMeshProUGUI comboNameText;
      [Tooltip("连击面板")]
      public GameObject comboPanel;

      [Header("动画设置")]
      [Tooltip("数字增长动画")]
      public bool enableCountAnimation = true;
      [Tooltip("动画持续时间")]
      public float animationDuration = 0.3f;
      [Tooltip("数字放大倍数")]
      public float scaleMultiplier = 1.2f;

      [Header("颜色设置")]
      [Tooltip("普通连击颜色")]
      public Color normalColor = Color.white;
      [Tooltip("高连击颜色")]
      public Color highComboColor = Color.yellow;
      [Tooltip("超高连击颜色")]
      public Color ultraComboColor = Color.red;
      [Tooltip("高连击阈值")]
      public int highComboThreshold = 10;
      [Tooltip("超高连击阈值")]
      public int ultraComboThreshold = 20;

      [Header("特效设置")]
      [Tooltip("连击特效")]
      public ParticleSystem comboEffect;
      [Tooltip("连击完成特效")]
      public ParticleSystem comboCompleteEffect;
      [Tooltip("连击音效")]
      public AudioClip comboSound;

      [Header("显示设置")]
      [Tooltip("连击结束后隐藏延迟")]
      public float hideDelay = 2f;
      [Tooltip("淡入淡出时间")]
      public float fadeTime = 0.5f;

      private ComboSystem comboSystem;
      private AudioSource audioSource;
      private CanvasGroup canvasGroup;
      private RectTransform rectTransform;

      // 动画协程
      private Coroutine countAnimationCoroutine;
      private Coroutine hideCoroutine;
      private Coroutine fadeCoroutine;

      // 当前显示状态
      private int currentDisplayCount = 0;
      private float currentDisplayMultiplier = 1f;

      void Start()
      {
          InitializeComponents();
          InitializeUI();
          RegisterEvents();
      }

      /// <summary>
      /// 初始化组件
      /// </summary>
      void InitializeComponents()
      {
          // 查找ComboSystem（可能在父对象或其他地方）
          comboSystem = FindObjectOfType<ComboSystem>();

          audioSource = GetComponent<AudioSource>();
          canvasGroup = GetComponent<CanvasGroup>();
          rectTransform = GetComponent<RectTransform>();

          if (canvasGroup == null)
          {
              canvasGroup = gameObject.AddComponent<CanvasGroup>();
          }

          if (comboSystem == null)
          {
              Debug.LogWarning("ComboDisplay: 找不到 ComboSystem 组件");
          }
      }

      /// <summary>
      /// 初始化UI
      /// </summary>
      void InitializeUI()
      {
          // 初始隐藏
          if (comboPanel != null)
          {
              comboPanel.SetActive(false);
          }

          canvasGroup.alpha = 0f;

          // 初始化文本
          UpdateComboCountText(0);
          UpdateComboMultiplierText(1f);
          UpdateComboNameText("");
      }

      /// <summary>
      /// 注册事件
      /// </summary>
      void RegisterEvents()
      {
          if (comboSystem != null)
          {
              comboSystem.OnComboStart += OnComboStart;
              comboSystem.OnComboExtend += OnComboExtend;
              comboSystem.OnComboComplete += OnComboComplete;
              comboSystem.OnComboReset += OnComboReset;
              comboSystem.OnComboSequenceComplete += OnComboSequenceComplete;
          }
      }

      /// <summary>
      /// 连击开始事件
      /// </summary>
      void OnComboStart(int comboCount)
      {
          ShowComboDisplay();
          UpdateComboDisplay(comboCount, 1f);

          PlayComboEffect();
          PlayComboSound();
      }

      /// <summary>
      /// 连击扩展事件
      /// </summary>
      void OnComboExtend(int comboCount, float multiplier)
      {
          UpdateComboDisplay(comboCount, multiplier);

          PlayComboEffect();
          PlayComboSound();

          // 重置隐藏计时器
          ResetHideTimer();
      }

      /// <summary>
      /// 连击完成事件
      /// </summary>
      void OnComboComplete(int comboCount, ComboData comboData)
      {
          UpdateComboDisplay(comboCount, comboSystem.ComboMultiplier);

          if (comboData != null)
          {
              UpdateComboNameText(comboData.comboName);
          }

          PlayComboCompleteEffect();

          // 延迟隐藏
          StartHideTimer();
      }

      /// <summary>
      /// 连击重置事件
      /// </summary>
      void OnComboReset(int finalComboCount)
      {
          // 显示最终连击数一段时间后隐藏
          StartHideTimer();
      }

      /// <summary>
      /// 连击序列完成事件
      /// </summary>
      void OnComboSequenceComplete(ComboData comboData, int comboCount)
      {
          if (comboData != null)
          {
              UpdateComboNameText($"{comboData.comboName} 完成！");
          }

          PlayComboCompleteEffect();
      }

      /// <summary>
      /// 显示连击面板
      /// </summary>
      void ShowComboDisplay()
      {
          if (comboPanel != null)
          {
              comboPanel.SetActive(true);
          }

          StopFadeCoroutine();
          fadeCoroutine = StartCoroutine(FadeIn());

          // 停止隐藏计时器
          StopHideTimer();
      }

      /// <summary>
      /// 隐藏连击面板
      /// </summary>
      void HideComboDisplay()
      {
          StopFadeCoroutine();
          fadeCoroutine = StartCoroutine(FadeOut());
      }

      /// <summary>
      /// 淡入效果
      /// </summary>
      IEnumerator FadeIn()
      {
          float elapsedTime = 0f;
          float startAlpha = canvasGroup.alpha;

          while (elapsedTime < fadeTime)
          {
              elapsedTime += Time.deltaTime;
              canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeTime);
              yield return null;
          }

          canvasGroup.alpha = 1f;
      }

      /// <summary>
      /// 淡出效果
      /// </summary>
      IEnumerator FadeOut()
      {
          float elapsedTime = 0f;
          float startAlpha = canvasGroup.alpha;

          while (elapsedTime < fadeTime)
          {
              elapsedTime += Time.deltaTime;
              canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeTime);
              yield return null;
          }

          canvasGroup.alpha = 0f;

          if (comboPanel != null)
          {
              comboPanel.SetActive(false);
          }
      }

      /// <summary>
      /// 更新连击显示
      /// </summary>
      void UpdateComboDisplay(int comboCount, float multiplier)
      {
          UpdateComboCountText(comboCount);
          UpdateComboMultiplierText(multiplier);
          UpdateComboColors(comboCount);

          // 播放数字动画
          if (enableCountAnimation)
          {
              PlayCountAnimation();
          }
      }

      /// <summary>
      /// 更新连击数字文本
      /// </summary>
      void UpdateComboCountText(int count)
      {
          if (comboCountText != null)
          {
              comboCountText.text = count.ToString();
          }

          currentDisplayCount = count;
      }

      /// <summary>
      /// 更新连击倍数文本
      /// </summary>
      void UpdateComboMultiplierText(float multiplier)
      {
          if (comboMultiplierText != null)
          {
              comboMultiplierText.text = $"×{multiplier:F1}";
          }

          currentDisplayMultiplier = multiplier;
      }

      /// <summary>
      /// 更新连击名称文本
      /// </summary>
      void UpdateComboNameText(string comboName)
      {
          if (comboNameText != null)
          {
              comboNameText.text = comboName;
          }
      }

      /// <summary>
      /// 更新连击颜色
      /// </summary>
      void UpdateComboColors(int comboCount)
      {
          Color targetColor = normalColor;

          if (comboCount >= ultraComboThreshold)
          {
              targetColor = ultraComboColor;
          }
          else if (comboCount >= highComboThreshold)
          {
              targetColor = highComboColor;
          }

          if (comboCountText != null)
          {
              comboCountText.color = targetColor;
          }

          if (comboMultiplierText != null)
          {
              comboMultiplierText.color = targetColor;
          }
      }

      /// <summary>
      /// 播放数字动画
      /// </summary>
      void PlayCountAnimation()
      {
          if (countAnimationCoroutine != null)
          {
              StopCoroutine(countAnimationCoroutine);
          }

          countAnimationCoroutine = StartCoroutine(CountAnimationCoroutine());
      }

      /// <summary>
      /// 数字动画协程
      /// </summary>
      IEnumerator CountAnimationCoroutine()
      {
          if (rectTransform == null) yield break;

          Vector3 originalScale = rectTransform.localScale;
          Vector3 targetScale = originalScale * scaleMultiplier;

          float elapsedTime = 0f;

          // 放大
          while (elapsedTime < animationDuration / 2)
          {
              elapsedTime += Time.deltaTime;
              float progress = elapsedTime / (animationDuration / 2);
              rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
              yield return null;
          }

          elapsedTime = 0f;

          // 缩小
          while (elapsedTime < animationDuration / 2)
          {
              elapsedTime += Time.deltaTime;
              float progress = elapsedTime / (animationDuration / 2);
              rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
              yield return null;
          }

          rectTransform.localScale = originalScale;
      }

      /// <summary>
      /// 播放连击特效
      /// </summary>
      void PlayComboEffect()
      {
          if (comboEffect != null)
          {
              comboEffect.Play();
          }
      }

      /// <summary>
      /// 播放连击完成特效
      /// </summary>
      void PlayComboCompleteEffect()
      {
          if (comboCompleteEffect != null)
          {
              comboCompleteEffect.Play();
          }
      }

      /// <summary>
      /// 播放连击音效
      /// </summary>
      void PlayComboSound()
      {
          if (audioSource != null && comboSound != null)
          {
              audioSource.PlayOneShot(comboSound);
          }
      }

      /// <summary>
      /// 开始隐藏计时器
      /// </summary>
      void StartHideTimer()
      {
          StopHideTimer();
          hideCoroutine = StartCoroutine(HideTimerCoroutine());
      }

      /// <summary>
      /// 停止隐藏计时器
      /// </summary>
      void StopHideTimer()
      {
          if (hideCoroutine != null)
          {
              StopCoroutine(hideCoroutine);
              hideCoroutine = null;
          }
      }

      /// <summary>
      /// 重置隐藏计时器
      /// </summary>
      void ResetHideTimer()
      {
          StartHideTimer();
      }

      /// <summary>
      /// 隐藏计时器协程
      /// </summary>
      IEnumerator HideTimerCoroutine()
      {
          yield return new WaitForSeconds(hideDelay);
          HideComboDisplay();
      }

      /// <summary>
      /// 停止淡入淡出协程
      /// </summary>
      void StopFadeCoroutine()
      {
          if (fadeCoroutine != null)
          {
              StopCoroutine(fadeCoroutine);
              fadeCoroutine = null;
          }
      }

      /// <summary>
      /// 手动设置显示状态
      /// </summary>
      public void SetDisplayVisible(bool visible)
      {
          if (visible)
          {
              ShowComboDisplay();
          }
          else
          {
              HideComboDisplay();
          }
      }

      /// <summary>
      /// 获取当前显示的连击数
      /// </summary>
      public int GetCurrentDisplayCount()
      {
          return currentDisplayCount;
      }

      /// <summary>
      /// 设置目标连击系统
      /// </summary>
      public void SetTargetComboSystem(ComboSystem targetComboSystem)
      {
          // 取消之前的事件订阅
          if (comboSystem != null)
          {
              comboSystem.OnComboStart -= OnComboStart;
              comboSystem.OnComboExtend -= OnComboExtend;
              comboSystem.OnComboComplete -= OnComboComplete;
              comboSystem.OnComboReset -= OnComboReset;
              comboSystem.OnComboSequenceComplete -= OnComboSequenceComplete;
          }

          comboSystem = targetComboSystem;

          // 注册新的事件
          if (comboSystem != null)
          {
              comboSystem.OnComboStart += OnComboStart;
              comboSystem.OnComboExtend += OnComboExtend;
              comboSystem.OnComboComplete += OnComboComplete;
              comboSystem.OnComboReset += OnComboReset;
              comboSystem.OnComboSequenceComplete += OnComboSequenceComplete;
          }
      }

      void OnDestroy()
      {
          // 取消事件注册
          if (comboSystem != null)
          {
              comboSystem.OnComboStart -= OnComboStart;
              comboSystem.OnComboExtend -= OnComboExtend;
              comboSystem.OnComboComplete -= OnComboComplete;
              comboSystem.OnComboReset -= OnComboReset;
              comboSystem.OnComboSequenceComplete -= OnComboSequenceComplete;
          }

          // 停止所有协程
          StopAllCoroutines();
      }
  }