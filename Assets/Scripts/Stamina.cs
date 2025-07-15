using UnityEngine;
using UnityEngine.Events;

public class Stamina : MonoBehaviour
{
    [Header("能量设置")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenRate = 15f; // 每秒恢复量
    public float regenDelay = 1f; // 上次消耗后多久开始恢复
    
    [Header("事件")]
    public UnityEvent<float> OnStaminaChanged;
    
    private float lastUsedTime;
    
    void Start() {
        currentStamina = maxStamina;
    }
    
    void Update() {
        // 延迟后恢复耐力
        if (Time.time > lastUsedTime + regenDelay) {
            RegenStamina();
        }
    }
    
    public void UseStamina(float amount) {
        currentStamina = Mathf.Max(currentStamina - amount, 0);
        lastUsedTime = Time.time;
        OnStaminaChanged.Invoke(currentStamina / maxStamina);
    }
    
    public void AddStamina(float amount) {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        OnStaminaChanged.Invoke(currentStamina / maxStamina);
    }
    
    private void RegenStamina() {
        if (currentStamina < maxStamina) {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            OnStaminaChanged.Invoke(currentStamina / maxStamina);
        }
    }
    
    public bool HasStamina(float amount) {
        return currentStamina >= amount;
    }
}