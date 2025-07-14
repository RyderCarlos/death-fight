using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("输入键位设置")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.S;
    public KeyCode attackKey = KeyCode.J;
    public KeyCode blockKey = KeyCode.K;
    
    // 输入状态
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool BlockPressed { get; private set; }
    
    // 按键按下瞬间
    public bool JumpDown { get; private set; }
    public bool AttackDown { get; private set; }
    
    void Update()
    {
        // 持续按下
        LeftPressed = Input.GetKey(leftKey);
        RightPressed = Input.GetKey(rightKey);
        JumpPressed = Input.GetKey(jumpKey);
        CrouchPressed = Input.GetKey(crouchKey);
        AttackPressed = Input.GetKey(attackKey);
        BlockPressed = Input.GetKey(blockKey);
        
        // 按下瞬间
        JumpDown = Input.GetKeyDown(jumpKey);
        AttackDown = Input.GetKeyDown(attackKey);
    }
    
    public float GetHorizontalInput()
    {
        float horizontal = 0f;
        if (LeftPressed) horizontal -= 1f;
        if (RightPressed) horizontal += 1f;
        return horizontal;
    }
}