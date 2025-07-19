using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// AI难度管理器 - 动态调整AI难度和性能
/// </summary>
public class AIDifficultyManager : MonoBehaviour
{
    [Header("全局难度设置")]
    public AIDifficulty globalDifficulty = AIDifficulty.普通;
    public bool adaptiveDifficulty = true;
    public float difficultyUpdateInterval = 10f;
    
    [Header("自适应难度参数")]
    [Range(0f, 1f)]
    public float playerWinRate = 0.5f; // 目标胜率
    [Range(0f, 1f)]
    public float adjustmentSensitivity = 0.1f; // 调整敏感度
    public int evaluationWindow = 5; // 评估窗口（回合数）
    
    [Header("性能平衡")]
    public bool enablePerformanceBalancing = true;
    public float maxReactionTimeVariation = 0.5f;
    public float maxSkillVariation = 0.3f;
    
    [Header("调试信息")]
    public bool showDebugInfo = true;
    
    // 单例
    public static AIDifficultyManager Instance { get; private set; }
    
    // 内部状态
    private Dictionary<AIController, DifficultyProfile> aiProfiles = new Dictionary<AIController, DifficultyProfile>();
    private List<CombatResult> recentResults = new List<CombatResult>();
    private float lastUpdateTime;
    
    // 事件
    public event Action<AIDifficulty> OnGlobalDifficultyChanged;
    public event Action<AIController, DifficultyProfile> OnAIDifficultyAdjusted;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        lastUpdateTime = Time.time;
        
