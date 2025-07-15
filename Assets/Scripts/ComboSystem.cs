using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [System.Serializable]
    public class ComboSequence
    {
        public string comboName;
        public AttackType[] sequence;
        public float timeWindow = 0.5f;
        public float damageMultiplier = 1.5f;
        public AnimationClip comboAnimation;
    }
    
    [Header("连击设置")]
    public ComboSequence[] combos;
    public float defaultComboWindow = 0.7f;
    public float maxComboMultiplier = 2.5f;
    
    [Header("当前状态")]
    public List<AttackType> currentSequence = new List<AttackType>();
    public float comboTimer;
    public float currentMultiplier = 1.0f;
    public int comboCount;
    
    private PlayerCombat playerCombat;
    private AttackDetector attackDetector;
    private Animator animator;
    
    void Start() {
        playerCombat = GetComponent<PlayerCombat>();
        attackDetector = GetComponent<AttackDetector>();
        animator = GetComponent<Animator>();
    }
    
    void Update() {
        if (comboTimer > 0) {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0) {
                ResetCombo();
            }
        }
    }
    
    public void RegisterAttack(AttackType attackType) {
        // 开始或重置连击计时器
        comboTimer = defaultComboWindow;
        
        // 添加到当前序列
        currentSequence.Add(attackType);
        
        // 检查匹配连击
        CheckForCombo();
    }
    
    private void CheckForCombo() {
        foreach (ComboSequence combo in combos) {
            if (currentSequence.Count < combo.sequence.Length) continue;
            
            bool match = true;
            for (int i = 0; i < combo.sequence.Length; i++) {
                int index = currentSequence.Count - combo.sequence.Length + i;
                if (currentSequence[index] != combo.sequence[i]) {
                    match = false;
                    break;
                }
            }
            
            if (match) {
                ExecuteCombo(combo);
                return;
            }
        }
        
        // 增加连击计数
        comboCount++;
        currentMultiplier = Mathf.Min(1.0f + (comboCount * 0.2f), maxComboMultiplier);
        attackDetector.SetComboMultiplier(currentMultiplier);
    }
    
    private void ExecuteCombo(ComboSequence combo) {
        // 应用连击倍率
        currentMultiplier = Mathf.Min(combo.damageMultiplier * currentMultiplier, maxComboMultiplier);
        attackDetector.SetComboMultiplier(currentMultiplier);
        
        // 播放连击动画
        animator.Play(combo.comboAnimation.name);
        
        // 重置序列但保持倍率
        currentSequence.Clear();
        comboCount++;
    }
    
    public void ResetCombo() {
        currentSequence.Clear();
        comboCount = 0;
        currentMultiplier = 1.0f;
        attackDetector.SetComboMultiplier(1.0f);
        comboTimer = 0;
    }
}