using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AttackSystem : MonoBehaviour
{
    [Header("攻击设置")]
    public AttackData[] availableAttacks;
    public float globalCooldown = 0.1f;
    public bool canCancelAttack = false;
    
    [Header("组件引用")]
    public Transform attackPoint;
    
    // 内部状态
    private bool isAttacking = false;
    private bool canAttack = true;
    private Queue<AttackData> attackQueue = new Queue<AttackData>();
    private AttackData currentAttack;
    private Coroutine currentAttackCoroutine;
    
    // 组件引用
    private Animator animator;
    private EnergySystem energySystem;
    private ComboSystem comboSystem;
    
    // 事件
    public event Action<AttackData> OnAttackStart;
    public event Action<AttackData> OnAttackEnd;
    public event Action<GameObject, AttackData> OnHitTarget;
    public event Action<AttackData> OnAttackCancel;
    
    // 统计
    [Header("统计信息")]
    public int totalAttacks = 0;
    public int successfulHits = 0;
    
    void Start()
    {
        // 获取组件
        animator = GetComponent<Animator>();
        energySystem = GetComponent<EnergySystem>();
        comboSystem = GetComponent<ComboSystem>();
        
        // 自动创建攻击点
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(transform);
            attackPointObj.transform.localPosition = Vector3.right;
            attackPoint = attackPointObj.transform;
        }
    }
    
    void Update()
    {
        ProcessAttackQueue();
    }
    
    public bool TryAttack(AttackType attackType)
    {
        AttackData attackData = GetAttackData(attackType);
        if (attackData == null) return false;
        
        // 检查是否可以攻击
        if (!CanPerformAttack(attackData)) return false;
        
        // 添加到攻击队列
        attackQueue.Enqueue(attackData);
        return true;
    }
    
    bool CanPerformAttack(AttackData attackData)
    {
        // 能量检查
        if (energySystem != null && energySystem.GetCurrentEnergy() < attackData.energyCost)
        {
            Debug.Log("能量不足，无法发动攻击");
            return false;
        }
        
        // 冷却检查
        if (!canAttack)
        {
            return false;
        }
        
        return true;
    }
    
    void ProcessAttackQueue()
    {
        if (attackQueue.Count > 0 && !isAttacking)
        {
            AttackData nextAttack = attackQueue.Dequeue();
            ExecuteAttack(nextAttack);
        }
    }
    
    void ExecuteAttack(AttackData attackData)
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        
        currentAttackCoroutine = StartCoroutine(AttackSequence(attackData));
    }
    
    IEnumerator AttackSequence(AttackData attackData)
    {
        isAttacking = true;
        canAttack = false;
        currentAttack = attackData;
        totalAttacks++;
        
        // 消耗能量
        if (energySystem != null)
        {
            energySystem.ConsumeEnergy(attackData.energyCost);
        }
        
        // 触发攻击开始事件
        OnAttackStart?.Invoke(attackData);
        
        // 播放动画
        if (animator != null && !string.IsNullOrEmpty(attackData.animationTrigger))
        {
            animator.SetTrigger(attackData.animationTrigger);
        }
        
        // 播放音效
        if (attackData.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackData.attackSound, transform.position);
        }
        
        // 前摇阶段
        yield return new WaitForSeconds(attackData.startupTime);
        
        // 判定阶段
        float activeTimer = 0f;
        bool hasHit = false;
        
        while (activeTimer < attackData.activeTime)
        {
            // 执行攻击检测
            if (!hasHit) // 防止一次攻击多次命中同一目标
            {
                hasHit = PerformAttackDetection(attackData);
            }
            
            activeTimer += Time.deltaTime;
            yield return null;
        }
        
        // 后摇阶段
        yield return new WaitForSeconds(attackData.recoveryTime);
        
        // 攻击结束
        OnAttackEnd?.Invoke(attackData);
        isAttacking = false;
        currentAttack = null;
        
        // 全局冷却
        yield return new WaitForSeconds(globalCooldown);
        canAttack = true;
    }
    
    bool PerformAttackDetection(AttackData attackData)
    {
        Collider2D[] hits;
        
        if (attackData.useCircleDetection)
        {
            hits = Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackData.attackRange
            );
        }
        else
        {
            hits = Physics2D.OverlapBoxAll(
                attackPoint.position,
                new Vector2(attackData.attackRange, attackData.attackWidth),
                0f
            );
        }
        
        bool hitTarget = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject) // 不攻击自己
            {
                HealthSystem targetHealth = hit.GetComponent<HealthSystem>();
                if (targetHealth != null)
                {
                    // 创建伤害信息
                    DamageInfo damageInfo = CreateDamageInfo(attackData);
                    
                    // 应用伤害
                    targetHealth.TakeDamage(damageInfo);
                    
                    // 触发命中事件
                    OnHitTarget?.Invoke(hit.gameObject, attackData);
                    
                    // 获得能量
                    if (energySystem != null)
                    {
                        energySystem.GainEnergy(attackData.energyGain);
                    }
                    
                    // 更新连击
                    if (comboSystem != null)
                    {
                        comboSystem.ExtendCombo();
                    }
                    
                    successfulHits++;
                    hitTarget = true;
                    
                    // 播放命中音效
                    if (attackData.hitSound != null)
                    {
                        AudioSource.PlayClipAtPoint(attackData.hitSound, hit.transform.position);
                    }
                }
            }
        }
        
        return hitTarget;
    }
    
    DamageInfo CreateDamageInfo(AttackData attackData)
    {
        DamageInfo damageInfo = new DamageInfo();
        damageInfo.damage = attackData.damage;
        damageInfo.attacker = gameObject;
        damageInfo.attackType = attackData.attackType;
        damageInfo.attackName = attackData.name;
        damageInfo.knockbackForce = attackData.knockbackForce;
        damageInfo.knockbackDirection = transform.right;
        
        // 应用连击倍数
        if (comboSystem != null)
        {
            damageInfo.damageMultiplier = comboSystem.GetDamageMultiplier();
        }
        
        // 暴击判断（10%概率）
        if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
        {
            damageInfo.isCritical = true;
        }
        
        damageInfo.CalculateFinalDamage();
        return damageInfo;
    }
    
    AttackData GetAttackData(AttackType attackType)
    {
        foreach (AttackData data in availableAttacks)
        {
            if (data.attackType == attackType)
            {
                return data;
            }
        }
        return null;
    }
    
    public void CancelCurrentAttack()
    {
        if (isAttacking && canCancelAttack && currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            OnAttackCancel?.Invoke(currentAttack);
            isAttacking = false;
            canAttack = true;
            currentAttack = null;
        }
    }
    
    // 调试绘制
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null && currentAttack != null)
        {
            Gizmos.color = Color.red;
            if (currentAttack.useCircleDetection)
            {
                Gizmos.DrawWireSphere(attackPoint.position, currentAttack.attackRange);
            }
            else
            {
                Gizmos.DrawWireCube(
                    attackPoint.position,
                    new Vector3(currentAttack.attackRange, currentAttack.attackWidth, 0)
                );
            }
        }
    }
    
    // Public method for checking if character can attack
    public bool CanAttack()
    {
        return canAttack && !isAttacking;
    }
}