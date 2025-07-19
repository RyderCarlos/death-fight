using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// AI检测系统 - 负责目标检测、追踪和感知
/// </summary>
public class AIDetectionSystem : MonoBehaviour
{
    [Header("检测设置")]
    public float detectionRange = 8f;
    public float loseTargetRange = 12f;
    public float viewAngle = 120f;
    public LayerMask targetLayers = 1;
    public LayerMask obstacleLayer = 1;
    
    [Header("感知设置")]
    public bool enableHearing = true;
    public float hearingRange = 6f;
    public bool enableSmell = false;
    public float smellRange = 3f;
    
    [Header("记忆系统")]
    public bool enableMemory = true;
    public float memoryDuration = 5f;
    public float searchTime = 3f;
    
    [Header("调试")]
    public bool showDebugGizmos = true;
    public Color detectionColor = Color.yellow;
    public Color viewColor = Color.green;
    public Color hearingColor = Color.blue;
    
    // 检测状态
    private Transform currentTarget;
    private List<Transform> detectedTargets = new List<Transform>();
    private List<MemoryEntry> targetMemories = new List<MemoryEntry>();
    
    // 组件引用
    private AIController aiController;
    
    // 事件
    public System.Action<Transform> OnTargetDetected;
    public System.Action OnTargetLost;
    public System.Action<Vector3> OnSuspiciousSoundHeard;
    public System.Action<Vector3> OnLastKnownPositionReached;
    
    void Start()
    {
        aiController = GetComponent<AIController>();
    }
    
    void Update()
    {
        UpdateDetection();
        UpdateMemorySystem();
    }
    
    #region 主检测逻辑
    
    private void UpdateDetection()
    {
        // 清空之前的检测结果
        detectedTargets.Clear();
        
        // 视觉检测
        DetectByVision();
        
        // 听觉检测
        if (enableHearing)
        {
            DetectByHearing();
        }
        
        // 嗅觉检测
        if (enableSmell)
        {
            DetectBySmell();
        }
        
        // 选择最佳目标
        SelectBestTarget();
    }
    
