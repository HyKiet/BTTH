using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu nhỏ hiển thị trên đầu enemy.
/// Tự động ẩn khi HP đầy, hiện khi bị đánh.
/// </summary>
public class EnemyHPBar : MonoBehaviour
{
    private Transform target;     // Enemy mà bar này theo
    private int maxHP;
    private int currentHP;
    private Canvas canvas;
    private Image bgImage;
    private Image fillImage;
    private Vector3 offset = new Vector3(0, 0.8f, 0);
    
    // Ẩn/hiện
    private float hideTimer = 0f;
    private float hideDelay = 3f;
    private bool isVisible = false;

    /// <summary>
    /// Tạo HP bar cho enemy
    /// </summary>
    public static EnemyHPBar Create(Transform enemyTransform, int maxHealth)
    {
        // Tạo Canvas World Space trên enemy
        GameObject hpBarObj = new GameObject("EnemyHPBar");
        
        Canvas canvas = hpBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRt = hpBarObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(1f, 0.12f);
        
        // Background (đen)
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(hpBarObj.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Fill (xanh → đỏ)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(hpBarObj.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(1, 1);
        fillRt.offsetMax = new Vector2(-1, -1);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // Gắn script
        EnemyHPBar hpBar = hpBarObj.AddComponent<EnemyHPBar>();
        hpBar.target = enemyTransform;
        hpBar.maxHP = maxHealth;
        hpBar.currentHP = maxHealth;
        hpBar.canvas = canvas;
        hpBar.bgImage = bgImg;
        hpBar.fillImage = fillImg;
        
        // Bắt đầu ẩn
        hpBarObj.SetActive(false);
        
        return hpBar;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Theo dõi vị trí enemy
        transform.position = target.position + offset;
        
        // Luôn nhìn về camera
        transform.rotation = Quaternion.identity;

        // Auto-hide sau 1 thời gian
        if (isVisible)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                gameObject.SetActive(false);
                isVisible = false;
            }
        }
    }

    public void UpdateHP(int hp)
    {
        currentHP = hp;
        
        if (fillImage != null)
        {
            float ratio = (float)currentHP / maxHP;
            fillImage.fillAmount = ratio;
            
            // Đổi màu theo % HP
            if (ratio > 0.5f)
                fillImage.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
            else
                fillImage.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
        }

        // Hiện bar khi bị đánh
        if (!isVisible || !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            isVisible = true;
        }
        hideTimer = hideDelay;
    }
}
