using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// AI调试器 - 用于测试和调试AI行为
/// </summary>
public class AIDebugger : MonoBehaviour
{
    [Header("调试UI")]
    public GameObject debugPanel;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI behaviorText;
    public Slider difficultySlider;
    public Button[] difficultyButtons;
    
    [Header("目标AI")]
    public AIController targetAI;
    public bool autoFindAI = true;
    
    [Header("调试设置")]
    public bool showDebugPanel = false;
    public bool logAIBehavior = true;
    public float updateInterval = 0.1f;
    
    [Header("可视化")]
    public bool showGizmos = true;
    public LineRenderer pathLineRenderer;
    public GameObject waypointPrefab;
    
    // 内部状态
    private float lastUpdateTime;
    private List<Vector3> aiPath = new List<Vector3>();
    private List<string> recentBehaviors = new List<string>();
    private int maxBehaviorHistory = 10;
    
    void Start()
    {
        InitializeDebugger();
        
        if (autoFindAI && targetAI == null)
        {
            targetAI = FindObjectOfType<AIController>();
        }
        
        SetupUI();
    }
    
    void Update()
    {
        // 切换调试面板
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleDebugPanel();
        }
        
        // 更新调试信息
        if (showDebugPanel && Time.time - lastUpdateTime > updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
        
        // 记录AI路径
        if (targetAI != null && showGizmos)
        {
            RecordAIPath();
        }
    }
    
    #region 初始化
    
    void InitializeDebugger()
    {
        // 如果没有调试面板，创建一个简单的
        if (debugPanel == null)
        {
            CreateDebugPanel();
        }
        
        // 订阅AI事件
        if (targetAI != null)
        {
            SubscribeToAIEvents();
        }
    }
    
    void CreateDebugPanel()
    {
        // 创建Canvas（如果不存在）
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DebugCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 创建调试面板
        debugPanel = new GameObject("AI Debug Panel");
        debugPanel.transform.SetParent(canvas.transform, false);
        
        // 添加背景
        Image background = debugPanel.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.8f);
        
