using UnityEngine;

/// <summary>
/// Quản lý vật phẩm phục hồi máu (Health Potion) dựa trên xác suất rơi từ quái.
/// </summary>
public class PotionManager : MonoBehaviour
{
    public static PotionManager Instance;

    public GameObject potionPrefab;
    
    [Header("Drop Settings")]
    [Tooltip("Xác suất rơi ra cục máu khi diệt quái (từ 0.0 đến 1.0)")]
    public float dropProbability = 0.2f; // 20% tỉ lệ rơi
    public int maxPotionsOnMap = 5;      // Tối đa 5 cục trên bản đồ

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Gọi hàm này khi một quái vật chết để kiểm tra tỉ lệ rớt lọ máu.
    /// </summary>
    public void TryDropPotion(Vector3 dropPosition)
    {
        if (potionPrefab == null) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        // Đếm số lượng máu hiện có trên bản đồ
        HealthPotion[] activePotions = FindObjectsByType<HealthPotion>(FindObjectsSortMode.None);
        if (activePotions.Length >= maxPotionsOnMap) return;

        // Kiểm tra xác suất rơi
        if (Random.value <= dropProbability)
        {
            Instantiate(potionPrefab, dropPosition, Quaternion.identity);
        }
    }
}
