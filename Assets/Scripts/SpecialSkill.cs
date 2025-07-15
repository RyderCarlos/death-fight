using UnityEngine;

public class SpecialSkill : MonoBehaviour
{
    [Header("技能设置")]
    public KeyCode skillKey = KeyCode.U;
    public float staminaCost = 50f;
    public float cooldown = 5f;
    public GameObject skillEffect;
    [Header("攻击点")]
    public Transform attackPoint; // 添加攻击点引用

    [Header("状态")]
    public bool canUseSkill = true;

    private Stamina stamina;
    private Animator animator;

    void Start()
    {
        stamina = GetComponent<Stamina>();
        animator = GetComponent<Animator>();
        if (attackPoint == null)
        {
            attackPoint = transform.Find("AttackPoint");
            if (attackPoint == null)
            {
                Debug.LogError("SpecialSkill: AttackPoint not found! Please assign in inspector.");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(skillKey) && canUseSkill)
        {
            TryUseSkill();
        }
    }

    private void TryUseSkill()
    {
        if (stamina.HasStamina(staminaCost))
        {
            stamina.UseStamina(staminaCost);
            canUseSkill = false;

            // 触发技能动画
            animator.SetTrigger("SpecialSkill");

            // 冷却计时
            Invoke("ResetSkill", cooldown);
        }
    }

    // 由动画事件调用


    private void ResetSkill()
    {
        canUseSkill = true;
    }
     public void ActivateSkillEffect() {
        if (skillEffect != null && attackPoint != null) {
            Instantiate(skillEffect, attackPoint.position, Quaternion.identity);
        }
        else if (attackPoint == null) {
            Debug.LogError("SpecialSkill: AttackPoint is null!");
        }
    }
}