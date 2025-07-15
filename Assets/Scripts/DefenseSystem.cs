// 修改后的 DefenseSystem.cs
using UnityEngine;
using System.Collections;

public class DefenseSystem : MonoBehaviour
{
    [Header("格挡参数")]
    public KeyCode blockKey = KeyCode.S;
    public float blockDamageReduction = 0.6f;
    public float blockStaminaCostPerSecond = 10f;
    public float parryWindow = 0.2f;
    public float parryStaminaReward = 15f;
    
    [Header("防御破解")]
    public float blockBreakThreshold = 30f;
    public float blockBreakDuration = 2f;
    
    [Header("特效")]
    public GameObject parryEffect; // 添加招架特效
    
    [Header("状态")]
    [SerializeField] private bool isBlocking;
    [SerializeField] private bool isParrying;
    [SerializeField] private bool canBlock = true;
    
    private Stamina stamina;
    private float blockStartTime;
    private Animator animator;
    
    void Start() {
        stamina = GetComponent<Stamina>();
        animator = GetComponent<Animator>();
    }
    
    void Update() {
        if (canBlock) {
            isBlocking = Input.GetKey(blockKey) && stamina.HasStamina(1f);
            
            if (isBlocking) {
                stamina.UseStamina(blockStaminaCostPerSecond * Time.deltaTime);
                
                if (Time.time - blockStartTime < parryWindow) {
                    isParrying = true;
                } else {
                    isParrying = false;
                }
            } else {
                isParrying = false;
            }
            
            if (Input.GetKeyDown(blockKey)) {
                blockStartTime = Time.time;
            }
        }
        
        animator.SetBool("IsBlocking", isBlocking);
    }
    
    public float ProcessDamage(float incomingDamage) {
        if (isParrying) {
            stamina.AddStamina(parryStaminaReward);
            
            // 生成招架特效
            if (parryEffect != null) {
                Instantiate(parryEffect, transform.position, Quaternion.identity);
            }
            return 0f;
        }
        else if (isBlocking) {
            if (incomingDamage >= blockBreakThreshold) {
                StartCoroutine(BlockBreak());
                return incomingDamage * 0.8f;
            }
            return incomingDamage * (1 - blockDamageReduction);
        }
        return incomingDamage;
    }
    
    private IEnumerator BlockBreak() {
        animator.SetTrigger("BlockBreak");
        isBlocking = false;
        canBlock = false;
        yield return new WaitForSeconds(blockBreakDuration);
        canBlock = true;
    }
}