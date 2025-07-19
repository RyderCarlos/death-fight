using UnityEngine;
using TMPro;

public class DebugUI : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public PlayerController playerController;
    
    void Update()
    {
        if (debugText != null && playerController != null)
        {
            debugText.text = $"Speed: {playerController.rb2d.velocity.x:F1}\n" +
                           $"Grounded: {playerController.isGrounded}\n" +
                           $"Velocity Y: {playerController.rb2d.velocity.y:F1}";
        }
    }
}