using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Jugador")]
    [SerializeField] private Transform player;   // arrastra tu Jugador aquí

    [Header("HUD (UI.Text)")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text coinsText;

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;           // Panel inicial con botón Play
    [SerializeField] private GameObject levelCompletePanel;  // Botón Continuar
    [SerializeField] private GameObject gameCompletedPanel;  // Botón Reiniciar
    [SerializeField] private GameObject gameOverPanel;       // Botón Reiniciar

    [Header("Configuración de niveles (monedas objetivo)")]
    [SerializeField] private int[] coinTargets = new int[] { 10, 15, 25 };
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Velocidad por nivel (opcional)")]
    [Tooltip("Multiplicador de velocidad que se aplicará a tu baseSpeed por nivel. Si está vacío o de tamaño distinto, se asume 1 en todos.")]
    [SerializeField] private float[] speedMultipliers = new float[] { 1f, 1f, 1f };

    public int Score { get; private set; } = 0;
    public int CoinsCollectedThisLevel { get; private set; } = 0;

    public int CurrentTarget => coinTargets[Mathf.Clamp(currentLevelIndex, 0, coinTargets.Length - 1)];
    public int CurrentLevelNumber => currentLevelIndex + 1;

    bool isPlaying = false;
    bool isPlayerDead = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 0f; // arrancamos en pausa (menú)
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
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"SCORE: {Score}";
        if (levelText) levelText.text = $"LEVEL: {CurrentLevelNumber}";
        if (coinsText) coinsText.text = $"COINS: {CoinsCollectedThisLevel}/{CurrentTarget}";
    }

    // ======= Métodos que pide tu PlayerController =======

    // Devuelve la velocidad a usar por el jugador según el nivel
    public float GetPlayerSpeed(float baseSpeed)
    {
        float mult = 1f;
        if (speedMultipliers != null && speedMultipliers.Length > 0)
        {
            int i = Mathf.Clamp(currentLevelIndex, 0, speedMultipliers.Length - 1);
            mult = speedMultipliers[i] <= 0 ? 1f : speedMultipliers[i];
        }
        return baseSpeed * mult;
    }

    // Llamado cuando el jugador muere
    public void OnPlayerDied()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        isPlaying = false;
        Time.timeScale = 0f;
        SafeSet(gameOverPanel, true);
    }

    // Accesos para otros scripts
    public Transform GetPlayer() => player;
    public bool IsPlaying() => isPlaying && !isPlayerDead;
}
