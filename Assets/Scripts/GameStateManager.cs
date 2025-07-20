using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    Playing,
    PlayerWon,
    PlayerLost,
    Paused
}

public class GameStateManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.Playing;
    
    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject victoryUI;
    public GameObject pauseUI;
    public Text gameOverText;
    public Text victoryText;
    public Button restartButton;
    public Button mainMenuButton;
    public Button resumeButton;
    
    [Header("Settings")]
    public float gameOverDelay = 2f;
    public bool autoRestartAfterVictory = false;
    public float autoRestartDelay = 5f;
    
    private HealthSystem playerHealth;
    private List<HealthSystem> enemyHealthSystems = new List<HealthSystem>();
    private int totalEnemies;
    private int deadEnemies;
    
    public static GameStateManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        FindGameObjects();
        SetupUI();
        SubscribeToEvents();
        
        currentState = GameState.Playing;
        Time.timeScale = 1f;
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void FindGameObjects()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>()?.gameObject;
        }
        
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
        }
        
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in enemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealthSystems.Add(enemyHealth);
            }
        }
        
        totalEnemies = enemyHealthSystems.Count;
        deadEnemies = 0;
        
        Debug.Log($"Found {totalEnemies} enemies and player: {(playerHealth != null ? "Yes" : "No")}");
    }
    
    private void SetupUI()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (pauseUI != null) pauseUI.SetActive(false);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);
            
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
    }
    
    private void SubscribeToEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath += OnPlayerDeath;
        }
        
        foreach (HealthSystem enemyHealth in enemyHealthSystems)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
        }
    }
    
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R) && (currentState == GameState.PlayerLost || currentState == GameState.PlayerWon))
        {
            RestartGame();
        }
    }
    
    private void OnPlayerDeath()
    {
        if (currentState != GameState.Playing) return;
        
        Debug.Log("Player died - Game Over!");
        StartCoroutine(HandleGameOver());
    }
    
    private void OnEnemyDeath()
    {
        if (currentState != GameState.Playing) return;
        
        deadEnemies++;
        Debug.Log($"Enemy died! {deadEnemies}/{totalEnemies} enemies defeated");
        
        if (deadEnemies >= totalEnemies)
        {
            Debug.Log("All enemies defeated - Victory!");
            StartCoroutine(HandleVictory());
        }
    }
    
    private IEnumerator HandleGameOver()
    {
        currentState = GameState.PlayerLost;
        
        yield return new WaitForSeconds(gameOverDelay);
        
        Time.timeScale = 0f;
        
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        
        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER\n\nPress R to Restart\nPress ESC for Menu";
        }
    }
    
    private IEnumerator HandleVictory()
    {
        currentState = GameState.PlayerWon;
        
        yield return new WaitForSeconds(1f);
        
        if (victoryUI != null)
        {
            victoryUI.SetActive(true);
        }
        
        if (victoryText != null)
        {
            victoryText.text = "VICTORY!\n\nAll enemies defeated!\n\nPress R to Restart\nPress ESC for Menu";
        }
        
        if (autoRestartAfterVictory)
        {
            yield return new WaitForSeconds(autoRestartDelay);
            RestartGame();
        }
    }
    
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            
            if (pauseUI != null)
            {
                pauseUI.SetActive(true);
            }
        }
    }
    
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            
            if (pauseUI != null)
            {
                pauseUI.SetActive(false);
            }
        }
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
    
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsGameActive()
    {
        return currentState == GameState.Playing;
    }
    
    public float GetGameProgress()
    {
        if (totalEnemies == 0) return 1f;
        return (float)deadEnemies / totalEnemies;
    }
}