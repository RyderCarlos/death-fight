  using UnityEngine;
  using System.Collections.Generic;
  using System.Collections;

  public class HealthManager : MonoBehaviour
  {
      [Header("管理设置")]
      [Tooltip("管理的所有HealthSystem")]
      public List<HealthSystem> managedHealthSystems = new List<HealthSystem>();
      [Tooltip("自动查找HealthSystem")]
      public bool autoFindHealthSystems = true;
      [Tooltip("查找标签")]
      public string[] searchTags = { "Player", "Enemy" };

      [Header("全局设置")]
      [Tooltip("全局伤害倍数")]
      [Range(0.1f, 5f)]
      public float globalDamageMultiplier = 1f;
      [Tooltip("全局治疗倍数")]
      [Range(0.1f, 5f)]
      public float globalHealMultiplier = 1f;
      [Tooltip("启用友伤")]
      public bool enableFriendlyFire = false;

      [Header("统计信息")]
      [SerializeField] private int totalDamageDealt = 0;
      [SerializeField] private int totalHealingDone = 0;
      [SerializeField] private int totalDeaths = 0;
      [SerializeField] private int totalRevives = 0;

      // 事件系统
      public System.Action<HealthSystem, DamageInfo> OnAnyDamage;
      public System.Action<HealthSystem, int> OnAnyHeal;
      public System.Action<HealthSystem> OnAnyDeath;
      public System.Action<HealthSystem, int> OnAnyRevive;

      // 单例模式
      public static HealthManager Instance { get; private set; }

      void Awake()
      {
          // 单例模式设置
          if (Instance == null)
          {
              Instance = this;
              DontDestroyOnLoad(gameObject);
          }
          else
          {
              Destroy(gameObject);
              return;
          }
      }

      void Start()
      {
          if (autoFindHealthSystems)
          {
              FindAllHealthSystems();
          }

          RegisterHealthSystems();
      }

      /// <summary>
      /// 查找所有HealthSystem
      /// </summary>
      void FindAllHealthSystems()
      {
          managedHealthSystems.Clear();

          // 通过标签查找
          foreach (string tag in searchTags)
          {
              GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
              foreach (GameObject obj in taggedObjects)
              {
                  HealthSystem healthSystem = obj.GetComponent<HealthSystem>();
                  if (healthSystem != null && !managedHealthSystems.Contains(healthSystem))
                  {
                      managedHealthSystems.Add(healthSystem);
                  }
              }
          }

          // 查找所有HealthSystem组件
          HealthSystem[] allHealthSystems = FindObjectsOfType<HealthSystem>();
          foreach (HealthSystem healthSystem in allHealthSystems)
          {
              if (!managedHealthSystems.Contains(healthSystem))
              {
                  managedHealthSystems.Add(healthSystem);
              }
          }

          Debug.Log($"HealthManager 找到 {managedHealthSystems.Count} 个HealthSystem");
      }

      /// <summary>
      /// 注册HealthSystem事件
      /// </summary>
      void RegisterHealthSystems()
      {
          foreach (HealthSystem healthSystem in managedHealthSystems)
          {
              if (healthSystem != null)
              {
                  RegisterHealthSystem(healthSystem);
              }
          }
      }

      /// <summary>
      /// 注册单个HealthSystem
      /// </summary>
      public void RegisterHealthSystem(HealthSystem healthSystem)
      {
          if (healthSystem == null) return;

          // 取消之前的注册（防止重复注册）
          UnregisterHealthSystem(healthSystem);

          // 注册事件
          healthSystem.OnTakeDamage += (damageInfo) => HandleDamage(healthSystem, damageInfo);
          healthSystem.OnHeal += (amount) => HandleHeal(healthSystem, amount);
          healthSystem.OnDeath += () => HandleDeath(healthSystem);
          healthSystem.OnRevive += (health) => HandleRevive(healthSystem, health);

          // 添加到管理列表
          if (!managedHealthSystems.Contains(healthSystem))
          {
              managedHealthSystems.Add(healthSystem);
          }

          Debug.Log($"注册HealthSystem：{healthSystem.gameObject.name}");
      }

      /// <summary>
      /// 取消注册HealthSystem
      /// </summary>
      public void UnregisterHealthSystem(HealthSystem healthSystem)
      {
          if (healthSystem == null) return;

          // 这里应该取消事件注册，但由于C#事件的限制，我们只能移除列表
          managedHealthSystems.Remove(healthSystem);

          Debug.Log($"取消注册HealthSystem：{healthSystem.gameObject.name}");
      }

      /// <summary>
      /// 处理伤害事件
      /// </summary>
      void HandleDamage(HealthSystem target, DamageInfo damageInfo)
      {
          // 应用全局伤害倍数
          damageInfo.finalDamage *= globalDamageMultiplier;

          // 友伤检查
          if (!enableFriendlyFire)
          {
              if (AreFriendly(target.gameObject, damageInfo.attacker))
              {
                  Debug.Log("友军伤害被阻止");
                  return;
              }
          }

          totalDamageDealt += Mathf.RoundToInt(damageInfo.finalDamage);

          OnAnyDamage?.Invoke(target, damageInfo);

          Debug.Log($"HealthManager: {damageInfo.attacker.name} 对 {target.gameObject.name} 造成 {damageInfo.finalDamage} 伤害");
      }

      /// <summary>
      /// 处理治疗事件
      /// </summary>
      void HandleHeal(HealthSystem target, int amount)
      {
          int finalAmount = Mathf.RoundToInt(amount * globalHealMultiplier);
          totalHealingDone += finalAmount;

          OnAnyHeal?.Invoke(target, finalAmount);

          Debug.Log($"HealthManager: {target.gameObject.name} 恢复了 {finalAmount} 血量");
      }

      /// <summary>
      /// 处理死亡事件
      /// </summary>
      void HandleDeath(HealthSystem target)
      {
          totalDeaths++;

          OnAnyDeath?.Invoke(target);

          Debug.Log($"HealthManager: {target.gameObject.name} 死亡");

          // 可以在这里添加死亡后的处理逻辑
          HandlePostDeath(target);
      }

      /// <summary>
      /// 处理复活事件
      /// </summary>
      void HandleRevive(HealthSystem target, int health)
      {
          totalRevives++;

          OnAnyRevive?.Invoke(target, health);

          Debug.Log($"HealthManager: {target.gameObject.name} 复活，血量：{health}");
      }

      /// <summary>
      /// 检查是否为友军
      /// </summary>
      bool AreFriendly(GameObject obj1, GameObject obj2)
      {
          // 简单的友军检查：相同标签为友军
          return obj1.tag == obj2.tag;
      }

      /// <summary>
      /// 死亡后处理
      /// </summary>
      void HandlePostDeath(HealthSystem healthSystem)
      {
          // 可以添加掉落道具、经验值等逻辑
          DropHealthPickup(healthSystem.transform.position);
      }

      /// <summary>
      /// 掉落血量道具
      /// </summary>
      void DropHealthPickup(Vector3 position)
      {
          // 这里需要预先设置的血量道具预制体
          GameObject healthPickupPrefab = Resources.Load<GameObject>("HealthPickup");
          if (healthPickupPrefab != null)
          {
              Instantiate(healthPickupPrefab, position, Quaternion.identity);
          }
      }

      /// <summary>
      /// 全局治疗所有目标
      /// </summary>
      public void HealAll(int amount, string targetTag = "")
      {
          foreach (HealthSystem healthSystem in managedHealthSystems)
          {
              if (healthSystem != null && healthSystem.IsAlive)
              {
                  if (string.IsNullOrEmpty(targetTag) || healthSystem.gameObject.tag == targetTag)
                  {
                      healthSystem.Heal(amount);
                  }
              }
          }
      }

      /// <summary>
      /// 全局对所有目标造成伤害
      /// </summary>
      public void DamageAll(int damage, GameObject attacker, string targetTag = "")
      {
          DamageInfo damageInfo = new DamageInfo
          {
              damage = damage,
              attacker = attacker,
              hitPosition = Vector3.zero
          };

          foreach (HealthSystem healthSystem in managedHealthSystems)
          {
              if (healthSystem != null && healthSystem.IsAlive)
              {
                  if (string.IsNullOrEmpty(targetTag) || healthSystem.gameObject.tag == targetTag)
                  {
                      healthSystem.TakeDamage(damageInfo);
                  }
              }
          }
      }

      /// <summary>
      /// 复活所有死亡的目标
      /// </summary>
      public void ReviveAll(int health = -1, string targetTag = "")
      {
          foreach (HealthSystem healthSystem in managedHealthSystems)
          {
              if (healthSystem != null && healthSystem.IsDead)
              {
                  if (string.IsNullOrEmpty(targetTag) || healthSystem.gameObject.tag == targetTag)
                  {
                      healthSystem.Revive(health);
                  }
              }
          }
      }

      /// <summary>
      /// 获取统计信息
      /// </summary>
      public HealthManagerStats GetStats()
      {
          return new HealthManagerStats
          {
              totalDamageDealt = totalDamageDealt,
              totalHealingDone = totalHealingDone,
              totalDeaths = totalDeaths,
              totalRevives = totalRevives,
              activeHealthSystems = managedHealthSystems.Count
          };
      }

      /// <summary>
      /// 重置统计信息
      /// </summary>
      public void ResetStats()
      {
          totalDamageDealt = 0;
          totalHealingDone = 0;
          totalDeaths = 0;
          totalRevives = 0;
      }

      /// <summary>
      /// 清理无效的HealthSystem引用
      /// </summary>
      public void CleanupNullReferences()
      {
          managedHealthSystems.RemoveAll(item => item == null);
      }

      void Update()
      {
          // 定期清理空引用
          if (Time.frameCount % 300 == 0) // 每5秒清理一次
          {
              CleanupNullReferences();
          }
      }
  }

  /// <summary>
  /// HealthManager统计信息
  /// </summary>
  [System.Serializable]
  public struct HealthManagerStats
  {
      public int totalDamageDealt;
      public int totalHealingDone;
      public int totalDeaths;
      public int totalRevives;
      public int activeHealthSystems;
  }