    private void DetectByVision()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRange, targetLayers);
        
        foreach (Collider2D collider in colliders)
        {
            Transform target = collider.transform;
            if (target == transform) continue; // 不检测自己
            
            Vector2 directionToTarget = (target.position - transform.position).normalized;
            float angleToTarget = Vector2.Angle(transform.right, directionToTarget);
            
            // 检查是否在视野角度内
            if (angleToTarget <= viewAngle / 2f)
            {
                // 检查是否有障碍物遮挡
                if (CanSeeTarget(target))
                {
                    AddDetectedTarget(target, DetectionType.Vision);
                }
            }
        }
    }
    
    private void DetectByHearing()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, hearingRange, targetLayers);
        
        foreach (Collider2D collider in colliders)
        {
            Transform target = collider.transform;
            if (target == transform) continue;
            
            // 检查目标是否在移动（产生声音）
            PlayerController playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
                if (targetRb != null && targetRb.velocity.magnitude > 1f)
                {
                    AddDetectedTarget(target, DetectionType.Hearing);
                    OnSuspiciousSoundHeard?.Invoke(target.position);
                }
            }
        }
    }
    
    private void DetectBySmell()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, smellRange, targetLayers);
        
        foreach (Collider2D collider in colliders)
        {
            Transform target = collider.transform;
            if (target == transform) continue;
            
            // 嗅觉检测不受障碍物影响
            AddDetectedTarget(target, DetectionType.Smell);
        }
    }
    
    private bool CanSeeTarget(Transform target)
    {
        Vector2 directionToTarget = target.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            directionToTarget.normalized, 
            directionToTarget.magnitude,
            obstacleLayer
        );
        
        // 如果没有碰撞或碰撞的是目标本身，则可以看到
        return hit.collider == null || hit.collider.transform == target;
    }
    
    #endregion
    
    #region 目标管理
    
    private void AddDetectedTarget(Transform target, DetectionType detectionType)
    {
        if (!detectedTargets.Contains(target))
        {
            detectedTargets.Add(target);
            
            // 更新记忆
            UpdateMemory(target, detectionType);
        }
    }
    
    private void SelectBestTarget()
    {
        Transform bestTarget = null;
        float bestScore = 0f;
        
        foreach (Transform target in detectedTargets)
        {
            float score = CalculateTargetScore(target);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }
        
        // 检查是否需要更新当前目标
        if (bestTarget != currentTarget)
        {
            if (currentTarget != null)
            {
                OnTargetLost?.Invoke();
            }
            
            currentTarget = bestTarget;
            
            if (currentTarget != null)
            {
                OnTargetDetected?.Invoke(currentTarget);
                
                // 更新AI控制器的目标
                if (aiController != null)
                {
                    aiController.target = currentTarget;
                }
            }
        }
        
        // 检查是否丢失目标
        if (currentTarget != null && !detectedTargets.Contains(currentTarget))
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
            if (distanceToTarget > loseTargetRange)
            {
                LoseCurrentTarget();
            }
        }
    }
    
    private float CalculateTargetScore(Transform target)
    {
        float score = 0f;
        
        // 距离因子（距离越近分数越高）
        float distance = Vector2.Distance(transform.position, target.position);
        float distanceScore = Mathf.Clamp01(1f - (distance / detectionRange));
        score += distanceScore * 3f;
        
        // 威胁评估
        HealthSystem targetHealth = target.GetComponent<HealthSystem>();
        if (targetHealth != null)
        {
            float healthPercentage = targetHealth.GetHealthPercentage();
            score += (1f - healthPercentage) * 2f; // 血量越低分数越高（优先攻击残血目标）
        }
        
        // 视线清晰度
        if (CanSeeTarget(target))
        {
            score += 2f;
        }
        
        // 角度因子（正前方的目标分数更高）
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector2.Angle(transform.right, directionToTarget);
        float angleScore = Mathf.Clamp01(1f - (angle / 180f));
        score += angleScore * 1f;
        
        return score;
    }
    
    private void LoseCurrentTarget()
    {
        if (currentTarget != null)
        {
            // 记录最后已知位置
            if (enableMemory)
            {
                AddToMemory(currentTarget, currentTarget.position, DetectionType.LastKnown);
            }
            
            OnTargetLost?.Invoke();
            currentTarget = null;
            
            if (aiController != null)
            {
                aiController.target = null;
            }
        }
    }
    
    #endregion
    
    #region 记忆系统
    
    private void UpdateMemorySystem()
    {
        if (!enableMemory) return;
        
        // 清理过期的记忆
        for (int i = targetMemories.Count - 1; i >= 0; i--)
        {
            if (Time.time - targetMemories[i].timestamp > memoryDuration)
            {
                targetMemories.RemoveAt(i);
            }
        }
        
        // 如果没有当前目标，尝试搜索记忆中的位置
        if (currentTarget == null && targetMemories.Count > 0)
        {
            SearchMemoryLocations();
        }
    }
    
    private void UpdateMemory(Transform target, DetectionType detectionType)
    {
        AddToMemory(target, target.position, detectionType);
    }
    
    private void AddToMemory(Transform target, Vector3 position, DetectionType detectionType)
    {
        // 查找是否已有该目标的记忆
        MemoryEntry existingEntry = targetMemories.Find(m => m.target == target);
        
        if (existingEntry != null)
        {
            // 更新现有记忆
            existingEntry.lastKnownPosition = position;
            existingEntry.timestamp = Time.time;
            existingEntry.detectionType = detectionType;
            existingEntry.confidence = Mathf.Min(1f, existingEntry.confidence + 0.1f);
        }
        else
        {
            // 创建新记忆
            MemoryEntry newEntry = new MemoryEntry
            {
                target = target,
                lastKnownPosition = position,
                timestamp = Time.time,
                detectionType = detectionType,
                confidence = 0.8f
            };
            targetMemories.Add(newEntry);
        }
    }
    
    private void SearchMemoryLocations()
    {
        // 选择最新的记忆位置进行搜索
        MemoryEntry latestMemory = null;
        float latestTime = 0f;
        
        foreach (MemoryEntry memory in targetMemories)
        {
            if (memory.timestamp > latestTime && memory.confidence > 0.3f)
            {
                latestTime = memory.timestamp;
                latestMemory = memory;
            }
        }
        
        if (latestMemory != null)
        {
            // 移动到记忆位置
            float distanceToMemory = Vector2.Distance(transform.position, latestMemory.lastKnownPosition);
            
            if (distanceToMemory > 0.5f)
            {
                // 移动到记忆位置
                if (aiController != null)
                {
                    aiController.MoveToPosition(latestMemory.lastKnownPosition);
                }
            }
            else
            {
                // 到达记忆位置
                OnLastKnownPositionReached?.Invoke(latestMemory.lastKnownPosition);
                
                // 降低记忆置信度
                latestMemory.confidence -= 0.3f;
                
                if (latestMemory.confidence <= 0f)
                {
                    targetMemories.Remove(latestMemory);
                }
            }
        }
    }
    
    #endregion
    
    #region 公共接口
    
    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
    
    public List<Transform> GetDetectedTargets()
    {
        return new List<Transform>(detectedTargets);
    }
    
    public bool HasTarget()
    {
        return currentTarget != null;
    }
    
    public Vector3? GetLastKnownPosition(Transform target)
    {
        MemoryEntry memory = targetMemories.Find(m => m.target == target);
        return memory?.lastKnownPosition;
    }
    
    public void ForgetTarget(Transform target)
    {
        targetMemories.RemoveAll(m => m.target == target);
        
        if (currentTarget == target)
        {
            LoseCurrentTarget();
        }
    }
    
    public void ClearAllMemories()
    {
        targetMemories.Clear();
    }
    
    #endregion
    
    #region 调试绘制
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 绘制检测范围
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 绘制丢失目标范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
        
        // 绘制视野
        Gizmos.color = viewColor;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * transform.right * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * transform.right * detectionRange;
        
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        
        // 绘制听觉范围
        if (enableHearing)
        {
            Gizmos.color = hearingColor;
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }
        
        // 绘制嗅觉范围
        if (enableSmell)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, smellRange);
        }
        
        // 绘制记忆位置
        Gizmos.color = Color.cyan;
        foreach (MemoryEntry memory in targetMemories)
        {
            Gizmos.DrawWireSphere(memory.lastKnownPosition, 0.5f * memory.confidence);
        }
        
        // 绘制到当前目标的连线
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
    
    #endregion
}

/// <summary>
/// 检测类型枚举
/// </summary>
public enum DetectionType
{
    Vision,     // 视觉
    Hearing,    // 听觉
    Smell,      // 嗅觉
    LastKnown   // 最后已知位置
}

/// <summary>
/// 记忆条目
/// </summary>
[System.Serializable]
public class MemoryEntry
{
    public Transform target;
    public Vector3 lastKnownPosition;
    public float timestamp;
    public DetectionType detectionType;
    public float confidence; // 置信度 0-1
}