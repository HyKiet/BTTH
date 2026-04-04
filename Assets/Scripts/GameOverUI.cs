using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý toàn bộ UI Game Over: hiệu ứng fade-in, slide animation, score display.
/// Tối ưu: dừng PulseText coroutine khi ẩn, tránh infinite loop.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup overlayGroup;
    public RectTransform popupRect;
    public CanvasGroup popupGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI newRecordText;
    public Button playAgainBtn;
    public Button mainMenuBtn;

    [Header("Animation Settings")]
    public float overlayFadeDuration = 0.5f;
    public float popupSlideDistance = 100f;
    public float popupAnimDuration = 0.4f;
    public float scoreCountDuration = 1.0f;

    private int targetScore = 0;
    private int targetKills = 0;
    private int targetWave = 0;
    private int targetHighScore = 0;
    private bool isNewRecord = false;

    // ── Track coroutines để stop khi cần ──
    private Coroutine pulseCoroutine;

    /// <summary>
    /// Hiển thị Game Over panel với hiệu ứng đẹp.
    /// </summary>
    public void Show(int score, int kills, int wave, int highScore, bool newRecord)
    {
        targetScore = score;
        targetKills = kills;
        targetWave = wave;
        targetHighScore = highScore;
        isNewRecord = newRecord;

        gameObject.SetActive(true);

        // Reset trạng thái ban đầu
        if (overlayGroup != null) overlayGroup.alpha = 0f;
        if (popupGroup != null) popupGroup.alpha = 0f;
        if (popupRect != null) popupRect.anchoredPosition = new Vector2(0, -popupSlideDistance);

        // Reset text
        if (scoreText != null) scoreText.text = "SCORE: 0";
        if (killsText != null) killsText.text = "KILLS: 0";
        if (waveText != null) waveText.text = "WAVE: 0";
        if (highScoreText != null) highScoreText.text = "HIGHSCORE: " + highScore;
        if (newRecordText != null) newRecordText.gameObject.SetActive(false);

        // Stop old pulse nếu đang chạy
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        StartCoroutine(AnimateShow());
    }

    void OnDisable()
    {
        // Cleanup khi panel bị ẩn
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    IEnumerator AnimateShow()
    {
        // Phase 1: Fade in overlay
        float elapsed = 0f;
        while (elapsed < overlayFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / overlayFadeDuration);
            if (overlayGroup != null) overlayGroup.alpha = t * 0.85f;
            yield return null;
        }
        if (overlayGroup != null) overlayGroup.alpha = 0.85f;

        // Phase 2: Slide popup
        elapsed = 0f;
        Vector2 startPos = new Vector2(0, -popupSlideDistance);
        Vector2 endPos = Vector2.zero;
        while (elapsed < popupAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popupAnimDuration);
            float eased = EaseOutBack(t);
            if (popupRect != null) popupRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            if (popupGroup != null) popupGroup.alpha = t;
            yield return null;
        }
        if (popupRect != null) popupRect.anchoredPosition = endPos;
        if (popupGroup != null) popupGroup.alpha = 1f;

        // Phase 3: Count-up
        yield return StartCoroutine(CountUpScores());

        // Phase 4: NEW RECORD pulse (tracked coroutine)
        if (isNewRecord && newRecordText != null)
        {
            newRecordText.gameObject.SetActive(true);
            pulseCoroutine = StartCoroutine(PulseText(newRecordText));
        }
    }

    IEnumerator CountUpScores()
    {
        float elapsed = 0f;
        int prevScore = 0, prevKills = 0, prevWave = 0;

        while (elapsed < scoreCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scoreCountDuration);
            float eased = EaseOutQuart(t);

            int curScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, eased));
            int curKills = Mathf.RoundToInt(Mathf.Lerp(0, targetKills, eased));
            int curWave = Mathf.RoundToInt(Mathf.Lerp(0, targetWave, eased));

            if (curScore != prevScore && scoreText != null)
                scoreText.text = "SCORE: " + curScore;
            if (curKills != prevKills && killsText != null)
                killsText.text = "KILLS: " + curKills;
            if (curWave != prevWave && waveText != null)
                waveText.text = "WAVE: " + curWave;

            prevScore = curScore;
            prevKills = curKills;
            prevWave = curWave;

            yield return null;
        }

        // Final values
        if (scoreText != null) scoreText.text = "SCORE: " + targetScore;
        if (killsText != null) killsText.text = "KILLS: " + targetKills;
        if (waveText != null) waveText.text = "WAVE: " + targetWave;
    }

    IEnumerator PulseText(TextMeshProUGUI text)
    {
        if (text == null) yield break;

        float speed = 2f;
        // Giới hạn thời gian pulse thay vì loop vô tận
        float maxDuration = 30f;
        float elapsed = 0f;

        while (text != null && text.gameObject.activeInHierarchy && elapsed < maxDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float scale = 1f + Mathf.Sin(Time.unscaledTime * speed) * 0.1f;
            text.transform.localScale = Vector3.one * scale;

            float glow = 0.8f + Mathf.Sin(Time.unscaledTime * speed * 1.5f) * 0.2f;
            text.color = new Color(1f, glow, 0.2f, 1f);

            yield return null;
        }

        // Cleanup
        if (text != null)
        {
            text.transform.localScale = Vector3.one;
            text.color = new Color32(255, 200, 50, 255);
        }
        pulseCoroutine = null;
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }
}
