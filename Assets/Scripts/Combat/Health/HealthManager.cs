using UnityEngine;
using System.Collections.Generic;
using System;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }
    
    [Header("全局设置")]
    public bool enableFriendlyFire = false;
    public float globalDamageMultiplier = 1f;
    public float globalHealingMultiplier = 1f;
    
    // 注册的健康系统
    private List<HealthSystem> registeredSystems = new List<HealthSystem>();
    
    // 全局统计
    public float totalDamageDealt = 0f;
    public float totalHealingDone = 0f;
    public int totalDeaths = 0;
    
    // 事件
    public event Action<HealthSystem> OnHealthSystemRegistered;
    public event Action<HealthSystem> OnHealthSystemUnregistered;
    public event Action<float> OnGlobalDamageDealt;
    public event Action<float> OnGlobalHealingDone;
    public event Action OnGlobalDeath;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 自动查找并注册场景中的HealthSystem
        HealthSystem[] healthSystems = FindObjectsOfType<HealthSystem>();
        foreach (HealthSystem system in healthSystems)
        {
            RegisterHealthSystem(system);
        }
    }
    
    public void RegisterHealthSystem(HealthSystem healthSystem)
    {
        if (healthSystem == null || registeredSystems.Contains(healthSystem))
            return;
        
        registeredSystems.Add(healthSystem);
        
        // 订阅事件
        healthSystem.OnTakeDamage += OnSystemTakeDamage;
        healthSystem.OnHeal += OnSystemHeal;
        healthSystem.OnDeath += OnSystemDeath;
        
        OnHealthSystemRegistered?.Invoke(healthSystem);
    }
    
    public void UnregisterHealthSystem(HealthSystem healthSystem)
    {
        if (healthSystem == null || !registeredSystems.Contains(healthSystem))
            return;
        
        registeredSystems.Remove(healthSystem);
        
        // 取消订阅事件
        healthSystem.OnTakeDamage -= OnSystemTakeDamage;
        healthSystem.OnHeal -= OnSystemHeal;
        healthSystem.OnDeath -= OnSystemDeath;
        
        OnHealthSystemUnregistered?.Invoke(healthSystem);
    }
    
    private void OnSystemTakeDamage(DamageInfo damageInfo)
    {
        float damage = damageInfo.finalDamage * globalDamageMultiplier;
        totalDamageDealt += damage;
        OnGlobalDamageDealt?.Invoke(damage);
    }
    
    private void OnSystemHeal(float healAmount)
    {
        float healing = healAmount * globalHealingMultiplier;
        totalHealingDone += healing;
        OnGlobalHealingDone?.Invoke(healing);
    }
    
    private void OnSystemDeath()
    {
        totalDeaths++;
        OnGlobalDeath?.Invoke();
    }
    
    // 全局治疗
    public void HealAll(float amount)
    {
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && system.IsAlive())
            {
                system.Heal(amount);
            }
        }
    }
    
    // 全局伤害
    public void DamageAll(float amount, GameObject source = null)
    {
        DamageInfo damageInfo = new DamageInfo();
        damageInfo.damage = amount;
        damageInfo.attacker = source;
        damageInfo.CalculateFinalDamage();
        
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && system.IsAlive())
            {
                system.TakeDamage(damageInfo);
            }
        }
    }
    
    // 复活所有死亡单位
    public void ReviveAll(float healthPercentage = 1f)
    {
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && !system.IsAlive())
            {
                system.Revive(healthPercentage);
            }
        }
    }
    
    // 获取统计信息
    public int GetTotalRegisteredSystems()
    {
        return registeredSystems.Count;
    }
    
    public int GetAliveCount()
    {
        int count = 0;
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && system.IsAlive())
            {
                count++;
            }
        }
        return count;
    }
    
    public int GetDeadCount()
    {
        int count = 0;
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && !system.IsAlive())
            {
                count++;
            }
        }
        return count;
    }
    
    public List<HealthSystem> GetAllHealthSystems()
    {
        return new List<HealthSystem>(registeredSystems);
    }
    
    public List<HealthSystem> GetAliveHealthSystems()
    {
        List<HealthSystem> aliveSystems = new List<HealthSystem>();
        foreach (HealthSystem system in registeredSystems)
        {
            if (system != null && system.IsAlive())
            {
                aliveSystems.Add(system);
            }
        }
        return aliveSystems;
    }
}