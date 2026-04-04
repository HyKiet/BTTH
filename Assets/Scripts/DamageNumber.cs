using UnityEngine;
using TMPro;

/// <summary>
/// Hiệu ứng số damage nổi lên khi bắn trúng enemy.
/// Dùng Object Pool thay vì tạo GameObject mới mỗi lần → giảm GC pressure.
/// Gọi DamageNumber.Spawn(position, damage) từ bất kỳ đâu.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    public float floatSpeed = 1.5f;
    public float lifetime = 0.8f;
    public float fadeStartTime = 0.4f;

    private TextMeshPro tmp;
    private Color startColor;
    private float elapsed = 0f;

    // ── Pool constants ──
    public const string POOL_TAG = "DamageNumber";
    private static GameObject _prefab;
    private static bool _poolRegistered = false;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null) tmp = gameObject.AddComponent<TextMeshPro>();
    }

    void OnEnable()
    {
        // Reset state mỗi khi được lấy từ pool
        elapsed = 0f;
        if (tmp != null)
        {
            startColor = tmp.color;
        }

        // Auto-return về pool sau lifetime
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifetime);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void Update()
    {
        // Bay lên
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        elapsed += Time.deltaTime;

        // Fade out
        if (elapsed > fadeStartTime && tmp != null)
        {
            float fadeProgress = (elapsed - fadeStartTime) / (lifetime - fadeStartTime);
            fadeProgress = Mathf.Clamp01(fadeProgress);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeProgress);
        }
    }

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Đăng ký pool cho DamageNumber (gọi 1 lần duy nhất khi game start)
    /// </summary>
    static void EnsurePoolRegistered()
    {
        if (_poolRegistered || ObjectPool.Instance == null) return;

        // Tạo prefab tạm
        if (_prefab == null)
        {
            _prefab = new GameObject("DamageNumberPrefab");
            _prefab.SetActive(false);

            TextMeshPro t = _prefab.AddComponent<TextMeshPro>();
            t.fontSize = 5;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.yellow;
            t.sortingOrder = 100;
            t.outlineWidth = 0.2f;
            t.outlineColor = Color.black;

            _prefab.AddComponent<DamageNumber>();
            _prefab.transform.localScale = Vector3.one * 0.5f;
            DontDestroyOnLoad(_prefab);
        }

        ObjectPool.Instance.RegisterPool(POOL_TAG, _prefab, 10, 30);
        _poolRegistered = true;
    }

    /// <summary>
    /// Tạo số damage tại vị trí chỉ định (dùng Object Pool)
    /// </summary>
    public static void Spawn(Vector3 position, int damage, Color? color = null)
    {
        EnsurePoolRegistered();

        Vector3 spawnPos = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);

        GameObject obj = null;
        if (ObjectPool.Instance != null)
            obj = ObjectPool.Instance.Get(POOL_TAG, spawnPos, Quaternion.identity);

        // Fallback nếu pool đầy hoặc chưa sẵn sàng
        if (obj == null) return; // Bỏ qua thay vì tạo mới → bảo vệ bộ nhớ

        TextMeshPro tmp = obj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = damage.ToString();
            Color c = color ?? Color.yellow;
            tmp.color = c;
        }

        obj.transform.localScale = Vector3.one * 0.5f;
    }
}