        // 设置面板位置和大小
        RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.5f);
        panelRect.anchorMax = new Vector2(0.4f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 创建文本组件
        CreateDebugText();
        
        debugPanel.SetActive(showDebugPanel);
    }
    
    void CreateDebugText()
    {
        // 信息文本
        GameObject infoObj = new GameObject("Info Text");
        infoObj.transform.SetParent(debugPanel.transform, false);
        infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.text = "AI调试信息";
        infoText.fontSize = 14;
        infoText.color = Color.white;
        
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0.6f);
        infoRect.anchorMax = new Vector2(1, 1f);
        infoRect.offsetMin = new Vector2(10, 0);
        infoRect.offsetMax = new Vector2(-10, -10);
        
        // 状态文本
        GameObject stateObj = new GameObject("State Text");
        stateObj.transform.SetParent(debugPanel.transform, false);
        stateText = stateObj.AddComponent<TextMeshProUGUI>();
        stateText.text = "状态信息";
        stateText.fontSize = 12;
        stateText.color = Color.yellow;
        
        RectTransform stateRect = stateObj.GetComponent<RectTransform>();
        stateRect.anchorMin = new Vector2(0, 0.3f);
        stateRect.anchorMax = new Vector2(1, 0.6f);
        stateRect.offsetMin = new Vector2(10, 0);
        stateRect.offsetMax = new Vector2(-10, 0);
        
        // 行为文本
        GameObject behaviorObj = new GameObject("Behavior Text");
        behaviorObj.transform.SetParent(debugPanel.transform, false);
        behaviorText = behaviorObj.AddComponent<TextMeshProUGUI>();
        behaviorText.text = "行为历史";
        behaviorText.fontSize = 10;
        behaviorText.color = Color.cyan;
        
        RectTransform behaviorRect = behaviorObj.GetComponent<RectTransform>();
        behaviorRect.anchorMin = new Vector2(0, 0f);
        behaviorRect.anchorMax = new Vector2(1, 0.3f);
        behaviorRect.offsetMin = new Vector2(10, 10);
        behaviorRect.offsetMax = new Vector2(-10, 0);
    }
    
    void SetupUI()
    {
        // 设置难度滑块
        if (difficultySlider != null)
        {
            difficultySlider.minValue = 0.3f;
            difficultySlider.maxValue = 2.5f;
            difficultySlider.value = 1f;
            difficultySlider.onValueChanged.AddListener(OnDifficultySliderChanged);
        }
        
        // 设置难度按钮
        if (difficultyButtons != null)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int difficulty = i; // 捕获循环变量
                if (difficultyButtons[i] != null)
                {
                    difficultyButtons[i].onClick.AddListener(() => SetAIDifficulty((AIDifficulty)difficulty));
                }
            }
        }
    }
    
    #endregion
    
    #region AI事件处理
    
    void SubscribeToAIEvents()
    {
        if (targetAI == null) return;
        
        // 订阅行为事件
        targetAI.OnBehaviorExecuted += OnAIBehaviorExecuted;
        targetAI.OnStateChanged += OnAIStateChanged;
        targetAI.OnTargetFound += OnTargetFound;
        targetAI.OnTargetLost += OnTargetLost;
    }
    
    void UnsubscribeFromAIEvents()
    {
        if (targetAI == null) return;
        
        targetAI.OnBehaviorExecuted -= OnAIBehaviorExecuted;
        targetAI.OnStateChanged -= OnAIStateChanged;
        targetAI.OnTargetFound -= OnTargetFound;
        targetAI.OnTargetLost -= OnTargetLost;
    }
    
    void OnAIBehaviorExecuted(string behavior)
    {
        recentBehaviors.Add($"{Time.time:F1}s: {behavior}");
        
        if (recentBehaviors.Count > maxBehaviorHistory)
        {
            recentBehaviors.RemoveAt(0);
        }
        
        if (logAIBehavior)
        {
            Debug.Log($"[AI行为] {targetAI.name}: {behavior}");
        }
    }
    
    void OnAIStateChanged(AIState oldState, AIState newState)
    {
        string stateChange = $"{oldState} → {newState}";
        OnAIBehaviorExecuted($"状态变更: {stateChange}");
    }
    
    void OnTargetFound(Transform target)
    {
        OnAIBehaviorExecuted($"发现目标: {target.name}");
    }
    
    void OnTargetLost()
    {
        OnAIBehaviorExecuted("目标丢失");
    }
    
    #endregion
    
    #region 调试信息更新
    
    void UpdateDebugInfo()
    {
        if (targetAI == null)
        {
            if (infoText != null)
                infoText.text = "未找到目标AI";
            return;
        }
        
        // 更新基础信息
        UpdateBasicInfo();
        
        // 更新状态信息
        UpdateStateInfo();
        
        // 更新行为历史
        UpdateBehaviorHistory();
    }
    
    void UpdateBasicInfo()
    {
        if (infoText == null) return;
        
        string info = $"=== AI调试信息 ===\n";
        info += $"AI名称: {targetAI.name}\n";
        info += $"当前状态: {targetAI.currentState}\n";
        
        if (targetAI.target != null)
        {
            info += $"目标: {targetAI.target.name}\n";
            info += $"距离: {targetAI.distanceToTarget:F2}\n";
            info += $"可见: {targetAI.canSeeTarget}\n";
        }
        else
        {
            info += "目标: 无\n";
        }
        
        // AI数据信息
        if (targetAI.aiData != null)
        {
            info += $"\n=== AI数据 ===\n";
            info += $"难度: {targetAI.aiData.difficulty}\n";
            info += $"反应时间: {targetAI.aiData.reactionTime:F2}s\n";
            info += $"攻击频率: {targetAI.aiData.attackFrequency:F2}/s\n";
            info += $"防御成功率: {targetAI.aiData.defenseSuccessRate:P0}\n";
        }
        
        infoText.text = info;
    }
    
    void UpdateStateInfo()
    {
        if (stateText == null || targetAI == null) return;
        
        string stateInfo = $"=== 状态详情 ===\n";
        
        // 健康状态
        if (targetAI.healthSystem != null)
        {
            float healthPercentage = targetAI.healthSystem.GetHealthPercentage();
            stateInfo += $"血量: {healthPercentage:P0}\n";
            stateInfo += $"状态: {targetAI.healthSystem.currentHealthState}\n";
        }
        
        // 能量状态
        if (targetAI.energySystem != null)
        {
            float energy = targetAI.energySystem.GetCurrentEnergy();
            float maxEnergy = targetAI.energySystem.maxEnergy;
            stateInfo += $"能量: {energy:F0}/{maxEnergy:F0}\n";
            stateInfo += $"特殊技能: {(targetAI.energySystem.CanUseSpecialSkill() ? "可用" : "不可用")}\n";
        }
        
        // 战斗状态
        if (targetAI.attackSystem != null)
        {
            stateInfo += $"总攻击: {targetAI.attackSystem.totalAttacks}\n";
            stateInfo += $"命中次数: {targetAI.attackSystem.successfulHits}\n";
        }
        
        stateText.text = stateInfo;
    }
    
    void UpdateBehaviorHistory()
    {
        if (behaviorText == null) return;
        
        string behaviorInfo = "=== 行为历史 ===\n";
        
        for (int i = recentBehaviors.Count - 1; i >= 0; i--)
        {
            behaviorInfo += recentBehaviors[i] + "\n";
        }
        
        behaviorText.text = behaviorInfo;
    }
    
    #endregion
    
    #region 路径记录和可视化
    
    void RecordAIPath()
    {
        if (targetAI == null) return;
        
        Vector3 currentPos = targetAI.transform.position;
        
        // 记录路径点
        if (aiPath.Count == 0 || Vector3.Distance(currentPos, aiPath[aiPath.Count - 1]) > 0.5f)
        {
            aiPath.Add(currentPos);
            
            // 限制路径点数量
            if (aiPath.Count > 50)
            {
                aiPath.RemoveAt(0);
            }
        }
        
        // 更新线渲染器
        UpdatePathVisualization();
    }
    
    void UpdatePathVisualization()
    {
        if (pathLineRenderer != null && aiPath.Count > 1)
        {
            pathLineRenderer.positionCount = aiPath.Count;
            pathLineRenderer.SetPositions(aiPath.ToArray());
        }
    }
    
    #endregion
    
    #region 控制接口
    
    public void ToggleDebugPanel()
    {
        showDebugPanel = !showDebugPanel;
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugPanel);
        }
    }
    
    public void SetTargetAI(AIController ai)
    {
        // 取消订阅旧AI事件
        UnsubscribeFromAIEvents();
        
        targetAI = ai;
        
        // 订阅新AI事件
        SubscribeToAIEvents();
        
        // 清空路径
        aiPath.Clear();
        recentBehaviors.Clear();
    }
    
    public void OnDifficultySliderChanged(float value)
    {
        if (targetAI != null && AIDifficultyManager.Instance != null)
        {
            AIDifficultyManager.Instance.AdjustAIDifficulty(targetAI, value);
        }
    }
    
    public void SetAIDifficulty(AIDifficulty difficulty)
    {
        if (AIDifficultyManager.Instance != null)
        {
            AIDifficultyManager.Instance.SetGlobalDifficulty(difficulty);
        }
    }
    
    public void ResetAI()
    {
        if (targetAI != null)
        {
            // 重置AI状态
            if (targetAI.healthSystem != null)
            {
                targetAI.healthSystem.Revive(1f);
            }
            
            if (targetAI.energySystem != null)
            {
                targetAI.energySystem.SetEnergy(targetAI.energySystem.maxEnergy);
            }
            
            // 清空历史
            recentBehaviors.Clear();
            aiPath.Clear();
        }
    }
    
    public void TriggerAIBehavior(string behaviorName)
    {
        if (targetAI == null) return;
        
        switch (behaviorName.ToLower())
        {
            case "attack":
                targetAI.TryAttack();
                break;
            case "defend":
                targetAI.TryDefend();
                break;
            case "move":
                if (targetAI.target != null)
                    targetAI.MoveToTarget();
                break;
        }
    }
    
    #endregion
    
    #region 调试绘制
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || targetAI == null) return;
        
        // 绘制AI路径
        Gizmos.color = Color.blue;
        for (int i = 0; i < aiPath.Count - 1; i++)
        {
            Gizmos.DrawLine(aiPath[i], aiPath[i + 1]);
        }
        
        // 绘制路径点
        Gizmos.color = Color.cyan;
        foreach (Vector3 point in aiPath)
        {
            Gizmos.DrawWireSphere(point, 0.1f);
        }
        
        // 绘制AI状态信息
        if (targetAI.target != null)
        {
            Gizmos.color = targetAI.canSeeTarget ? Color.green : Color.red;
            Gizmos.DrawLine(targetAI.transform.position, targetAI.target.position);
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        UnsubscribeFromAIEvents();
    }
}

