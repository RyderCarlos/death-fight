using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Health UI")]
    public HealthBarUI playerHealthBar;
    public HealthBarUI enemyHealthBar;
    
    [Header("HUD Elements")]
    public Text scoreText;
    public Text timerText;
    public Text enemiesRemainingText;
    public Slider staminaBar;
    
    [Header("Game Info")]
    public GameObject controlsPanel;
    public Text controlsText;
    public Button toggleControlsButton;
    
    [Header("Debug Info")]
    public GameObject debugPanel;
    public Text debugText;
    public bool showDebugInfo = true;
    
    private HealthSystem playerHealth;
    private DefenseSystem playerDefense;
    private GameStateManager gameStateManager;
    private float gameStartTime;
    private int score = 0;
    
    private void Start()
    {
        gameStartTime = Time.time;
        FindComponents();
        SetupUI();
        SetupControls();
    }
    
    private void Update()
    {
        UpdateHUD();
        UpdateDebugInfo();
        HandleUIInput();
    }
    
    private void FindComponents()
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
                playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            }
        }
        
        gameStateManager = FindObjectOfType<GameStateManager>();
    }
    
    private void SetupUI()
    {
        if (playerHealthBar != null && playerHealth != null)
        {
            playerHealthBar.UpdateHealth(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
        
        if (staminaBar != null)
        {
            staminaBar.maxValue = 1f;
            staminaBar.value = 1f;
        }
        
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
        
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugInfo);
        }
        
        if (toggleControlsButton != null)
        {
            toggleControlsButton.onClick.AddListener(ToggleControlsPanel);
        }
        
        UpdateScore(0);
    }
    
    private void SetupControls()
    {
        if (controlsText != null)
        {
            controlsText.text = 
                "=== CONTROLS ===\n\n" +
                "MOVEMENT:\n" +
                "WASD / Arrow Keys - Move\n" +
                "Space - Jump\n" +
                "S/Down - Crouch\n\n" +
                "COMBAT:\n" +
                "J / Left Click - Light Attack\n" +
                "K / Right Click - Heavy Attack\n" +
                "Shift - Block\n" +
                "Ctrl+Space - Dodge\n\n" +
                "GAME:\n" +
                "ESC - Pause/Resume\n" +
                "R - Restart (when game over)\n" +
                "Tab - Toggle Controls";
        }
    }
    
    private void UpdateHUD()
    {
        UpdateTimer();
        UpdateStamina();
        UpdateEnemiesRemaining();
    }
    
    private void UpdateTimer()
    {
        if (timerText != null && gameStateManager != null && gameStateManager.IsGameActive())
        {
            float gameTime = Time.time - gameStartTime;
            int minutes = Mathf.FloorToInt(gameTime / 60);
            int seconds = Mathf.FloorToInt(gameTime % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
    
    private void UpdateStamina()
    {
        if (staminaBar != null && playerDefense != null)
        {
            staminaBar.value = playerDefense.GetStaminaPercentage();
        }
    }
    
    private void UpdateEnemiesRemaining()
    {
        if (enemiesRemainingText != null && gameStateManager != null)
        {
            float progress = gameStateManager.GetGameProgress();
            int totalEnemies = FindObjectsOfType<EnemyAI>().Length;
            int remainingEnemies = Mathf.RoundToInt(totalEnemies * (1f - progress));
            enemiesRemainingText.text = $"Enemies: {remainingEnemies}";
        }
    }
    
    private void UpdateDebugInfo()
    {
        if (debugText != null && showDebugInfo)
        {
            string debugInfo = "=== DEBUG INFO ===\n";
            
            if (playerHealth != null)
            {
                debugInfo += $"Player Health: {playerHealth.GetCurrentHealth():F1}/{playerHealth.GetMaxHealth()}\n";
            }
            
            if (playerDefense != null)
            {
                debugInfo += $"Stamina: {playerDefense.GetStaminaPercentage():P0}\n";
                debugInfo += $"Blocking: {playerDefense.IsBlocking()}\n";
                debugInfo += $"Dodging: {playerDefense.IsDodging()}\n";
            }
            
            if (gameStateManager != null)
            {
                debugInfo += $"Game State: {gameStateManager.GetCurrentState()}\n";
                debugInfo += $"Progress: {gameStateManager.GetGameProgress():P0}\n";
            }
            
            debugInfo += $"FPS: {1f / Time.unscaledDeltaTime:F0}\n";
            debugInfo += $"Score: {score}";
            
            debugText.text = debugInfo;
        }
    }
    
    private void HandleUIInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleControlsPanel();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugPanel();
        }
    }
    
    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.UpdateHealth(currentHealth, maxHealth);
            
            if (currentHealth < maxHealth * 0.3f)
            {
                playerHealthBar.FlashDamage();
            }
        }
    }
    
    public void UpdateScore(int newScore)
    {
        score = newScore;
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    public void AddScore(int points)
    {
        UpdateScore(score + points);
    }
    
    public void ToggleControlsPanel()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(!controlsPanel.activeSelf);
        }
    }
    
    public void ToggleDebugPanel()
    {
        showDebugInfo = !showDebugInfo;
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugInfo);
        }
    }
    
    public void ShowMessage(string message, float duration = 3f)
    {
        StartCoroutine(ShowMessageCoroutine(message, duration));
    }
    
    private System.Collections.IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        GameObject messageObj = new GameObject("Message");
        messageObj.transform.SetParent(transform);
        
        Text messageText = messageObj.AddComponent<Text>();
        messageText.text = message;
        messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        messageText.fontSize = 24;
        messageText.color = Color.yellow;
        messageText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rect = messageObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, 100);
        rect.sizeDelta = new Vector2(400, 50);
        
        yield return new WaitForSeconds(duration);
        
        Destroy(messageObj);
    }
}