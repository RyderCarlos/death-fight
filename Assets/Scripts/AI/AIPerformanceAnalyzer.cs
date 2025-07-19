using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AI性能分析工具 - 分析AI行为效率和表现
/// </summary>
public class AIPerformanceAnalyzer : MonoBehaviour
{
    [Header("分析设置")]
    public bool enableAnalysis = true;
    public float analysisInterval = 5f;
    public int maxDataPoints = 100;
    
    [Header("UI显示")]
    public GameObject performancePanel;
    public TextMeshProUGUI performanceText;
    public LineRenderer chartRenderer;
    
    [Header("分析目标")]
    public List<AIController> analyzedAIs = new List<AIController>();
    public bool autoFindAIs = true;
    
    // 性能数据
    private Dictionary<AIController, AIPerformanceData> performanceData = new Dictionary<AIController, AIPerformanceData>();
    private List<float> globalPerformanceHistory = new List<float>();
    private float lastAnalysisTime;
    
    // 统计数据
    private AIAnalyticsData analyticsData = new AIAnalyticsData();
    
    void Start()
    {
        if (autoFindAIs)
        {
            FindAllAIs();
        }
        
        InitializePerformanceTracking();
    }
    
    void Update()
    {
        if (enableAnalysis && Time.time - lastAnalysisTime > analysisInterval)
        {
            PerformAnalysis();
            lastAnalysisTime = Time.time;
        }
        
        // 切换性能面板
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TogglePerformancePanel();
        }
    }
    
    #region 初始化
    
    void FindAllAIs()
    {
        analyzedAIs.Clear();
        AIController[] ais = FindObjectsOfType<AIController>();
        analyzedAIs.AddRange(ais);
    }
    
    void InitializePerformanceTracking()
    {
        foreach (AIController ai in analyzedAIs)
        {
            if (ai != null)
            {
                performanceData[ai] = new AIPerformanceData();
                SubscribeToAIEvents(ai);
            }
        }
    }
    
    void SubscribeToAIEvents(AIController ai)
    {
        // 订阅AI事件来收集数据
        ai.OnBehaviorExecuted += (behavior) => RecordBehavior(ai, behavior);
        ai.OnStateChanged += (oldState, newState) => RecordStateChange(ai, oldState, newState);
        
        if (ai.attackSystem != null)
        {
            ai.attackSystem.OnAttackStart += (attackData) => RecordAttack(ai, attackData);
            ai.attackSystem.OnHitTarget += (target, attackData) => RecordHit(ai, target, attackData);
        }
        
        if (ai.healthSystem != null)
        {
            ai.healthSystem.OnTakeDamage += (damageInfo) => RecordDamageTaken(ai, damageInfo);
        }
    }
    
    #endregion
    
    #region 数据收集
    
    void RecordBehavior(AIController ai, string behavior)
    {
        if (!performanceData.ContainsKey(ai)) return;
        
        AIPerformanceData data = performanceData[ai];
        data.totalBehaviors++;
        data.lastBehaviorTime = Time.time;
        
        // 分析行为类型
        if (behavior.Contains("攻击"))
        {
            data.attackBehaviors++;
        }
        else if (behavior.Contains("防御"))
        {
            data.defenseBehaviors++;
        }
        else if (behavior.Contains("移动"))
        {
            data.movementBehaviors++;
        }
        
        // 记录决策时间
        if (data.lastDecisionTime > 0)
        {
            float decisionTime = Time.time - data.lastDecisionTime;
            data.decisionTimes.Add(decisionTime);
            
            if (data.decisionTimes.Count > maxDataPoints)
            {
                data.decisionTimes.RemoveAt(0);
            }
        }
        data.lastDecisionTime = Time.time;
    }
    
    void RecordStateChange(AIController ai, AIState oldState, AIState newState)
    {
        if (!performanceData.ContainsKey(ai)) return;
        
        AIPerformanceData data = performanceData[ai];
        data.stateChanges++;
        
        // 记录状态持续时间
        if (data.lastStateChangeTime > 0)
        {
            float stateDuration = Time.time - data.lastStateChangeTime;
            data.stateDurations.Add(stateDuration);
            
            if (data.stateDurations.Count > maxDataPoints)
            {
                data.stateDurations.RemoveAt(0);
            }
        }
        data.lastStateChangeTime = Time.time;
    }
    
    void RecordAttack(AIController ai, AttackData attackData)
    {
        if (!performanceData.ContainsKey(ai)) return;
        
        AIPerformanceData data = performanceData[ai];
        data.totalAttacks++;
        data.lastAttackTime = Time.time;
    }
    
    void RecordHit(AIController ai, GameObject target, AttackData attackData)
    {
        if (!performanceData.ContainsKey(ai)) return;
        
        AIPerformanceData data = performanceData[ai];
        data.successfulHits++;
        
        // 计算命中率
        data.hitRate = data.totalAttacks > 0 ? (float)data.successfulHits / data.totalAttacks : 0f;
    }
    
    void RecordDamageTaken(AIController ai, DamageInfo damageInfo)
    {
        if (!performanceData.ContainsKey(ai)) return;
        
        AIPerformanceData data = performanceData[ai];
        data.damageTaken += damageInfo.finalDamage;
        data.timesHit++;
    }
    
    #endregion
    
    #region 性能分析
    
    void PerformAnalysis()
    {
        analyticsData.Reset();
        
        foreach (var kvp in performanceData)
        {
            AIController ai = kvp.Key;
            AIPerformanceData data = kvp.Value;
            
            // 分析单个AI
            AnalyzeAIPerformance(ai, data);
        }
        
        // 全局分析
        PerformGlobalAnalysis();
        
        // 更新显示
        UpdatePerformanceDisplay();
    }
    
    void AnalyzeAIPerformance(AIController ai, AIPerformanceData data)
    {
        // 计算效率指标
        data.efficiency = CalculateEfficiency(data);
        data.responsiveness = CalculateResponsiveness(data);
        data.adaptability = CalculateAdaptability(data);
        
        // 记录到全局统计
        analyticsData.totalAIs++;
        analyticsData.averageEfficiency += data.efficiency;
        analyticsData.averageHitRate += data.hitRate;
        
        if (data.efficiency > analyticsData.bestPerformingAI?.efficiency)
        {
            analyticsData.bestPerformingAI = data;
        }
        
        if (data.efficiency < analyticsData.worstPerformingAI?.efficiency || analyticsData.worstPerformingAI == null)
        {
            analyticsData.worstPerformingAI = data;
        }
    }
    
    float CalculateEfficiency(AIPerformanceData data)
    {
        if (data.totalBehaviors == 0) return 0f;
        
        // 效率 = (有效行为数 / 总行为数) * 命中率
        float effectiveBehaviors = data.attackBehaviors + data.defenseBehaviors;
        float behaviorEfficiency = effectiveBehaviors / data.totalBehaviors;
        
        return (behaviorEfficiency + data.hitRate) * 0.5f;
    }
    
    float CalculateResponsiveness(AIPerformanceData data)
    {
        if (data.decisionTimes.Count == 0) return 1f;
        
        // 响应性 = 1 / 平均决策时间
        float averageDecisionTime = data.decisionTimes.Average();
        return Mathf.Clamp01(1f / (averageDecisionTime + 0.1f));
    }
    
    float CalculateAdaptability(AIPerformanceData data)
    {
        if (data.stateDurations.Count < 2) return 0.5f;
        
        // 适应性基于状态变化的多样性
        float variance = CalculateVariance(data.stateDurations);
        return Mathf.Clamp01(variance * 0.5f);
    }
    
    float CalculateVariance(List<float> values)
    {
        if (values.Count < 2) return 0f;
        
        float mean = values.Average();
        float sumOfSquares = values.Sum(x => (x - mean) * (x - mean));
        return sumOfSquares / values.Count;
    }
    
    void PerformGlobalAnalysis()
    {
        if (analyticsData.totalAIs > 0)
        {
            analyticsData.averageEfficiency /= analyticsData.totalAIs;
            analyticsData.averageHitRate /= analyticsData.totalAIs;
        }
        
        // 记录全局性能历史
        globalPerformanceHistory.Add(analyticsData.averageEfficiency);
        
        if (globalPerformanceHistory.Count > maxDataPoints)
        {
            globalPerformanceHistory.RemoveAt(0);
        }
        
        // 性能趋势分析
        analyticsData.performanceTrend = CalculatePerformanceTrend();
    }
    
    PerformanceTrend CalculatePerformanceTrend()
    {
        if (globalPerformanceHistory.Count < 3) return PerformanceTrend.Stable;
        
        float recent = globalPerformanceHistory.Skip(globalPerformanceHistory.Count - 3).Average();
        float earlier = globalPerformanceHistory.Take(globalPerformanceHistory.Count - 3).Average();
        
        float difference = recent - earlier;
        
        if (difference > 0.1f) return PerformanceTrend.Improving;
        if (difference < -0.1f) return PerformanceTrend.Declining;
        return PerformanceTrend.Stable;
    }
    
    #endregion
    
    #region 性能优化建议
    
    public List<string> GetOptimizationSuggestions(AIController ai)
    {
        List<string> suggestions = new List<string>();
        
        if (!performanceData.ContainsKey(ai))
        {
            suggestions.Add("无法获取AI性能数据");
            return suggestions;
        }
        
        AIPerformanceData data = performanceData[ai];
        
        // 命中率建议
        if (data.hitRate < 0.3f)
        {
            suggestions.Add("命中率过低，建议调整攻击时机判断");
        }
        else if (data.hitRate > 0.8f)
        {
            suggestions.Add("命中率过高，建议增加攻击难度");
        }
        
        // 响应性建议
        if (data.responsiveness < 0.5f)
        {
            suggestions.Add("反应速度较慢，建议减少决策时间");
        }
        else if (data.responsiveness > 0.9f)
        {
            suggestions.Add("反应过快，建议增加反应延迟以提升真实感");
        }
        
        // 行为多样性建议
        float totalBehaviors = data.attackBehaviors + data.defenseBehaviors + data.movementBehaviors;
        if (totalBehaviors > 0)
        {
            float attackRatio = data.attackBehaviors / totalBehaviors;
            if (attackRatio > 0.7f)
            {
                suggestions.Add("过于激进，建议增加防御和移动行为");
            }
            else if (attackRatio < 0.3f)
            {
                suggestions.Add("过于保守，建议增加攻击行为");
            }
        }
        
        // 效率建议
        if (data.efficiency < 0.4f)
        {
            suggestions.Add("整体效率较低，建议优化行为选择逻辑");
        }
        
        return suggestions;
    }
    
    #endregion
    
    #region UI更新
    
    void UpdatePerformanceDisplay()
    {
        if (performanceText == null) return;
        
        string displayText = "=== AI性能分析 ===\n";
        displayText += $"分析AI数量: {analyticsData.totalAIs}\n";
        displayText += $"平均效率: {analyticsData.averageEfficiency:P1}\n";
        displayText += $"平均命中率: {analyticsData.averageHitRate:P1}\n";
        displayText += $"性能趋势: {analyticsData.performanceTrend}\n\n";
        
        // 显示各个AI的详细信息
        foreach (var kvp in performanceData)
        {
            AIController ai = kvp.Key;
            AIPerformanceData data = kvp.Value;
            
            if (ai != null)
            {
                displayText += $"--- {ai.name} ---\n";
                displayText += $"效率: {data.efficiency:P1}\n";
                displayText += $"命中率: {data.hitRate:P1}\n";
                displayText += $"响应性: {data.responsiveness:P1}\n";
                displayText += $"适应性: {data.adaptability:P1}\n";
                displayText += $"总行为: {data.totalBehaviors}\n\n";
            }
        }
        
        performanceText.text = displayText;
        
        // 更新图表
        UpdatePerformanceChart();
    }
    
    void UpdatePerformanceChart()
    {
        if (chartRenderer == null || globalPerformanceHistory.Count < 2) return;
        
        chartRenderer.positionCount = globalPerformanceHistory.Count;
        
        for (int i = 0; i < globalPerformanceHistory.Count; i++)
        {
            float x = i * 0.1f;
            float y = globalPerformanceHistory[i] * 5f; // 缩放显示
            chartRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    
    void TogglePerformancePanel()
    {
        if (performancePanel != null)
        {
            performancePanel.SetActive(!performancePanel.activeSelf);
        }
    }
    
    #endregion
    
    #region 公共接口
    
    public AIPerformanceData GetPerformanceData(AIController ai)
    {
        return performanceData.ContainsKey(ai) ? performanceData[ai] : null;
    }
    
    public AIAnalyticsData GetAnalyticsData()
    {
        return analyticsData;
    }
    
    public void ResetAnalysis()
    {
        foreach (var data in performanceData.Values)
        {
            data.Reset();
        }
        
        globalPerformanceHistory.Clear();
        analyticsData.Reset();
    }
    
    public void ExportAnalysisData()
    {
        // 导出分析数据到文件
        string data = GenerateAnalysisReport();
        
        string filename = $"AI_Analysis_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        
        try
        {
            System.IO.File.WriteAllText(path, data);
            Debug.Log($"分析数据已导出到: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出失败: {e.Message}");
        }
    }
    
    string GenerateAnalysisReport()
    {
        string report = "=== AI性能分析报告 ===\n";
        report += $"生成时间: {System.DateTime.Now}\n";
        report += $"分析时长: {Time.time:F1}秒\n\n";
        
        report += "=== 全局统计 ===\n";
        report += $"分析AI数量: {analyticsData.totalAIs}\n";
        report += $"平均效率: {analyticsData.averageEfficiency:P2}\n";
        report += $"平均命中率: {analyticsData.averageHitRate:P2}\n";
        report += $"性能趋势: {analyticsData.performanceTrend}\n\n";
        
        report += "=== 详细数据 ===\n";
        foreach (var kvp in performanceData)
        {
            AIController ai = kvp.Key;
            AIPerformanceData data = kvp.Value;
            
            if (ai != null)
            {
                report += $"AI: {ai.name}\n";
                report += $"  效率: {data.efficiency:P2}\n";
                report += $"  命中率: {data.hitRate:P2}\n";
                report += $"  响应性: {data.responsiveness:P2}\n";
                report += $"  适应性: {data.adaptability:P2}\n";
                report += $"  总攻击: {data.totalAttacks}\n";
                report += $"  成功命中: {data.successfulHits}\n";
                report += $"  受到伤害: {data.damageTaken:F1}\n";
                report += $"  总行为: {data.totalBehaviors}\n\n";
                
                // 优化建议
                var suggestions = GetOptimizationSuggestions(ai);
                if (suggestions.Count > 0)
                {
                    report += "  优化建议:\n";
                    foreach (string suggestion in suggestions)
                    {
                        report += $"    - {suggestion}\n";
                    }
                    report += "\n";
                }
            }
        }
        
        return report;
    }
    
    #endregion
}

/// <summary>
/// AI性能数据
/// </summary>
[System.Serializable]
public class AIPerformanceData
{
    // 基础统计
    public int totalBehaviors = 0;
    public int attackBehaviors = 0;
    public int defenseBehaviors = 0;
    public int movementBehaviors = 0;
    public int stateChanges = 0;
    
    // 战斗统计
    public int totalAttacks = 0;
    public int successfulHits = 0;
    public float hitRate = 0f;
    public float damageTaken = 0f;
    public int timesHit = 0;
    
    // 性能指标
    public float efficiency = 0f;
    public float responsiveness = 0f;
    public float adaptability = 0f;
    
    // 时间数据
    public List<float> decisionTimes = new List<float>();
    public List<float> stateDurations = new List<float>();
    public float lastBehaviorTime = 0f;
    public float lastDecisionTime = 0f;
    public float lastStateChangeTime = 0f;
    public float lastAttackTime = 0f;
    
    public void Reset()
    {
        totalBehaviors = 0;
        attackBehaviors = 0;
        defenseBehaviors = 0;
        movementBehaviors = 0;
        stateChanges = 0;
        totalAttacks = 0;
        successfulHits = 0;
        hitRate = 0f;
        damageTaken = 0f;
        timesHit = 0;
        efficiency = 0f;
        responsiveness = 0f;
        adaptability = 0f;
        
        decisionTimes.Clear();
        stateDurations.Clear();
        
        lastBehaviorTime = 0f;
        lastDecisionTime = 0f;
        lastStateChangeTime = 0f;
        lastAttackTime = 0f;
    }
}

/// <summary>
/// AI分析数据
/// </summary>
[System.Serializable]
public class AIAnalyticsData
{
    public int totalAIs = 0;
    public float averageEfficiency = 0f;
    public float averageHitRate = 0f;
    public PerformanceTrend performanceTrend = PerformanceTrend.Stable;
    public AIPerformanceData bestPerformingAI = null;
    public AIPerformanceData worstPerformingAI = null;
    
    public void Reset()
    {
        totalAIs = 0;
        averageEfficiency = 0f;
        averageHitRate = 0f;
        performanceTrend = PerformanceTrend.Stable;
        bestPerformingAI = null;
        worstPerformingAI = null;
    }
}

/// <summary>
/// 性能趋势枚举
/// </summary>
public enum PerformanceTrend
{
    Improving,  // 改善中
    Stable,     // 稳定
    Declining   // 下降中
}