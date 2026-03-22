using UnityEngine;
using TMPro;

/// <summary>
/// Hiệu ứng số damage nổi lên khi bắn trúng enemy.
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

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null) tmp = gameObject.AddComponent<TextMeshPro>();
    }

    void Start()
    {
        startColor = tmp.color;
        Destroy(gameObject, lifetime);
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
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeProgress);
        }
    }

    /// <summary>
    /// Tạo số damage tại vị trí chỉ định
    /// </summary>
    public static void Spawn(Vector3 position, int damage, Color? color = null)
    {
        GameObject obj = new GameObject("DamageNumber");
        obj.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = damage.ToString();
        tmp.fontSize = 5;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color ?? Color.yellow;
        tmp.sortingOrder = 100;

        // Outline cho dễ đọc
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        DamageNumber dn = obj.AddComponent<DamageNumber>();
        
        // Scale nhỏ cho phù hợp world space
        obj.transform.localScale = Vector3.one * 0.5f;
    }
}
