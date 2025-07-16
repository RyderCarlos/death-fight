  using UnityEngine;
  using System.Collections.Generic;
  using System.Collections;

  public class AttackDetector : MonoBehaviour
  {
      [Header("攻击检测设置")]
      [Tooltip("目标图层遮罩")]
      public LayerMask targetLayerMask = 1;
      [Tooltip("攻击检测点")]
      public Transform attackPoint;
      [Tooltip("是否显示检测范围")]
      public bool showDetectionGizmos = true;

      [Header("检测形状")]
      [Tooltip("检测形状类型")]
      public DetectionShape detectionShape = DetectionShape.圆形;
      [Tooltip("矩形检测大小")]
      public Vector2 boxSize = new Vector2(1f, 0.5f);

      [Header("调试信息")]
      [SerializeField] private bool isDetecting = false;
      [SerializeField] private int currentHitCount = 0;

      private List<Collider2D> hitTargets = new List<Collider2D>();
      private AttackData currentAttackData;

      // 事件系统
      public System.Action<GameObject, AttackData> OnHit;
      public System.Action OnDetectionStart;
      public System.Action OnDetectionEnd;

      public enum DetectionShape
      {
          圆形,
          矩形
      }

      void Start()
      {
          InitializeAttackPoint();
      }

      /// <summary>
      /// 初始化攻击检测点
      /// </summary>
      void InitializeAttackPoint()
      {
          if (attackPoint == null)
          {
              GameObject attackPointObj = new GameObject("AttackPoint");
              attackPointObj.transform.SetParent(transform);
              attackPointObj.transform.localPosition = new Vector3(1f, 0f, 0f);
              attackPoint = attackPointObj.transform;

              Debug.Log($"为 {gameObject.name} 自动创建了攻击检测点");
          }
      }

      /// <summary>
      /// 开始攻击检测
      /// </summary>
      public void StartDetection(AttackData attackData)
      {
          if (attackData == null)
          {
              Debug.LogWarning("攻击数据为空，无法开始检测");
              return;
          }

          currentAttackData = attackData;
          isDetecting = true;
          hitTargets.Clear();
          currentHitCount = 0;

          OnDetectionStart?.Invoke();
          StartCoroutine(DetectionCoroutine(attackData));
      }

      /// <summary>
      /// 检测协程
      /// </summary>
      private IEnumerator DetectionCoroutine(AttackData attackData)
      {
          float detectionTime = 0f;

          while (detectionTime < attackData.activeTime && isDetecting)
          {
              DetectHits(attackData);
              detectionTime += Time.deltaTime;
              yield return null;
          }

          StopDetection();
      }

      /// <summary>
      /// 检测击中目标
      /// </summary>
      void DetectHits(AttackData attackData)
      {
          Collider2D[] colliders = GetCollidersInRange(attackData.range);

          foreach (Collider2D collider in colliders)
          {
              if (IsValidTarget(collider))
              {
                  ProcessHit(collider.gameObject, attackData);
              }
          }
      }

      /// <summary>
      /// 根据形状获取范围内的碰撞体
      /// </summary>
      Collider2D[] GetCollidersInRange(float range)
      {
          Vector2 position = attackPoint.position;

          switch (detectionShape)
          {
              case DetectionShape.圆形:
                  return Physics2D.OverlapCircleAll(position, range, targetLayerMask);

              case DetectionShape.矩形:
                  Vector2 size = boxSize * range;
                  return Physics2D.OverlapBoxAll(position, size, 0f, targetLayerMask);

              default:
                  return Physics2D.OverlapCircleAll(position, range, targetLayerMask);
          }
      }

      /// <summary>
      /// 检查是否为有效目标
      /// </summary>
      bool IsValidTarget(Collider2D collider)
      {
          // 不攻击自己
          if (collider.gameObject == gameObject) return false;

          // 不重复攻击同一目标
          if (hitTargets.Contains(collider)) return false;

          // 可以在这里添加更多条件，比如敌友识别
          return true;
      }

      /// <summary>
      /// 处理击中目标
      /// </summary>
      void ProcessHit(GameObject target, AttackData attackData)
      {
          hitTargets.Add(target.GetComponent<Collider2D>());
          currentHitCount++;

          OnHit?.Invoke(target, attackData);

          Debug.Log($"{gameObject.name} 击中了 {target.name}，当前击中数量：{currentHitCount}");
      }

      /// <summary>
      /// 停止检测
      /// </summary>
      public void StopDetection()
      {
          isDetecting = false;
          OnDetectionEnd?.Invoke();
      }

      /// <summary>
      /// 手动设置攻击点位置
      /// </summary>
      public void SetAttackPointPosition(Vector3 localPosition)
      {
          if (attackPoint != null)
          {
              attackPoint.localPosition = localPosition;
          }
      }

      /// <summary>
      /// 获取当前击中目标数量
      /// </summary>
      public int GetHitCount()
      {
          return currentHitCount;
      }

      /// <summary>
      /// 绘制检测范围
      /// </summary>
      void OnDrawGizmosSelected()
      {
          if (!showDetectionGizmos || attackPoint == null) return;

          Gizmos.color = isDetecting ? Color.red : Color.yellow;

          float range = currentAttackData != null ? currentAttackData.range : 1f;

          switch (detectionShape)
          {
              case DetectionShape.圆形:
                  Gizmos.DrawWireSphere(attackPoint.position, range);
                  break;

              case DetectionShape.矩形:
                  Vector3 size = new Vector3(boxSize.x * range, boxSize.y * range, 0.1f);
                  Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, attackPoint.rotation, Vector3.one);
                  Gizmos.DrawWireCube(Vector3.zero, size);
                  Gizmos.matrix = Matrix4x4.identity;
                  break;
          }

          // 绘制攻击点
          Gizmos.color = Color.blue;
          Gizmos.DrawWireSphere(attackPoint.position, 0.1f);
      }
  }