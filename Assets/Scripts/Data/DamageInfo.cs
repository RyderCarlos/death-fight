using UnityEngine;

[System.Serializable]
public class DamageInfo
{
    [Header("伤害信息")]
    public float damage = 10f;
    public float damageMultiplier = 1f;
    public bool isCritical = false;
    public bool isBlocked = false;
    public Vector2 knockbackDirection = Vector2.right;
    public float knockbackForce = 1f;
    
    [Header("效果")]
    public bool causesStun = false;
    public float stunDuration = 0f;
    public bool causesKnockdown = false;
    
    [Header("来源信息")]
    public GameObject attacker;
    public AttackType attackType;
    public string attackName;
    
    public float finalDamage { get; set; }
    
    public void CalculateFinalDamage()
    {
        finalDamage = damage * damageMultiplier;
        
        if (isBlocked)
            finalDamage *= 0.5f; // 格挡减伤50%
            
        if (isCritical)
            finalDamage *= 1.5f; // 暴击增伤50%
            
        finalDamage = Mathf.Max(0, finalDamage);
    }
}