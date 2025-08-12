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
        if (CoinsCollectedThisLevel >= CurrentTarget) LevelCompleted();
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
        ApplyBackground();
        ApplyMusic();
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
        return baseSpeed * mult;
    }

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

    void ApplyBackground()
    {
        if (!backgroundRenderer || levelBackgrounds == null || levelBackgrounds.Length == 0) return;
        int i = Mathf.Clamp(currentLevelIndex, 0, levelBackgrounds.Length - 1);
        if (levelBackgrounds[i]) backgroundRenderer.sprite = levelBackgrounds[i];
    }

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
