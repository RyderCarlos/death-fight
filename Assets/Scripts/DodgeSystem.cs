using UnityEngine;
using System.Collections; 
public class DodgeSystem : MonoBehaviour
{
    [Header("闪避参数")]
    public KeyCode dodgeKey = KeyCode.LeftShift;
    public float dodgeDistance = 2f;
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 1.5f;
    public float dodgeStaminaCost = 25f;
    public float invulnerableDuration = 0.4f;
    
    [Header("状态")]
    public bool isDodging;
    public bool canDodge = true;
    
    private Rigidbody2D rb;
    private Stamina stamina;
    private float dodgeDirection;
    private float dodgeEndTime;   // 闪避结束时间
    private float nextDodgeTime;  // 下次可闪避时间  
private Animator animator;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stamina = GetComponent<Stamina>();
        animator = GetComponent<Animator>();
    }
    
    void Update() {
        if (Input.GetKeyDown(dodgeKey) && canDodge && stamina.HasStamina(dodgeStaminaCost)) {
            StartDodge();
        }
    }
    
    private void StartDodge() {
        isDodging = true;
        canDodge = false;
        
        // 消耗耐力
        stamina.UseStamina(dodgeStaminaCost);
        
        // 确定闪避方向（根据角色朝向）
        dodgeDirection = transform.localScale.x > 0 ? 1 : -1;
        
        // 播放闪避动画
        animator.SetTrigger("Dodge");
        
        // 开始闪避移动
        StartCoroutine(PerformDodge());
        
        // 设置无敌状态
        StartCoroutine(InvulnerablePeriod());
        
        // 冷却计时
        Invoke("ResetDodge", dodgeCooldown);
    }
    
    private IEnumerator PerformDodge() {
        float timer = 0f;
        Vector2 startPos = rb.position;
        Vector2 endPos = startPos + new Vector2(dodgeDirection * dodgeDistance, 0);
        
        while (timer < dodgeDuration) {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / dodgeDuration);
            rb.MovePosition(Vector2.Lerp(startPos, endPos, progress));
            yield return null;
        }
        
        isDodging = false;
    }
    
    private IEnumerator InvulnerablePeriod() {
        gameObject.layer = LayerMask.NameToLayer("Invulnerable");
        yield return new WaitForSeconds(invulnerableDuration);
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    
    private void ResetDodge() {
        canDodge = true;
    }
}