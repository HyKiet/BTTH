using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script tự động tạo và cấu hình toàn bộ HUD (HP Bar, Score Box, Kill Counter, Game Over Panel).
/// Dùng Scale-based positioning để responsive trên mọi resolution.
/// Gắn script này vào GameManager hoặc một GameObject trống trên Scene.
/// </summary>
[DefaultExecutionOrder(-10)]
public class HUDSetup : MonoBehaviour
{
    void Awake()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Đảm bảo CanvasScaler dùng Scale With Screen Size
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        SetupHPBar(canvas);
        SetupScoreBox(canvas);
        SetupKillCounter(canvas);
        SetupWaveText(canvas);
        SetupGameOverPanel(canvas);
    }

    void SetupWaveText(Canvas canvas)
    {
        var old = canvas.transform.Find("WaveText");
        if (old != null) Destroy(old.gameObject);

        GameObject waveObj = new GameObject("WaveText");
        waveObj.transform.SetParent(canvas.transform, false);
        RectTransform wrt = waveObj.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.3f, 0.85f);
        wrt.anchorMax = new Vector2(0.7f, 0.95f);
        wrt.offsetMin = Vector2.zero;
        wrt.offsetMax = Vector2.zero;
        TextMeshProUGUI wTmp = waveObj.AddComponent<TextMeshProUGUI>();
        wTmp.text = "";
        wTmp.fontSize = 24;
        wTmp.alignment = TextAlignmentOptions.Center;
        wTmp.color = Color.white;
        wTmp.outlineWidth = 0.3f;
        wTmp.outlineColor = Color.black;
    }

    void SetupHPBar(Canvas canvas)
    {
        // Xóa HPContainer cũ nếu tồn tại
        var old = canvas.transform.Find("HPContainer");
        if (old != null) Destroy(old.gameObject);

        // Tạo HPContainer — neo góc trên trái (Scale-based)
        GameObject container = new GameObject("HPContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0.2f, 1); // 20% chiều rộng màn hình
        rt.pivot = new Vector2(0, 1);
        rt.offsetMin = new Vector2(15, -65); // bottom, left offset
        rt.offsetMax = new Vector2(0, -15);  // top, right offset

        // Icon Tim
        GameObject iconObj = new GameObject("HPIcon");
        iconObj.transform.SetParent(container.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.2f);
        iconRt.anchorMax = new Vector2(0, 1f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.sizeDelta = new Vector2(45, 0);
        iconRt.anchoredPosition = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/HpICon.png");

        // BG Thanh máu
        GameObject barBg = new GameObject("HPBarBg");
        barBg.transform.SetParent(container.transform, false);
        RectTransform barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0, 0.3f);
        barBgRt.anchorMax = new Vector2(1, 0.8f);
        barBgRt.pivot = new Vector2(0, 0.5f);
        barBgRt.offsetMin = new Vector2(50, 0);
        barBgRt.offsetMax = new Vector2(-5, 0);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        // Thanh màu xanh (Fill)
        GameObject barFill = new GameObject("HPBarFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform barFillRt = barFill.AddComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.offsetMin = Vector2.zero;
        barFillRt.offsetMax = Vector2.zero;
        Image fillImg = barFill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.2f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // Text số HP
        GameObject hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(container.transform, false);
        RectTransform hpTextRt = hpTextObj.AddComponent<RectTransform>();
        hpTextRt.anchorMin = new Vector2(0.15f, 0);
        hpTextRt.anchorMax = new Vector2(1, 0.35f);
        hpTextRt.offsetMin = Vector2.zero;
        hpTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI hpTmp = hpTextObj.AddComponent<TextMeshProUGUI>();
        hpTmp.text = "100/100";
        hpTmp.fontSize = 14;
        hpTmp.alignment = TextAlignmentOptions.Center;
        hpTmp.color = Color.white;
    }

    void SetupScoreBox(Canvas canvas)
    {
        // Xóa cũ nếu tồn tại
        var old = canvas.transform.Find("ScoreBox");
        if (old != null) Destroy(old.gameObject);

        // ScoreBox — góc trên phải
        GameObject scoreBox = new GameObject("ScoreBox");
        scoreBox.transform.SetParent(canvas.transform, false);
        RectTransform srt = scoreBox.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.85f, 1);
        srt.anchorMax = new Vector2(1, 1);
        srt.pivot = new Vector2(1, 1);
        srt.offsetMin = new Vector2(0, -55);
        srt.offsetMax = new Vector2(-15, -15);
        Image sImg = scoreBox.AddComponent<Image>();
        sImg.color = new Color(0, 0, 0, 0.5f);

        // Score text
        GameObject scoreText = new GameObject("ScoreText");
        scoreText.transform.SetParent(scoreBox.transform, false);
        RectTransform strt = scoreText.AddComponent<RectTransform>();
        strt.anchorMin = Vector2.zero;
        strt.anchorMax = Vector2.one;
        strt.offsetMin = new Vector2(5, 5);
        strt.offsetMax = new Vector2(-5, -5);
        TextMeshProUGUI sTmp = scoreText.AddComponent<TextMeshProUGUI>();
        sTmp.text = "SCORE: 0";
        sTmp.fontSize = 18;
        sTmp.alignment = TextAlignmentOptions.Center;
        sTmp.color = Color.yellow;
    }

    void SetupKillCounter(Canvas canvas)
    {
        // Xóa cũ
        var old = canvas.transform.Find("KillsText");
        if (old != null) Destroy(old.gameObject);

        // Kill counter — dưới HP bar
        GameObject killsObj = new GameObject("KillsText");
        killsObj.transform.SetParent(canvas.transform, false);
        RectTransform krt = killsObj.AddComponent<RectTransform>();
        krt.anchorMin = new Vector2(0, 1);
        krt.anchorMax = new Vector2(0.15f, 1);
        krt.pivot = new Vector2(0, 1);
        krt.offsetMin = new Vector2(15, -95);
        krt.offsetMax = new Vector2(0, -70);
        TextMeshProUGUI kTmp = killsObj.AddComponent<TextMeshProUGUI>();
        kTmp.text = "KILLS: 0";
        kTmp.fontSize = 16;
        kTmp.alignment = TextAlignmentOptions.Left;
        kTmp.color = Color.white;
    }

    void SetupGameOverPanel(Canvas canvas)
    {
        // Xóa cũ
        var old = canvas.transform.Find("GameOverPanel");
        if (old != null) Destroy(old.gameObject);

        // Panel full-screen overlay
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        Image pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0, 0, 0, 0.7f);

        // Game Over Text
        GameObject goText = new GameObject("GameOverText");
        goText.transform.SetParent(panel.transform, false);
        RectTransform goRt = goText.AddComponent<RectTransform>();
        goRt.anchorMin = new Vector2(0.2f, 0.4f);
        goRt.anchorMax = new Vector2(0.8f, 0.7f);
        goRt.offsetMin = Vector2.zero;
        goRt.offsetMax = Vector2.zero;
        TextMeshProUGUI goTmp = goText.AddComponent<TextMeshProUGUI>();
        goTmp.text = "GAME OVER!";
        goTmp.fontSize = 48;
        goTmp.alignment = TextAlignmentOptions.Center;
        goTmp.color = Color.red;

        // Play Again Button
        GameObject btnObj = new GameObject("PlayAgainButton");
        btnObj.transform.SetParent(panel.transform, false);
        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.25f);
        btnRt.anchorMax = new Vector2(0.65f, 0.35f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.2f);
        Button btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.85f, 0.3f);
        colors.pressedColor = new Color(0.15f, 0.5f, 0.15f);
        btn.colors = colors;

        // Button Text
        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btrt = btnTextObj.AddComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero;
        btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero;
        btrt.offsetMax = Vector2.zero;
        TextMeshProUGUI btTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btTmp.text = "PLAY AGAIN";
        btTmp.fontSize = 24;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        // Ẩn panel khi bắt đầu
        panel.SetActive(false);
    }

    Sprite LoadSprite(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
