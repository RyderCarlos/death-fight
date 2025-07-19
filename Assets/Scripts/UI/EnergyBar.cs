using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyBar : MonoBehaviour
{
    [Header("UI组件")]
    public Image fillImage;
    public TextMeshProUGUI energyText;
    public Image backgroundImage;
    
    [Header("特殊技能指示")]
    public GameObject specialSkillIndicator;
    public Color specialSkillAvailableColor = Color.yellow;
    public Color normalColor = Color.blue;
    
    // 内部状态
    private EnergySystem targetEnergySystem;
    
    public void Initialize(EnergySystem energySystem)
    {
        targetEnergySystem = energySystem;
        
        if (targetEnergySystem != null)
        {
            // 订阅事件
            targetEnergySystem.OnEnergyChanged += OnEnergyChanged;
            targetEnergySystem.OnSpecialSkillAvailable += OnSpecialSkillAvailable;
            
            // 初始化显示
            UpdateDisplay();
        }
    }
    
    void OnEnergyChanged(int currentEnergy, int maxEnergy)
    {
        UpdateDisplay();
    }
    
    void OnSpecialSkillAvailable(bool available)
    {
        if (fillImage != null)
        {
            fillImage.color = available ? specialSkillAvailableColor : normalColor;
        }
        
        if (specialSkillIndicator != null)
        {
            specialSkillIndicator.SetActive(available);
        }
    }
    
    void UpdateDisplay()
    {
        if (targetEnergySystem == null) return;
        
        float energyPercentage = targetEnergySystem.GetEnergyPercentage();
        
        // 更新填充
        if (fillImage != null)
        {
            fillImage.fillAmount = energyPercentage;
        }
        
        // 更新文本
        if (energyText != null)
        {
            energyText.text = $"{targetEnergySystem.GetCurrentEnergy():F0}/{targetEnergySystem.maxEnergy:F0}";
        }
    }
    
    void OnDestroy()
    {
        if (targetEnergySystem != null)
        {
            targetEnergySystem.OnEnergyChanged -= OnEnergyChanged;
            targetEnergySystem.OnSpecialSkillAvailable -= OnSpecialSkillAvailable;
        }
    }
}