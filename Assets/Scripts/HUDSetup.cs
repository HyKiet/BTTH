using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script tự động tạo và cấu hình toàn bộ HUD.
/// Tối ưu: fix TMP_FontAsset.CreateFontAsset leak, giảm code thừa.
/// </summary>
[DefaultExecutionOrder(10)]
public class HUDSetup : MonoBehaviour
{
    // ── Cache font asset static để tránh tạo lại mỗi lần ──
    private static TMP_FontAsset _cachedGameFont;
    private static bool _fontLoaded = false;

    void Awake()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[HUDSetup] GameManager.Instance is NULL!");
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[HUDSetup] Không tìm thấy Canvas trên scene!");
            return;
        }

        // Cleanup old objects
        DestroyIfExists(canvas.transform, "HPBar");
        DestroyIfExists(canvas.transform, "Avatar");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Load font 1 lần duy nhất (tránh leak)
        TMP_FontAsset gameFont = GetCachedFont();

        SetupHPBar(canvas);
        SetupKillCounter(canvas);
        SetupWeaponBox(canvas);
        SetupGameOverPanel(canvas, gameFont);
    }

    static void DestroyIfExists(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Destroy(old.gameObject);
    }

    /// <summary>
    /// Load font chỉ 1 lần, cache static. Fix memory leak.
    /// </summary>
    static TMP_FontAsset GetCachedFont()
    {
        if (_fontLoaded) return _cachedGameFont;
        _fontLoaded = true;

#if UNITY_EDITOR
        Font showgFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Mad Doctor Assets/Font/SHOWG.TTF");
        if (showgFont != null)
        {
            _cachedGameFont = TMP_FontAsset.CreateFontAsset(showgFont);
        }
#endif
        return _cachedGameFont;
    }

    void SetupHPBar(Canvas canvas)
    {
        DestroyIfExists(canvas.transform, "HPContainer");

        GameObject container = new GameObject("HPContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(650, 180);
        rt.anchoredPosition = new Vector2(20, -20);

        Image bgImg = container.AddComponent<Image>();
        bgImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/ProfileBar.png");
        bgImg.preserveAspect = true;
        bgImg.raycastTarget = false;

        // Fill bar
        GameObject barFill = new GameObject("HPBarFill");
        barFill.transform.SetParent(container.transform, false);
        RectTransform fillRt = barFill.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0.5f);
        fillRt.anchorMax = new Vector2(0, 0.5f);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.sizeDelta = new Vector2(400, 40);
        fillRt.anchoredPosition = new Vector2(195, 20);
        Image fillImg = barFill.AddComponent<Image>();
        fillImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/GreenHPbar.png");
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // HP Text
        GameObject hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(barFill.transform, false);
        TextMeshProUGUI hpTmp = hpTextObj.AddComponent<TextMeshProUGUI>();
        hpTmp.text = "";

        // Assign references
        GameManager.Instance.hpBarFill = fillImg;
        GameManager.Instance.hpText = hpTmp;
    }

    void SetupKillCounter(Canvas canvas)
    {
        DestroyIfExists(canvas.transform, "KillsText");

        GameObject killsObj = new GameObject("KillsText");
        killsObj.transform.SetParent(canvas.transform, false);
        RectTransform krt = killsObj.AddComponent<RectTransform>();
        krt.anchorMin = new Vector2(0, 1);
        krt.anchorMax = new Vector2(0, 1);
        krt.pivot = new Vector2(0, 1);
        krt.sizeDelta = new Vector2(250, 60);
        krt.anchoredPosition = new Vector2(175, -165);

        TextMeshProUGUI kTmp = killsObj.AddComponent<TextMeshProUGUI>();
        kTmp.text = "KILLS: 0";
        kTmp.fontSize = 42;
        kTmp.fontStyle = FontStyles.Bold;
        kTmp.alignment = TextAlignmentOptions.Left;
        kTmp.color = Color.white;
        kTmp.outlineWidth = 0.2f;
        kTmp.outlineColor = Color.black;

        GameManager.Instance.killsText = kTmp;
    }

    void SetupWeaponBox(Canvas canvas)
    {
        DestroyIfExists(canvas.transform, "WeaponContainer");

        GameObject container = new GameObject("WeaponContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(180, 180);
        rt.anchoredPosition = new Vector2(30, -220);

        Image bgImg = container.AddComponent<Image>();
        bgImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/BGWeapon.png");
        bgImg.preserveAspect = true;
        bgImg.color = new Color32(255, 0, 180, 255);

        // Weapon Icon
        GameObject wIconObj = new GameObject("WeaponIcon");
        wIconObj.transform.SetParent(container.transform, false);
        RectTransform wRt = wIconObj.AddComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0.5f, 0.5f);
        wRt.anchorMax = new Vector2(0.5f, 0.5f);
        wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.sizeDelta = new Vector2(140, 140);
        wRt.anchoredPosition = Vector2.zero;
        Image wImg = wIconObj.AddComponent<Image>();
        wImg.preserveAspect = true;

        // Swap Icon
        GameObject reloadIconObj = new GameObject("SwapIcon");
        reloadIconObj.transform.SetParent(container.transform, false);
        RectTransform rRt = reloadIconObj.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(1, 0);
        rRt.anchorMax = new Vector2(1, 0);
        rRt.pivot = new Vector2(1, 0);
        rRt.sizeDelta = new Vector2(50, 50);
        rRt.anchoredPosition = new Vector2(10, -10);
        Image reloadImg = reloadIconObj.AddComponent<Image>();
        reloadImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/radioBtn.png");
        reloadImg.preserveAspect = true;
        reloadImg.color = new Color32(255, 220, 100, 255);
    }

    void SetupGameOverPanel(Canvas canvas, TMP_FontAsset gameFont)
    {
        DestroyIfExists(canvas.transform, "GameOverPanel");

        // Panel
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        // Overlay
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(panel.transform, false);
        RectTransform ovRt = overlayObj.AddComponent<RectTransform>();
        ovRt.anchorMin = Vector2.zero;
        ovRt.anchorMax = Vector2.one;
        ovRt.offsetMin = Vector2.zero;
        ovRt.offsetMax = Vector2.zero;
        Image ovImg = overlayObj.AddComponent<Image>();
        ovImg.color = new Color(0.05f, 0.0f, 0.1f, 0.85f);
        CanvasGroup overlayGroup = overlayObj.AddComponent<CanvasGroup>();

        // Popup Box
        GameObject popupBox = new GameObject("PopupBox");
        popupBox.transform.SetParent(panel.transform, false);
        RectTransform popupRt = popupBox.AddComponent<RectTransform>();
        popupRt.anchorMin = new Vector2(0.5f, 0.5f);
        popupRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupRt.pivot = new Vector2(0.5f, 0.5f);
        popupRt.sizeDelta = new Vector2(850, 550);
        popupRt.anchoredPosition = Vector2.zero;
        Image popupImg = popupBox.AddComponent<Image>();
        popupImg.sprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/PopUPbox.png");
        popupImg.type = Image.Type.Sliced;
        popupImg.preserveAspect = false;
        CanvasGroup popupGroup = popupBox.AddComponent<CanvasGroup>();

        // Title
        TextMeshProUGUI titleTmp = CreateText(popupBox.transform, "GameOverTitle", "GAME OVER!",
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.92f), 72, gameFont);
        titleTmp.outlineWidth = 0.3f;
        titleTmp.outlineColor = new Color32(30, 30, 30, 255);

        // Stats
        TextMeshProUGUI scoreTmp = CreateText(popupBox.transform, "ScoreStatText", "SCORE: 0",
            new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.65f), 38, gameFont);
        TextMeshProUGUI killsTmp = CreateText(popupBox.transform, "KillsStatText", "KILLS: 0",
            new Vector2(0.15f, 0.40f), new Vector2(0.5f, 0.52f), 30, gameFont);
        TextMeshProUGUI waveTmp = CreateText(popupBox.transform, "WaveStatText", "WAVE: 0",
            new Vector2(0.5f, 0.40f), new Vector2(0.85f, 0.52f), 30, gameFont);
        TextMeshProUGUI highScoreTmp = CreateText(popupBox.transform, "HighScoreText", "HIGHSCORE: 0",
            new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.40f), 32, gameFont);
        highScoreTmp.color = new Color32(255, 220, 100, 255);

        // New Record Badge
        TextMeshProUGUI nrTmp = CreateText(popupBox.transform, "NewRecordText", "★ NEW RECORD! ★",
            new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.30f), 28, gameFont);
        nrTmp.color = new Color32(255, 200, 50, 255);
        nrTmp.outlineWidth = 0.2f;
        nrTmp.outlineColor = new Color32(150, 80, 0, 255);
        nrTmp.gameObject.SetActive(false);

        // Play Again Button
        GameObject btnObj = new GameObject("PlayAgainButton");
        btnObj.transform.SetParent(popupBox.transform, false);
        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.25f, 0.05f);
        btnRt.anchorMax = new Vector2(0.75f, 0.20f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        Image btnImg = btnObj.AddComponent<Image>();
        Sprite btnSprite = LoadSprite("Assets/Mad Doctor Assets/Sprites/User Interfaces/Btn1.png");
        if (btnSprite != null)
        {
            btnImg.sprite = btnSprite;
            btnImg.preserveAspect = true;
        }
        else
        {
            btnImg.color = new Color32(100, 200, 255, 255);
        }
        Button btn = btnObj.AddComponent<Button>();
        var btnColors = btn.colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = new Color32(220, 240, 255, 255);
        btnColors.pressedColor = new Color32(180, 220, 255, 255);
        btnColors.selectedColor = Color.white;
        btn.colors = btnColors;

        // Button Text
        TextMeshProUGUI btTmp = CreateText(btnObj.transform, "ButtonText", "PLAY AGAIN",
            Vector2.zero, Vector2.one, 32, gameFont, new Vector2(10, 5), new Vector2(-10, -5));

        // GameOverUI component
        GameOverUI goUI = panel.AddComponent<GameOverUI>();
        goUI.overlayGroup = overlayGroup;
        goUI.popupRect = popupRt;
        goUI.popupGroup = popupGroup;
        goUI.titleText = titleTmp;
        goUI.scoreText = scoreTmp;
        goUI.killsText = killsTmp;
        goUI.waveText = waveTmp;
        goUI.highScoreText = highScoreTmp;
        goUI.newRecordText = nrTmp;
        goUI.playAgainBtn = btn;

        panel.SetActive(false);

        // Assign to GameManager
        GameManager.Instance.gameOverPanel = panel;
        GameManager.Instance.gameOverText = titleTmp;
        GameManager.Instance.playAgainBtn = btn;
    }

    /// <summary>
    /// Helper: Tạo TextMeshProUGUI
    /// </summary>
    TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize, TMP_FontAsset font,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin ?? Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = new Color32(30, 30, 30, 200);
        if (font != null) tmp.font = font;
        return tmp;
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
