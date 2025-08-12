using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Jugador")]
    [SerializeField] private Transform player;

    [Header("HUD (UI.Text)")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text coinsText;

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject gameCompletedPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Configuración de niveles (monedas objetivo)")]
    [SerializeField] private int[] coinTargets = new int[] { 5, 10, 15 };
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Velocidad por nivel")]
    [SerializeField] private float[] speedMultipliers = new float[] { 1f, 1.2f, 1.4f };
    [Range(0f, 1f)]
    [SerializeField] private float levelSpeedStep = 0.15f;

    public int Score { get; private set; } = 0;
    public int CoinsCollectedThisLevel { get; private set; } = 0;

    public int CurrentTarget => coinTargets[Mathf.Clamp(currentLevelIndex, 0, coinTargets.Length - 1)];
    public int CurrentLevelNumber => currentLevelIndex + 1;

    bool isPlaying = false;
    bool isPlayerDead = false;

    float playerBaseSpeed0 = -1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 0f;
        CacheAndApplyPlayerSpeed();
    }

    void Start()
    {
        SafeSet(menuPanel, true);
        SafeSet(levelCompletePanel, false);
        SafeSet(gameCompletedPanel, false);
        SafeSet(gameOverPanel, false);
        UpdateHUD();
    }

    void SafeSet(GameObject go, bool on) { if (go) go.SetActive(on); }

    public void StartGame()
    {
        isPlaying = true;
        isPlayerDead = false;
        Time.timeScale = 1f;
        CacheAndApplyPlayerSpeed();
        SafeSet(menuPanel, false);
        SafeSet(gameOverPanel, false);
        UpdateHUD();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddCoin(int amount = 1)
    {
        if (isPlayerDead) return;

        Score += amount;
        CoinsCollectedThisLevel += amount;
        UpdateHUD();

        if (CoinsCollectedThisLevel >= CurrentTarget)
            LevelCompleted();
    }

    void LevelCompleted()
    {
        isPlaying = false;
        Time.timeScale = 0f;

        if (currentLevelIndex < coinTargets.Length - 1)
            SafeSet(levelCompletePanel, true);
        else
            SafeSet(gameCompletedPanel, true);
    }

    public void NextLevel()
    {
        currentLevelIndex = Mathf.Min(currentLevelIndex + 1, coinTargets.Length - 1);
        CoinsCollectedThisLevel = 0;

        isPlaying = true;
        isPlayerDead = false;
        Time.timeScale = 1f;
        SafeSet(levelCompletePanel, false);
        CacheAndApplyPlayerSpeed();
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"SCORE: {Score}";
        if (levelText) levelText.text = $"LEVEL: {CurrentLevelNumber}";
        if (coinsText) coinsText.text = $"COINS: {CoinsCollectedThisLevel}/{CurrentTarget}";
    }

    public float GetPlayerSpeed(float baseSpeed)
    {
        float mult = 1f;
        if (speedMultipliers != null && speedMultipliers.Length > 0)
        {
            int i = Mathf.Clamp(currentLevelIndex, 0, speedMultipliers.Length - 1);
            mult = speedMultipliers[i] <= 0 ? 1f : speedMultipliers[i];
        }
        else
        {
            mult = 1f + Mathf.Max(0f, levelSpeedStep) * currentLevelIndex;
        }
        return baseSpeed * mult;
    }

    public void OnPlayerDied()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        isPlaying = false;
        Time.timeScale = 0f;
        SafeSet(gameOverPanel, true);
    }

    public Transform GetPlayer() => player;
    public bool IsPlaying() => isPlaying && !isPlayerDead;

    void CacheAndApplyPlayerSpeed()
    {
        if (!player) return;
        var pc = player.GetComponent<PlayerController>();
        if (!pc) return;
        if (playerBaseSpeed0 < 0f) playerBaseSpeed0 = pc.baseSpeed;
        pc.baseSpeed = GetPlayerSpeed(playerBaseSpeed0);
    }
}
