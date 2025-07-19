using UnityEngine;
using System.Collections;
using System;

public class DefenseSystem : MonoBehaviour
{
    [Header("防御设置")]
    public float blockDamageReduction = 0.5f;
    public float perfectBlockWindow = 0.1f;
    public float perfectBlockReduction = 0.8f;
    
    [Header("闪避设置")]
    public float dodgeInvincibilityTime = 0.3f;
    public float dodgeCooldown = 1f;
    public float dodgeDistance = 2f;
    
    [Header("反击设置")]
    public float counterAttackWindow = 0.5f;
    public float counterAttackDamageBonus = 1.5f;
    
    // 状态
    private bool isBlocking = false;
    private bool canCounterAttack = false;
    private bool isDodging = false;
    private float lastDodgeTime;
    private float lastBlockTime;
    
    // 组件引用
    private HealthSystem healthSystem;
    private EnergySystem energySystem;
    private Rigidbody2D rb2d;
    
    // 事件
    public event Action<DamageInfo> OnBlock;
    public event Action<DamageInfo> OnPerfectBlock;
    public event Action OnDodge;
    public event Action<DamageInfo> OnCounterAttack;
    
    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        energySystem = GetComponent<EnergySystem>();
        rb2d = GetComponent<Rigidbody2D>();
        
        // 订阅伤害事件
        if (healthSystem != null)
        {
            healthSystem.OnTakeDamage += ProcessIncomingDamage;
        }
    }
    
    public bool TryDefend()
    {
        return TryBlock();
    }
    
    public bool TryBlock()
    {
        if (isDodging) return false;
        
        isBlocking = true;
        lastBlockTime = Time.time;
        canCounterAttack = true;
        
        // 开始反击窗口计时
        StartCoroutine(CounterAttackWindowCoroutine());
        
        return true;
    }
    
    public bool TryDodge()
    {
        if (Time.time - lastDodgeTime < dodgeCooldown) return false;
        if (isDodging) return false;
        
        lastDodgeTime = Time.time;
        StartCoroutine(DodgeCoroutine());
        OnDodge?.Invoke();
        
        return true;
    }
    
    public void StopBlocking()
    {
        isBlocking = false;
    }
    
    private void ProcessIncomingDamage(DamageInfo damageInfo)
    {
        if (isDodging)
        {
            // 闪避中完全免疫伤害
            damageInfo.finalDamage = 0;
            return;
        }
        
        if (isBlocking)
        {
            float blockReduction = blockDamageReduction;
            
            // 检查完美格挡
            if (Time.time - lastBlockTime <= perfectBlockWindow)
            {
                blockReduction = perfectBlockReduction;
                OnPerfectBlock?.Invoke(damageInfo);
            }
            else
            {
                OnBlock?.Invoke(damageInfo);
            }
            
            // 应用格挡减伤
            damageInfo.finalDamage *= (1f - blockReduction);
            damageInfo.isBlocked = true;
            
            // 格挡获得能量
            if (energySystem != null)
            {
                energySystem.GainEnergy(8f);
            }
        }
    }
    
    public bool TryCounterAttack()
    {
        if (!canCounterAttack) return false;
        
        // 反击逻辑
        AttackSystem attackSystem = GetComponent<AttackSystem>();
        if (attackSystem != null)
        {
            // 创建反击伤害信息
            DamageInfo counterDamage = new DamageInfo();
            counterDamage.damage = 15f * counterAttackDamageBonus;
            counterDamage.attacker = gameObject;
            counterDamage.CalculateFinalDamage();
            
            OnCounterAttack?.Invoke(counterDamage);
            canCounterAttack = false;
            
            return true;
        }
        
        return false;
    }
    
    private IEnumerator DodgeCoroutine()
    {
        isDodging = true;
        
        // 执行闪避移动
        if (rb2d != null)
        {
            Vector2 dodgeDirection = GetDodgeDirection();
            rb2d.AddForce(dodgeDirection * dodgeDistance, ForceMode2D.Impulse);
        }
        
        // 无敌时间
        yield return new WaitForSeconds(dodgeInvincibilityTime);
        
        isDodging = false;
    }
    
    private Vector2 GetDodgeDirection()
    {
        // 简单的后退闪避
        return -transform.right;
    }
    
    private IEnumerator CounterAttackWindowCoroutine()
    {
        yield return new WaitForSeconds(counterAttackWindow);
        canCounterAttack = false;
    }
    
    public bool IsBlocking()
    {
        return isBlocking;
    }
    
    public bool IsDodging()
    {
        return isDodging;
    }
    
    public bool CanCounterAttack()
    {
        return canCounterAttack;
    }
    
    // Additional methods for input controllers
    public bool TryStartBlock()
    {
        isBlocking = true;
        lastBlockTime = Time.time;
        return true;
    }
    
    public void StopBlock()
    {
        isBlocking = false;
    }
    
    public bool TryStartDodge()
    {
        return TryDodge();
    }
    
    public bool TryCounterAttack(DamageInfo damageInfo)
    {
        return TryCounterAttack();
    }
    
    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnTakeDamage -= ProcessIncomingDamage;
        }
    }
}