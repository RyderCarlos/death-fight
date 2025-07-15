using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public Slider slider;
    public Image fill;
    public Gradient gradient;
    
    public void SetMaxStamina(float stamina) {
        slider.maxValue = stamina;
        slider.value = stamina;
        fill.color = gradient.Evaluate(1f);
    }
    
    public void SetStamina(float stamina) {
        slider.value = stamina;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}