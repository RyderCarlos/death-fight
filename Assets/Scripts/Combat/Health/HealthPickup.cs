  using UnityEngine;

  public class HealthPickup : MonoBehaviour
  {
      [Header("恢复设置")]
      [Tooltip("恢复血量")]
      public int healAmount = 25;
      [Tooltip("恢复类型")]
      public HealType healType = HealType.固定数值;
      [Tooltip("百分比恢复数值（0-1）")]
      [Range(0f, 1f)]
      public float healPercentage = 0.25f;

      [Header("拾取设置")]
      [Tooltip("自动拾取")]
      public bool autoPickup = true;
      [Tooltip("拾取距离")]
      public float pickupRange = 1f;
      [Tooltip("目标图层")]
      public LayerMask targetLayers = 1;

      [Header("视觉效果")]
      [Tooltip("拾取特效")]
      public GameObject pickupEffect;
      [Tooltip("拾取音效")]
      public AudioClip pickupSound;
      [Tooltip("漂浮动画")]
      public bool enableFloating = true;
      [Tooltip("漂浮高度")]
      public float floatHeight = 0.5f;
      [Tooltip("漂浮速度")]
      public float floatSpeed = 2f;

      [Header("生命周期")]
      [Tooltip("自动销毁时间（0为不销毁）")]
      public float lifetime = 0f;

      private Vector3 startPosition;
      private AudioSource audioSource;
      private bool isPickedUp = false;

      public enum HealType
      {
          固定数值,
          最大血量百分比,
          完全恢复
      }

      void Start()
      {
          startPosition = transform.position;
          audioSource = GetComponent<AudioSource>();

          // 自动销毁
          if (lifetime > 0)
          {
              Destroy(gameObject, lifetime);
          }
      }

      void Update()
      {
          HandleFloating();

          if (autoPickup)
          {
              CheckForPickup();
          }
      }

      /// <summary>
      /// 处理漂浮动画
      /// </summary>
      void HandleFloating()
      {
          if (!enableFloating) return;

          float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
          transform.position = new Vector3(transform.position.x, newY, transform.position.z);
      }

      /// <summary>
      /// 检查拾取
      /// </summary>
      void CheckForPickup()
      {
          if (isPickedUp) return;

          Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, targetLayers);

          foreach (var collider in colliders)
          {
              HealthSystem healthSystem = collider.GetComponent<HealthSystem>();
              if (healthSystem != null && healthSystem.IsAlive)
              {
                  PickupBy(healthSystem);
                  break;
              }
          }
      }

      /// <summary>
      /// 被拾取
      /// </summary>
      public void PickupBy(HealthSystem healthSystem)
      {
          if (isPickedUp || healthSystem == null || healthSystem.IsDead) return;

          isPickedUp = true;

          // 计算恢复量
          int actualHealAmount = CalculateHealAmount(healthSystem);

          // 只有在能够恢复血量时才拾取
          if (healthSystem.CurrentHealth >= healthSystem.MaxHealth)
          {
              Debug.Log($"{healthSystem.gameObject.name} 血量已满，无法拾取恢复道具");
              isPickedUp = false;
              return;
          }

          // 恢复血量
          healthSystem.Heal(actualHealAmount);

          // 播放特效
          PlayPickupEffect();

          // 播放音效
          PlayPickupSound();

          Debug.Log($"{healthSystem.gameObject.name} 拾取了恢复道具，恢复 {actualHealAmount} 点血量");

          // 销毁道具
          Destroy(gameObject);
      }

      /// <summary>
      /// 计算恢复量
      /// </summary>
      int CalculateHealAmount(HealthSystem healthSystem)
      {
          switch (healType)
          {
              case HealType.固定数值:
                  return healAmount;

              case HealType.最大血量百分比:
                  return Mathf.RoundToInt(healthSystem.MaxHealth * healPercentage);

              case HealType.完全恢复:
                  return healthSystem.MaxHealth - healthSystem.CurrentHealth;

              default:
                  return healAmount;
          }
      }

      /// <summary>
      /// 播放拾取特效
      /// </summary>
      void PlayPickupEffect()
      {
          if (pickupEffect != null)
          {
              GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
              Destroy(effect, 3f);
          }
      }

      /// <summary>
      /// 播放拾取音效
      /// </summary>
      void PlayPickupSound()
      {
          if (pickupSound != null)
          {
              if (audioSource != null)
              {
                  audioSource.PlayOneShot(pickupSound);
              }
              else
              {
                  AudioSource.PlayClipAtPoint(pickupSound, transform.position);
              }
          }
      }

      /// <summary>
      /// 手动触发拾取
      /// </summary>
      void OnTriggerEnter2D(Collider2D other)
      {
          if (!autoPickup) return;

          if (((1 << other.gameObject.layer) & targetLayers) != 0)
          {
              HealthSystem healthSystem = other.GetComponent<HealthSystem>();
              if (healthSystem != null)
              {
                  PickupBy(healthSystem);
              }
          }
      }

      /// <summary>
      /// 绘制拾取范围
      /// </summary>
      void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.green;
          Gizmos.DrawWireSphere(transform.position, pickupRange);
      }
  }