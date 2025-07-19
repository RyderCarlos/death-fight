using UnityEngine;
using System;

public class EnergySystem : MonoBehaviour
{
    [Header("能量设置")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyRegenRate = 5f;
    public bool autoRegeneration = true;
    
    [Header("特殊技能")]
    public float specialSkillThreshold = 50f;
    public float specialSkillCooldown = 10f;
    private float lastSpecialSkillTime;
    
    // 事件
    public event Action<int, int> OnEnergyChanged;
    public event Action<bool> OnSpecialSkillAvailable;
    public event Action OnEnergyFull;
    public event Action OnEnergyEmpty;
    
    void Start()
    {
        currentEnergy = maxEnergy;
    }
    
    void Update()
    {
        if (autoRegeneration && currentEnergy < maxEnergy)
        {
            GainEnergy(energyRegenRate * Time.deltaTime);
        }
    }
    
    public void GainEnergy(float amount)
    {
        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        
        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(Mathf.RoundToInt(currentEnergy), Mathf.RoundToInt(maxEnergy));
            CheckSpecialSkillAvailability();
            
            if (currentEnergy >= maxEnergy)
                OnEnergyFull?.Invoke();
        }
    }
    
    public void ConsumeEnergy(float amount)
    {
        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
        
        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(Mathf.RoundToInt(currentEnergy), Mathf.RoundToInt(maxEnergy));
            CheckSpecialSkillAvailability();
            
            if (currentEnergy <= 0)
                OnEnergyEmpty?.Invoke();
        }
    }
    
    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }
    
    public bool CanUseSpecialSkill()
    {
        return currentEnergy >= specialSkillThreshold && 
               Time.time - lastSpecialSkillTime >= specialSkillCooldown;
    }
    
    public bool UseSpecialSkill(float energyCost)
    {
        if (CanUseSpecialSkill() && currentEnergy >= energyCost)
        {
            ConsumeEnergy(energyCost);
            lastSpecialSkillTime = Time.time;
            return true;
        }
        return false;
    }
    
    public void SetEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(Mathf.RoundToInt(currentEnergy), Mathf.RoundToInt(maxEnergy));
        CheckSpecialSkillAvailability();
    }
    
    public float GetEnergyPercentage()
    {
        return currentEnergy / maxEnergy;
    }
    
    private void CheckSpecialSkillAvailability()
    {
        bool wasAvailable = currentEnergy >= specialSkillThreshold;
        OnSpecialSkillAvailable?.Invoke(wasAvailable);
    }
}