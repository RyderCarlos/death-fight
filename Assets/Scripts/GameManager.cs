using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthBar;
    public Slider staminaBar;
    public Text healthText;
    public Text controlsText;
    
    [Header("New UI Components")]
    public GameUI gameUI;
    public GameStateManager gameStateManager;
    public HealthBarUI playerHealthBarUI;
    
    [Header("Game Settings")]
    public bool showDebugInfo = true;
    public int pointsPerEnemyKill = 100;
    
    private HealthSystem playerHealth;
    private DefenseSystem playerDefense;
    private int currentScore = 0;
    
    private void Start()
    {
        FindPlayerComponents();
        SetupUI();
        ShowControls();
        SetupNewSystems();
        SubscribeToEnemyEvents();
    }
    
    private void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>()?.gameObject;
        }
        
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            playerDefense = player.GetComponent<DefenseSystem>();
            
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
                playerHealth.OnDeath += OnPlayerDeath;
                
                if (playerHealthBarUI != null)
                {
                    playerHealth.OnHealthChanged += playerHealthBarUI.UpdateHealth;
                }
            }
        }
    }
    
    private void SetupUI()
    {
        if (healthBar != null && playerHealth != null)
        {
            healthBar.maxValue = playerHealth.GetMaxHealth();
            healthBar.value = playerHealth.GetCurrentHealth();
        }
        
        if (staminaBar != null)
        {
            staminaBar.maxValue = 1f;
            staminaBar.value = 1f;
        }
    }
    
    private void Update()
    {
        UpdateStaminaUI();
        
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }
    
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    private void UpdateStaminaUI()
    {
        if (staminaBar != null && playerDefense != null)
        {
            staminaBar.value = playerDefense.GetStaminaPercentage();
        }
    }
    
    private void OnPlayerDeath()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0.5f;
        
        Invoke(nameof(RestartGame), 3f);
    }
    
    private void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    private void ShowControls()
    {
        if (controlsText != null)
        {
            controlsText.text = "Controls:\n" +
                               "WASD/Arrow Keys - Move\n" +
                               "Space - Jump\n" +
                               "S/Down - Crouch\n" +
                               "J/Left Click - Light Attack\n" +
                               "K/Right Click - Heavy Attack\n" +
                               "Shift - Block\n" +
                               "Ctrl+Space - Dodge";
        }
    }
    
    private void DrawDebugInfo()
    {
        if (playerHealth != null)
        {
            Vector3 playerPos = playerHealth.transform.position;
            
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 200, 20), $"Health: {playerHealth.GetCurrentHealth():F1}/{playerHealth.GetMaxHealth()}");
            
            if (playerDefense != null)
            {
                GUI.Label(new Rect(10, 30, 200, 20), $"Stamina: {playerDefense.GetStaminaPercentage():P0}");
                GUI.Label(new Rect(10, 50, 200, 20), $"Blocking: {playerDefense.IsBlocking()}");
                GUI.Label(new Rect(10, 70, 200, 20), $"Dodging: {playerDefense.IsDodging()}");
            }
        }
    }
    
    private void OnGUI()
    {
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }
    
    private void SetupNewSystems()
    {
        if (gameStateManager == null)
        {
            gameStateManager = FindObjectOfType<GameStateManager>();
        }
        
        if (gameUI == null)
        {
            gameUI = FindObjectOfType<GameUI>();
        }
        
        if (gameUI != null)
        {
            gameUI.UpdateScore(currentScore);
        }
    }
    
    private void SubscribeToEnemyEvents()
    {
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in enemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath += OnEnemyDeath;
            }
        }
    }
    
    private void OnEnemyDeath()
    {
        currentScore += pointsPerEnemyKill;
        
        if (gameUI != null)
        {
            gameUI.AddScore(pointsPerEnemyKill);
            gameUI.ShowMessage($"+{pointsPerEnemyKill} Points!", 2f);
        }
        
        Debug.Log($"Enemy defeated! Score: {currentScore}");
    }
    
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public void ResetScore()
    {
        currentScore = 0;
        if (gameUI != null)
        {
            gameUI.UpdateScore(currentScore);
        }
    }
}