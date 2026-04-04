using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Manager Singleton — quản lý score, kills, HP UI, Game Over flow.
/// Tối ưu: xóa fallback Find() thừa, tối ưu string, cleanup on restart.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Stats")]
    public int score = 0;
    public int kills = 0;

    /// <summary>
    /// Flag cho các script khác kiểm tra trạng thái Game Over.
    /// </summary>
    public bool isGameOver { get; private set; } = false;

    [Header("UI Dependencies")]
    public TextMeshProUGUI killsText;
    public Image hpBarFill;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button playAgainBtn;
    public Image weaponIcon;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI scoreText;

    // ── String builder cache ──
    private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(32);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Time.timeScale = 1f;

        // HUDSetup (order=10) sẽ gán hầu hết references.
        // Chỉ fallback cho các field còn null sau HUDSetup
        if (weaponIcon == null)
        {
            GameObject iconObj = GameObject.Find("WeaponIcon");
            if (iconObj != null) weaponIcon = iconObj.GetComponent<Image>();
        }

        if (playAgainBtn != null)
            playAgainBtn.onClick.AddListener(RestartGame);

        UpdateUI();
    }

    public void AddKill()
    {
        if (isGameOver) return;
        kills++;
        score += 10;
        UpdateUI();
    }

    public void UpdateHP(int currentHealth, int maxHealth)
    {
        if (hpBarFill != null)
            hpBarFill.fillAmount = (float)currentHealth / maxHealth;

        if (hpText != null)
        {
            sb.Clear();
            sb.Append(currentHealth).Append('/').Append(maxHealth);
            hpText.text = sb.ToString();
        }
    }

    public void UpdateWeaponUI(Sprite icon)
    {
        if (weaponIcon != null && icon != null)
            weaponIcon.sprite = icon;
    }

    void UpdateUI()
    {
        if (killsText != null)
        {
            sb.Clear();
            sb.Append("KILLS: ").Append(kills);
            killsText.text = sb.ToString();
        }
        if (scoreText != null)
        {
            sb.Clear();
            sb.Append("SCORE: ").Append(score);
            scoreText.text = sb.ToString();
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 1. Dừng WaveManager
        if (WaveManager.Instance != null)
            WaveManager.Instance.StopAllCoroutines();

        // 2. Disable tất cả enemy còn sống
        DisableAllEnemies();

        // 3. Return tất cả bullets về pool
        ReturnAllBullets();

        // 4. High Score
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        bool isNewRecord = score > currentHighScore;
        if (isNewRecord)
        {
            currentHighScore = score;
            PlayerPrefs.SetInt("HighScore", currentHighScore);
            PlayerPrefs.Save();
        }

        // 5. Lấy wave hiện tại
        int currentWave = 0;
        if (WaveManager.Instance != null)
            currentWave = WaveManager.Instance.GetCurrentWave();

        // 6. Hiển thị Game Over Panel
        if (gameOverPanel != null)
        {
            GameOverUI goUI = gameOverPanel.GetComponent<GameOverUI>();
            if (goUI != null)
            {
                goUI.Show(score, kills, currentWave, currentHighScore, isNewRecord);
            }
            else
            {
                gameOverPanel.SetActive(true);
                if (gameOverText != null)
                {
                    sb.Clear();
                    sb.Append("GAME OVER!\n\nSCORE: ").Append(score)
                      .Append("\nKILLS: ").Append(kills)
                      .Append("\nWAVE: ").Append(currentWave)
                      .Append("\n\nHIGHSCORE: ").Append(currentHighScore);
                    gameOverText.text = sb.ToString();
                }
            }
        }

        // 7. Slow-motion + Fade BGM
        StartCoroutine(SlowMotionThenFreeze());
        StartCoroutine(FadeBGM());
    }

    System.Collections.IEnumerator SlowMotionThenFreeze()
    {
        float duration = 0.8f;
        float elapsed = 0f;
        float startScale = Time.timeScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Time.timeScale = Mathf.Lerp(startScale, 0f, t);
            yield return null;
        }

        Time.timeScale = 0f;
    }

    System.Collections.IEnumerator FadeBGM()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.bgmSource == null)
            yield break;

        AudioSource bgm = AudioManager.Instance.bgmSource;
        float startVol = bgm.volume;
        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgm.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        bgm.volume = 0f;
        bgm.Pause();
    }

    void DisableAllEnemies()
    {
        // Dùng FindObjectsByType chỉ 1 lần duy nhất khi game over (chấp nhận được)
        EnemyController[] melees = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var e in melees)
        {
            if (e != null) e.gameObject.SetActive(false);
        }

        EnemyRangedController[] rangeds = FindObjectsByType<EnemyRangedController>(FindObjectsSortMode.None);
        foreach (var e in rangeds)
        {
            if (e != null) e.gameObject.SetActive(false);
        }
    }

    void ReturnAllBullets()
    {
        if (ObjectPool.Instance == null) return;

        // Return tất cả active bullets về pool
        Bullet[] bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
        foreach (var b in bullets)
        {
            if (b != null) ObjectPool.Instance.Return(b.gameObject);
        }

        EnemyBullet[] eBullets = FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None);
        foreach (var b in eBullets)
        {
            if (b != null) ObjectPool.Instance.Return(b.gameObject);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}