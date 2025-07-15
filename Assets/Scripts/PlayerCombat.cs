using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("引用")]
    public Animator animator;
    public AttackDetector attackDetector;

    [Header("攻击输入")]
    public KeyCode lightAttackKey = KeyCode.J;
    public KeyCode heavyAttackKey = KeyCode.K;
    public KeyCode kickKey = KeyCode.L;

    private bool canAttack = true;

    void Update()
    {
        if (!canAttack) return;

        if (Input.GetKeyDown(lightAttackKey))
        {
            StartAttack(AttackType.Light);
        }
        else if (Input.GetKeyDown(heavyAttackKey))
        {
            StartAttack(AttackType.Heavy);
        }
        else if (Input.GetKeyDown(kickKey))
        {
            StartAttack(AttackType.Kick);
        }
    }
private ComboSystem comboSystem;

void Start() {
    comboSystem = GetComponent<ComboSystem>();
}
    private void StartAttack(AttackType type)
    {
        canAttack = false;

        // 触发动画
        string triggerName = type switch
        {
            AttackType.Light => "LightAttack",
            AttackType.Heavy => "HeavyAttack",
            AttackType.Kick => "Kick",
            _ => "LightAttack"
        };

        animator.SetTrigger(triggerName);
         // 注册攻击到连击系统
        comboSystem.RegisterAttack(type);
    }

    // 由动画事件调用
    public void OnAttackAnimationHit(int attackTypeInt)
    {
        AttackType type = (AttackType)attackTypeInt;
        attackDetector.DetectHit(type);
    }

    // 由动画事件调用
    public void OnAttackAnimationEnd()
    {
        canAttack = true;
    }
    
}