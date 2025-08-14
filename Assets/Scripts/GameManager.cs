using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// GameManager que controla el estado del juego, puntajes, niveles y UI
// Maneja la lógica del juego, incluyendo el inicio, reinicio, y progresión de niveles
// También gestiona el HUD, música de fondo y eventos de finalización de nivel
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
    [SerializeField] private int[] coinTargets = new int[] { 10, 15, 25 };
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Velocidad por nivel (opcional)")]
    [SerializeField] private float[] speedMultipliers = new float[] { 1f, 1f, 1f };

    [Header("Fondo por nivel")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Sprite[] levelBackgrounds;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] levelMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    // Referencias al GameManager y al AudioSource para la música de fondo
    // Maneja el puntaje, monedas recogidas, y el estado del juego
    public int Score { get; private set; } = 0;
    public int CoinsCollectedThisLevel { get; private set; } = 0;
    // Índice del nivel actual y monedas objetivo para el nivel
    // Proporciona acceso a la información del nivel actual y al objetivo de monedas
    public int CurrentTarget => coinTargets[Mathf.Clamp(currentLevelIndex, 0, coinTargets.Length - 1)];
    public int CurrentLevelNumber => currentLevelIndex + 1;

    bool isPlaying = false;
    bool isPlayerDead = false;
    // Inicializa el GameManager, asigna componentes y configura el estado inicial
    // Configura el AudioSource y establece el tiempo de juego en pausa
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!musicSource) musicSource = GetComponent<AudioSource>();
        Time.timeScale = 0f;
        ApplyBackground();
        ApplyMusic();
        UpdateHUD();
    }

    void Start()
    {
        SafeSet(menuPanel, true);
        SafeSet(levelCompletePanel, false);
        SafeSet(gameCompletedPanel, false);
        SafeSet(gameOverPanel, false);
    }

    void SafeSet(GameObject go, bool on) { if (go) go.SetActive(on); }
    // Inicia el juego, resetea el estado del jugador y actualiza la UI
    // Configura el estado del juego para comenzar, resetea el puntaje y las monedas
    // Reanuda el tiempo de juego y actualiza el HUD con la información del nivel
    public void StartGame()
    {
        isPlaying = true;
        isPlayerDead = false;
        Time.timeScale = 1f;
        SafeSet(menuPanel, false);
        SafeSet(gameOverPanel, false);
        ApplyBackground();
        ApplyMusic();
        UpdateHUD();
    }
    // Reinicia el juego, recargando la escena actual
    // Resetea el tiempo de juego y recarga la escena para reiniciar el juego
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    // Añade monedas al puntaje del GameManager, actualiza el HUD y verifica si se completa el nivel
    // Incrementa el puntaje y las monedas recogidas, actualiza la UI y verifica si se ha alcanzado el objetivo de monedas
    // Si se alcanza el objetivo, marca el nivel como completado
    public void AddCoin(int amount = 1)
    {
        if (isPlayerDead) return;
        Score += amount;
        CoinsCollectedThisLevel += amount;
        UpdateHUD();
        if (CoinsCollectedThisLevel >= CurrentTarget) LevelCompleted();
    }
    // Marca el nivel como completado, pausa el juego y muestra el panel de finalización de nivel
    // Actualiza el estado del juego, pausa el tiempo y muestra el panel correspondiente según el nivel actual
    // Si es el último nivel, muestra el panel de finalización del juego
    void LevelCompleted()
    {
        isPlaying = false;
        Time.timeScale = 0f;
        if (currentLevelIndex < coinTargets.Length - 1)
            SafeSet(levelCompletePanel, true);
        else
            SafeSet(gameCompletedPanel, true);
    }
    // Avanza al siguiente nivel, resetea las monedas recogidas y actualiza el estado del juego
    // Incrementa el índice del nivel actual, resetea las monedas recogidas y actual
    public void NextLevel()
    {
        currentLevelIndex = Mathf.Min(currentLevelIndex + 1, coinTargets.Length - 1);
        CoinsCollectedThisLevel = 0;
        isPlaying = true;
        isPlayerDead = false;
        Time.timeScale = 1f;
        SafeSet(levelCompletePanel, false);
        ApplyBackground();
        ApplyMusic();
        UpdateHUD();
    }
    // Actualiza el HUD con la información del puntaje, nivel y monedas recogidas
    // Muestra el puntaje actual, el número del nivel y las monedas recogidas en el nivel actual
    // Actualiza los textos de la UI para reflejar el estado actual del juego
    void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"SCORE: {Score}";
        if (levelText) levelText.text = $"LEVEL: {CurrentLevelNumber}";
        if (coinsText) coinsText.text = $"COINS: {CoinsCollectedThisLevel}/{CurrentTarget}";
    }
    // Obtiene la velocidad del jugador basada en el nivel actual y un multiplicador opcional
    // Calcula la velocidad del jugador multiplicando la velocidad base por el multiplicador correspondiente al nivel actual
    // Si no hay multiplicadores definidos, usa 1 como valor por defecto
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
    // Maneja la muerte del jugador, detiene el juego y muestra el panel de Game Over
    // Marca al jugador como muerto, detiene la música de fondo y muestra el panel de Game Over
    // Pausa el tiempo de juego y reproduce el clip de Game Over si está disponible
    public void OnPlayerDied()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        isPlaying = false;

        if (musicSource && musicSource.isPlaying) musicSource.Stop();
        if (musicSource && gameOverClip) musicSource.PlayOneShot(gameOverClip, sfxVolume);
        else if (gameOverClip) AudioSource.PlayClipAtPoint(gameOverClip, Camera.main ? Camera.main.transform.position : Vector3.zero, sfxVolume);

        Time.timeScale = 0f;
        SafeSet(gameOverPanel, true);
    }

    public Transform GetPlayer() => player;
    public bool IsPlaying() => isPlaying && !isPlayerDead;
    // Obtiene el índice del nivel actual
    // Proporciona acceso al índice del nivel actual para otros scripts o componentes
    void ApplyBackground()
    {
        if (!backgroundRenderer || levelBackgrounds == null || levelBackgrounds.Length == 0) return;
        int i = Mathf.Clamp(currentLevelIndex, 0, levelBackgrounds.Length - 1);
        if (levelBackgrounds[i]) backgroundRenderer.sprite = levelBackgrounds[i];
    }
    // Aplica la música de fondo según el nivel actual
    // Configura el volumen de la música y cambia la pista según el nivel actual
    void ApplyMusic()
    {
        if (!musicSource) return;
        musicSource.volume = musicVolume;
        AudioClip clip = null;
        if (levelMusic != null && levelMusic.Length > 0)
        {
            int i = Mathf.Clamp(currentLevelIndex, 0, levelMusic.Length - 1);
            clip = levelMusic[i];
        }
        if (!clip) clip = musicSource.clip;
        if (!clip) return;
        if (musicSource.clip != clip) musicSource.clip = clip;
        if (!musicSource.isPlaying)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }
}
