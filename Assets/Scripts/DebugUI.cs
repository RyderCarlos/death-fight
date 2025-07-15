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
            debugText.text = $"Speed: {playerController.rb.velocity.x:F1}\n" +
                           $"Grounded: {playerController.isGrounded}\n" +
                           $"Velocity Y: {playerController.rb.velocity.y:F1}";
        }
    }
}