        // 查找所有AI并注册
        RegisterAllAIs();
    }
    
    void Update()
    {
        // 定期更新难度
        if (adaptiveDifficulty && Time.time - lastUpdateTime > difficultyUpdateInterval)
        {
            UpdateAdaptiveDifficulty();
            lastUpdateTime = Time.time;
        }
    }
    
    #region AI注册和管理
    
    private void RegisterAllAIs()
    {
        AIController[] ais = FindObjectsOfType<AIController>();
        foreach (AIController ai in ais)
        {
            RegisterAI(ai);
        }
    }
    
    public void RegisterAI(AIController ai)
    {
        if (ai == null || aiProfiles.ContainsKey(ai)) return;
        
        // 创建难度配置文件
        DifficultyProfile profile = new DifficultyProfile();
        profile.Initialize(ai.aiData, globalDifficulty);
        
        aiProfiles[ai] = profile;
        
        // 应用初始难度设置
        ApplyDifficultyToAI(ai, profile);
        
        if (showDebugInfo)
        {
            Debug.Log($"[难度管理] 注册AI: {ai.name}, 难度: {globalDifficulty}");
        }
    }
    
    public void UnregisterAI(AIController ai)
    {
        if (ai != null && aiProfiles.ContainsKey(ai))
        {
            aiProfiles.Remove(ai);
        }
    }
    
    #endregion
    
    #region 难度调整
    
    public void SetGlobalDifficulty(AIDifficulty difficulty)
    {
        globalDifficulty = difficulty;
        
        // 更新所有AI的难度
        foreach (var kvp in aiProfiles)
        {
            AIController ai = kvp.Key;
            DifficultyProfile profile = kvp.Value;
            
            profile.UpdateForDifficulty(difficulty);
            ApplyDifficultyToAI(ai, profile);
        }
        
        OnGlobalDifficultyChanged?.Invoke(difficulty);
        
        if (showDebugInfo)
        {
            Debug.Log($"[难度管理] 全局难度设置为: {difficulty}");
        }
    }
    
    public void AdjustAIDifficulty(AIController ai, float difficultyModifier)
    {
        if (!aiProfiles.ContainsKey(ai)) return;
        
        DifficultyProfile profile = aiProfiles[ai];
        profile.ApplyModifier(difficultyModifier);
        
        ApplyDifficultyToAI(ai, profile);
        OnAIDifficultyAdjusted?.Invoke(ai, profile);
        
        if (showDebugInfo)
        {
            Debug.Log($"[难度管理] 调整AI {ai.name} 难度: {difficultyModifier:F2}");
        }
    }
    
    private void ApplyDifficultyToAI(AIController ai, DifficultyProfile profile)
    {
        if (ai.aiData == null) return;
        
        // 应用反应时间调整
        ai.aiData.reactionTime = profile.adjustedReactionTime;
        
        // 应用攻击频率调整
        ai.aiData.attackFrequency = profile.adjustedAttackFrequency;
        
        // 应用防御成功率调整
        ai.aiData.defenseSuccessRate = profile.adjustedDefenseSuccessRate;
        
        // 应用其他参数
        ai.aiData.perfectBlockChance = profile.adjustedPerfectBlockChance;
        ai.aiData.counterAttackChance = profile.adjustedCounterAttackChance;
        ai.aiData.specialSkillChance = profile.adjustedSpecialSkillChance;
        ai.aiData.comboChance = profile.adjustedComboChance;
    }
    
    #endregion
    
    #region 自适应难度
    
    private void UpdateAdaptiveDifficulty()
    {
        if (recentResults.Count < evaluationWindow) return;
        
        // 计算最近的胜率
        float currentWinRate = CalculatePlayerWinRate();
        
        // 计算需要的调整量
        float difficultyAdjustment = CalculateDifficultyAdjustment(currentWinRate);
        
        if (Mathf.Abs(difficultyAdjustment) > 0.1f)
        {
            // 应用难度调整
            ApplyAdaptiveDifficultyAdjustment(difficultyAdjustment);
            
            if (showDebugInfo)
            {
                Debug.Log($"[自适应难度] 玩家胜率: {currentWinRate:F2}, 调整: {difficultyAdjustment:F2}");
            }
        }
    }
    
    private float CalculatePlayerWinRate()
    {
        if (recentResults.Count == 0) return 0.5f;
        
        int playerWins = 0;
        int totalBattles = 0;
        
        foreach (CombatResult result in recentResults)
        {
            if (result.isPlayerVictory)
                playerWins++;
            totalBattles++;
        }
        
        return (float)playerWins / totalBattles;
    }
    
    private float CalculateDifficultyAdjustment(float currentWinRate)
    {
        float winRateDifference = currentWinRate - playerWinRate;
        
        // 如果玩家胜率过高，增加AI难度
        // 如果玩家胜率过低，降低AI难度
        float adjustment = -winRateDifference * adjustmentSensitivity;
        
        return Mathf.Clamp(adjustment, -1f, 1f);
    }
    
    private void ApplyAdaptiveDifficultyAdjustment(float adjustment)
    {
        foreach (var kvp in aiProfiles)
        {
            AIController ai = kvp.Key;
            DifficultyProfile profile = kvp.Value;
            
            // 应用调整
            profile.ApplyAdaptiveAdjustment(adjustment);
            ApplyDifficultyToAI(ai, profile);
        }
    }
    
    #endregion
    
    #region 性能平衡
    
    public void ApplyPerformanceBalancing(AIController ai)
    {
        if (!enablePerformanceBalancing || !aiProfiles.ContainsKey(ai)) return;
        
        DifficultyProfile profile = aiProfiles[ai];
        
        // 添加随机变化来模拟真实玩家的不一致性
        float reactionVariation = UnityEngine.Random.Range(-maxReactionTimeVariation, maxReactionTimeVariation);
        float skillVariation = UnityEngine.Random.Range(-maxSkillVariation, maxSkillVariation);
        
        // 临时调整AI参数
        ai.aiData.reactionTime = Mathf.Max(0.1f, profile.adjustedReactionTime + reactionVariation);
        ai.aiData.defenseSuccessRate = Mathf.Clamp01(profile.adjustedDefenseSuccessRate + skillVariation);
        ai.aiData.perfectBlockChance = Mathf.Clamp01(profile.adjustedPerfectBlockChance + skillVariation * 0.5f);
    }
    
    #endregion
    
    #region 战斗结果记录
    
    public void RecordCombatResult(bool isPlayerVictory, float playerHealthRemaining, float aiHealthRemaining, float battleDuration)
    {
        CombatResult result = new CombatResult
        {
            isPlayerVictory = isPlayerVictory,
            playerHealthRemaining = playerHealthRemaining,
            aiHealthRemaining = aiHealthRemaining,
            battleDuration = battleDuration,
            timestamp = Time.time
        };
        
        recentResults.Add(result);
        
        // 保持评估窗口大小
        while (recentResults.Count > evaluationWindow)
        {
            recentResults.RemoveAt(0);
        }
        
        if (showDebugInfo)
        {
            string winner = isPlayerVictory ? "玩家" : "AI";
            Debug.Log($"[战斗结果] 胜者: {winner}, 持续时间: {battleDuration:F1}秒");
        }
    }
    
    #endregion
    
    #region 难度预设
    
    public void ApplyDifficultyPreset(AIController ai, DifficultyPreset preset)
    {
        if (!aiProfiles.ContainsKey(ai)) return;
        
        DifficultyProfile profile = aiProfiles[ai];
        
        switch (preset)
        {
            case DifficultyPreset.训练模式:
                profile.SetTrainingMode();
                break;
                
            case DifficultyPreset.挑战模式:
                profile.SetChallengeMode();
                break;
                
            case DifficultyPreset.竞技模式:
                profile.SetCompetitiveMode();
                break;
                
            case DifficultyPreset.娱乐模式:
                profile.SetCasualMode();
                break;
        }
        
        ApplyDifficultyToAI(ai, profile);
        
        if (showDebugInfo)
        {
            Debug.Log($"[难度管理] AI {ai.name} 应用预设: {preset}");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    public DifficultyProfile GetAIDifficultyProfile(AIController ai)
    {
        return aiProfiles.ContainsKey(ai) ? aiProfiles[ai] : null;
    }
    
    public float GetCurrentPlayerWinRate()
    {
        return CalculatePlayerWinRate();
    }
    
    public List<CombatResult> GetRecentResults()
    {
        return new List<CombatResult>(recentResults);
    }
    
    public void ClearCombatHistory()
    {
        recentResults.Clear();
    }
    
    #endregion
    
    #region 调试信息
    
    public string GetDebugInfo()
    {
        string info = $"=== AI难度管理器 ===\n";
        info += $"全局难度: {globalDifficulty}\n";
        info += $"自适应难度: {adaptiveDifficulty}\n";
        info += $"玩家胜率: {CalculatePlayerWinRate():F2}\n";
        info += $"注册AI数量: {aiProfiles.Count}\n";
        info += $"战斗记录: {recentResults.Count}/{evaluationWindow}\n";
        
        return info;
    }
    
    #endregion
}

/// <summary>
/// 难度配置文件
/// </summary>
[System.Serializable]
public class DifficultyProfile
{
    // 基础参数
    public float baseReactionTime;
    public float baseAttackFrequency;
    public float baseDefenseSuccessRate;
    public float basePerfectBlockChance;
    public float baseCounterAttackChance;
    public float baseSpecialSkillChance;
    public float baseComboChance;
    
    // 调整后参数
    public float adjustedReactionTime;
    public float adjustedAttackFrequency;
    public float adjustedDefenseSuccessRate;
    public float adjustedPerfectBlockChance;
    public float adjustedCounterAttackChance;
    public float adjustedSpecialSkillChance;
    public float adjustedComboChance;
    
    // 修正因子
    public float difficultyModifier = 1f;
    
    public void Initialize(AIData aiData, AIDifficulty difficulty)
    {
        // 保存基础值
        baseReactionTime = aiData.reactionTime;
        baseAttackFrequency = aiData.attackFrequency;
        baseDefenseSuccessRate = aiData.defenseSuccessRate;
        basePerfectBlockChance = aiData.perfectBlockChance;
        baseCounterAttackChance = aiData.counterAttackChance;
        baseSpecialSkillChance = aiData.specialSkillChance;
        baseComboChance = aiData.comboChance;
        
        // 根据难度更新
        UpdateForDifficulty(difficulty);
    }
    
    public void UpdateForDifficulty(AIDifficulty difficulty)
    {
        float modifier = GetDifficultyModifier(difficulty);
        ApplyModifier(modifier);
    }
    
    private float GetDifficultyModifier(AIDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AIDifficulty.简单: return 0.6f;
            case AIDifficulty.普通: return 1f;
            case AIDifficulty.困难: return 1.4f;
            case AIDifficulty.专家: return 1.8f;
            default: return 1f;
        }
    }
    
    public void ApplyModifier(float modifier)
    {
        difficultyModifier = modifier;
        
        // 反应时间：难度越高越快
        adjustedReactionTime = baseReactionTime / modifier;
        
        // 攻击频率：难度越高越快
        adjustedAttackFrequency = baseAttackFrequency * modifier;
        
        // 防御成功率：难度越高越高
        adjustedDefenseSuccessRate = baseDefenseSuccessRate * modifier;
        
        // 特殊技能：难度越高使用越频繁
        adjustedPerfectBlockChance = basePerfectBlockChance * modifier;
        adjustedCounterAttackChance = baseCounterAttackChance * modifier;
        adjustedSpecialSkillChance = baseSpecialSkillChance * modifier;
        adjustedComboChance = baseComboChance * modifier;
        
        // 限制在合理范围内
        ClampValues();
    }
    
    public void ApplyAdaptiveAdjustment(float adjustment)
    {
        float newModifier = difficultyModifier + adjustment;
        ApplyModifier(Mathf.Clamp(newModifier, 0.3f, 2.5f));
    }
    
    private void ClampValues()
    {
        adjustedReactionTime = Mathf.Clamp(adjustedReactionTime, 0.05f, 3f);
        adjustedAttackFrequency = Mathf.Clamp(adjustedAttackFrequency, 0.1f, 5f);
        adjustedDefenseSuccessRate = Mathf.Clamp01(adjustedDefenseSuccessRate);
        adjustedPerfectBlockChance = Mathf.Clamp01(adjustedPerfectBlockChance);
        adjustedCounterAttackChance = Mathf.Clamp01(adjustedCounterAttackChance);
        adjustedSpecialSkillChance = Mathf.Clamp01(adjustedSpecialSkillChance);
        adjustedComboChance = Mathf.Clamp01(adjustedComboChance);
    }
    
    // 难度预设方法
    public void SetTrainingMode()
    {
        ApplyModifier(0.4f);
    }
    
    public void SetCasualMode()
    {
        ApplyModifier(0.8f);
    }
    
    public void SetChallengeMode()
    {
        ApplyModifier(1.6f);
    }
    
    public void SetCompetitiveMode()
    {
        ApplyModifier(2f);
    }
}

/// <summary>
/// 战斗结果记录
/// </summary>
[System.Serializable]
public class CombatResult
{
    public bool isPlayerVictory;
    public float playerHealthRemaining;
    public float aiHealthRemaining;
    public float battleDuration;
    public float timestamp;
}

/// <summary>
/// 难度预设枚举
/// </summary>
public enum DifficultyPreset
{
    训练模式,    // 很简单，用于学习
    娱乐模式,    // 轻松休闲
    挑战模式,    // 有挑战性
    竞技模式     // 最高难度
}