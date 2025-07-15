using UnityEngine;

public class AttackDetector : MonoBehaviour
{
    [Header("攻击参数")]
    public Transform attackPoint; // 拖入AttackPoint对象
    public float attackRadius = 0.3f;
    public LayerMask enemyLayer; // 设置为敌人所在图层
    public int lightDamage = 10;
    public int heavyDamage = 20;
    public int kickDamage = 15;
    public float comboMultiplier = 1.0f; // 连击倍率
    
    private void OnDrawGizmosSelected() {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
    
   public void DetectHit(AttackType attackType) {
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
        attackPoint.position, 
        attackRadius, 
        enemyLayer
    );
    
    foreach (Collider2D enemy in hitEnemies) {
        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null) {
            int damage = CalculateDamage(attackType);
            enemyHealth.TakeDamage(damage);
            
            // 使用 GameAssets 创建伤害弹出
            if (GameAssets.Instance != null && GameAssets.Instance.damagePopupPrefab != null) {
                DamagePopup.Create(enemy.transform.position, damage);
            }
        }
    }
}
    
    private int CalculateDamage(AttackType attackType) {
        int baseDamage = attackType switch {
            AttackType.Light => lightDamage,
            AttackType.Heavy => heavyDamage,
            AttackType.Kick => kickDamage,
            _ => lightDamage
        };
        
        return Mathf.RoundToInt(baseDamage * comboMultiplier);
    }
    
    public void SetComboMultiplier(float multiplier) {
        comboMultiplier = multiplier;
    }
}

public enum AttackType { Light, Heavy, Kick }