using UnityEngine;

/// <summary>
/// Đạn được bắn ra từ Enemy Ranged.
/// Gây sát thương cho PlayerController khi va chạm.
/// Tối ưu: dùng Object Pool, cache components.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 6f;
    public int damage = 15;
    public float lifeTime = 4f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 moveDirection;

    // ── Pool ──
    public const string POOL_TAG = "EnemyBullet";

    void Awake()
    {
        // Cache components 1 lần
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        // Reset state khi lấy từ pool
        moveDirection = Vector2.zero;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 0.001f;
            rb.linearVelocity = Vector2.zero;
        }

        if (col != null)
        {
            col.isTrigger = true;
            col.enabled = true;
        }

        // Auto-return sau lifeTime
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    void OnDisable()
    {
        CancelInvoke();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveDirection * speed;
    }

    /// <summary>
    /// Gọi sau khi lấy từ pool để đặt hướng bay.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Bỏ qua enemy khác
        if (hitInfo.CompareTag("Enemy")) return;
        // Bỏ qua đạn khác
        if (hitInfo.GetComponent<Bullet>() != null || hitInfo.GetComponent<EnemyBullet>() != null) return;

        // Gây sát thương cho player
        PlayerController player = hitInfo.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        CancelInvoke();
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }
}
