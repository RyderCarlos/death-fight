// EnergySystem.cs
using UnityEngine;
using System.Collections;

public class EnergySystem : MonoBehaviour
{
    [Header("能量设置")]
    public int maxEnergy = 100;
    public int currentEnergy = 0;
    public float energyRegenRate = 5f;     // 每秒恢复的能量
    public float energyRegenDelay = 2f;    // 使用技能后多久开始恢复能量
    
    [Header("能量获得设置")]
    public int energyOnHit = 10;           // 攻击命中获得的能量
    public int energyOnReceiveDamage = 15; // 受到伤害获得的能量
    public int energyOnBlock = 8;          // 格挡成功获得的能量
    
    [Header("特殊技能设置")]
    public int specialSkillThreshold = 50; // 特殊技能所需最低能量
    public bool canUseSpecialSkills = true;
    
    private bool canRegenerate = true;
    private Coroutine regenCoroutine;
    private Coroutine regenDelayCoroutine;
    
    // 事件
    public System.Action<int, int> OnEnergyChanged; // current, max
    public System.Action OnEnergyFull;
    public System.Action OnEnergyEmpty;
    public System.Action<bool> OnSpecialSkillAvailable; // 特殊技能可用状态变化
    
    void Start()
    {
        currentEnergy = 0;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        
        StartEnergyRegeneration();
        
        // 订阅其他系统的事件
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.OnTakeDamage += OnTakeDamage;
        }
        
        DefenseSystem defense = GetComponent<DefenseSystem>();
        if (defense != null)
        {
            defense.OnBlock += OnBlock;
        }
        
        AttackSystem attack = GetComponent<AttackSystem>();
        if (attack != null)
        {
            attack.OnAttackStart += OnAttackStart;
        }
    }
    
    void StartEnergyRegeneration()
    {
        if (regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(EnergyRegenerationCoroutine());
        }
    }
    
    IEnumerator EnergyRegenerationCoroutine()
    {
        while (true)
        {
            if (canRegenerate && currentEnergy < maxEnergy)
            {
                GainEnergy(Mathf.RoundToInt(energyRegenRate * Time.deltaTime));
            }
            yield return null;
        }
    }
    
    public void GainEnergy(int amount)
    {
        if (amount <= 0) return;
        
        int oldEnergy = currentEnergy;
        bool wasSpecialAvailableBefore = CanUseSpecialSkill();
        
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        
        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            
            if (currentEnergy >= maxEnergy)
            {
                OnEnergyFull?.Invoke();
            }
            
            // 检查特殊技能可用性变化
            bool isSpecialAvailableNow = CanUseSpecialSkill();
            if (wasSpecialAvailableBefore != isSpecialAvailableNow)
            {
                OnSpecialSkillAvailable?.Invoke(isSpecialAvailableNow);
            }
        }
    }
    
    public bool ConsumeEnergy(int amount)
    {
        if (amount <= 0) return true;
        if (currentEnergy < amount) return false;
        
        int oldEnergy = currentEnergy;
        bool wasSpecialAvailableBefore = CanUseSpecialSkill();
        
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
        
        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            
            if (currentEnergy <= 0)
            {
                OnEnergyEmpty?.Invoke();
            }
            
            // 检查特殊技能可用性变化
            bool isSpecialAvailableNow = CanUseSpecialSkill();
            if (wasSpecialAvailableBefore != isSpecialAvailableNow)
            {
                OnSpecialSkillAvailable?.Invoke(isSpecialAvailableNow);
            }
        }
        
        // 暂停能量恢复
        StopEnergyRegeneration();
        
        return true;
    }
    
    void StopEnergyRegeneration()
    {
        canRegenerate = false;
        
        if (regenDelayCoroutine != null)
        {
            StopCoroutine(regenDelayCoroutine);
        }
        
        regenDelayCoroutine = StartCoroutine(EnergyRegenDelayCoroutine());
    }
    
    IEnumerator EnergyRegenDelayCoroutine()
    {
        yield return new WaitForSeconds(energyRegenDelay);
        canRegenerate = true;
    }
    
    void OnTakeDamage(DamageInfo damageInfo)
    {
        if (!damageInfo.isBlocked)
        {
            GainEnergy(energyOnReceiveDamage);
        }
    }
    
    void OnBlock(DamageInfo damageInfo)
    {
        GainEnergy(energyOnBlock);
    }
    
    void OnAttackStart(AttackData attackData)
    {
        // 攻击命中的能量在AttackSystem中通过GainEnergy方法获得
    }
    
    public float GetEnergyPercentage()
    {
        return (float)currentEnergy / maxEnergy;
    }
    
    public bool HasEnergy(int amount)
    {
        return currentEnergy >= amount;
    }
    
    public bool CanUseSpecialSkill()
    {
        return canUseSpecialSkills && currentEnergy >= specialSkillThreshold;
    }
    
    public bool TryUseSpecialSkill(int energyCost = 50)
    {
        if (!CanUseSpecialSkill() || currentEnergy < energyCost)
        {
            Debug.Log($"无法使用特殊技能：能量不足 {currentEnergy}/{energyCost}，或特殊技能未启用");
            return false;
        }
        
        return ConsumeEnergy(energyCost);
    }
    
    public void SetSpecialSkillEnabled(bool enabled)
    {
        bool wasAvailable = CanUseSpecialSkill();
        canUseSpecialSkills = enabled;
        bool isAvailableNow = CanUseSpecialSkill();
        
        if (wasAvailable != isAvailableNow)
        {
            OnSpecialSkillAvailable?.Invoke(isAvailableNow);
        }
    }
    
    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public int SpecialSkillThreshold => specialSkillThreshold;
}