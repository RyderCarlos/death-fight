using UnityEngine;

public class CombatHUD : MonoBehaviour
{
    [Header("UI组件")]
    public HealthBar healthBar;
    public EnergyBar energyBar;
    public ComboDisplay comboDisplay;
    
    [Header("目标对象")]
    public GameObject targetPlayer;
    
    void Start()
    {
        if (targetPlayer != null)
        {
            // 自动连接各个系统
            HealthSystem healthSystem = targetPlayer.GetComponent<HealthSystem>();
            EnergySystem energySystem = targetPlayer.GetComponent<EnergySystem>();
            ComboSystem comboSystem = targetPlayer.GetComponent<ComboSystem>();
            
            // 初始化UI组件
            if (healthBar != null && healthSystem != null)
            {
                healthBar.Initialize(healthSystem);
            }
            
            if (energyBar != null && energySystem != null)
            {
                energyBar.Initialize(energySystem);
            }
            
            if (comboDisplay != null && comboSystem != null)
            {
                comboDisplay.Initialize(comboSystem);
            }
        }
    }
}