/// <summary>
/// AI测试工具 - 用于自动化测试AI行为
/// </summary>
[System.Serializable]
public class AITestSuite
{
    public string testName;
    public List<AITestCase> testCases = new List<AITestCase>();
    
    public void RunTests(AIController ai)
    {
        foreach (AITestCase testCase in testCases)
        {
            testCase.Execute(ai);
        }
    }
}

[System.Serializable]
public class AITestCase
{
    public string caseName;
    public AITestType testType;
    public float expectedValue;
    public float tolerance = 0.1f;
    
    public bool Execute(AIController ai)
    {
        float actualValue = GetTestValue(ai);
        bool passed = Mathf.Abs(actualValue - expectedValue) <= tolerance;
        
        Debug.Log($"[AI测试] {caseName}: {(passed ? "通过" : "失败")} (期望: {expectedValue}, 实际: {actualValue})");
        
        return passed;
    }
    
    private float GetTestValue(AIController ai)
    {
        switch (testType)
        {
            case AITestType.ReactionTime:
                return ai.aiData.reactionTime;
            case AITestType.AttackFrequency:
                return ai.aiData.attackFrequency;
            case AITestType.DefenseSuccessRate:
                return ai.aiData.defenseSuccessRate;
            default:
                return 0f;
        }
    }
}

public enum AITestType
{
    ReactionTime,
    AttackFrequency,
    DefenseSuccessRate,
    TargetDetection,
    PathFinding
}