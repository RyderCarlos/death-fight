using UnityEngine;
using System.Collections.Generic;
using System;

public class ComboSystem : MonoBehaviour
{
    [Header("连击设置")]
    public float comboResetTime = 2f;
    public float damageIncreaseRate = 0.1f;
    public float maxComboMultiplier = 3f;
    public int maxComboCount = 50;
    
    [Header("连击序列")]
    public ComboData[] comboSequences;
    
    // 连击状态
    private int currentCombo = 0;
    private float lastComboTime;
    private List<AttackType> currentSequence = new List<AttackType>();
    
    // 组件引用
    private EnergySystem energySystem;
    
    // 事件
    public event Action<int> OnComboStart;
    public event Action<int, float> OnComboExtend;
    public event Action<int> OnComboEnd;
    public event Action<ComboData, int> OnComboSequenceComplete;
    
    void Start()
    {
        energySystem = GetComponent<EnergySystem>();
    }
    
    void Update()
    {
        // 检查连击重置
        if (Time.time - lastComboTime > comboResetTime && currentCombo > 0)
        {
            ResetCombo();
        }
    }
    
    public void ExtendCombo()
    {
        if (currentCombo == 0)
        {
            OnComboStart?.Invoke(1);
        }
        
        currentCombo++;
        currentCombo = Mathf.Min(currentCombo, maxComboCount);
        lastComboTime = Time.time;
        
        float multiplier = GetDamageMultiplier();
        OnComboExtend?.Invoke(currentCombo, multiplier);
        OnComboComplete?.Invoke(currentCombo, null);
    }
    
    public void ResetCombo()
    {
        if (currentCombo > 0)
        {
            OnComboEnd?.Invoke(currentCombo);
            OnComboReset?.Invoke(currentCombo);
        }
        
        currentCombo = 0;
        currentSequence.Clear();
    }
    
    public float GetDamageMultiplier()
    {
        float multiplier = 1f + (currentCombo * damageIncreaseRate);
        return Mathf.Min(multiplier, maxComboMultiplier);
    }
    
    public int GetCurrentCombo()
    {
        return currentCombo;
    }
    
    public void AddAttackToSequence(AttackType attackType)
    {
        currentSequence.Add(attackType);
        
        // 检查是否完成了某个连击序列
        CheckComboSequences();
        
        // 限制序列长度
        if (currentSequence.Count > 10)
        {
            currentSequence.RemoveAt(0);
        }
    }
    
    private void CheckComboSequences()
    {
        foreach (ComboData combo in comboSequences)
        {
            if (IsSequenceMatch(combo.attackSequence))
            {
                OnComboSequenceComplete?.Invoke(combo, currentCombo);
                // 给予额外奖励
                ExtendCombo();
                
                // 应用连击奖励
                if (energySystem != null)
                {
                    energySystem.GainEnergy(combo.energyBonus);
                }
                break;
            }
        }
    }
    
    private bool IsSequenceMatch(AttackType[] sequence)
    {
        if (currentSequence.Count < sequence.Length) return false;
        
        for (int i = 0; i < sequence.Length; i++)
        {
            int index = currentSequence.Count - sequence.Length + i;
            if (currentSequence[index] != sequence[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Additional methods and events for compatibility
    public event Action<int, ComboData> OnComboComplete;
    public event Action<int> OnComboReset;
    
    public void AddInputToBuffer(AttackType attackType)
    {
        AddAttackToSequence(attackType);
    }
    
    public bool IsInCombo()
    {
        return currentCombo > 0;
    }
    
    public float ComboMultiplier => GetDamageMultiplier();
}

