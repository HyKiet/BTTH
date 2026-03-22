using UnityEngine;

/// <summary>
/// Đạn được bắn ra từ Enemy Ranged.
/// Gây sát thương cho PlayerController khi va chạm.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 6f;
    public int damage = 15;
    public float lifeTime = 4f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 0.001f;
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveDirection * speed;
    }

    /// <summary>
    /// Gọi sau khi Instantiate để đặt hướng bay của đạn về phía player.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // Xoay sprite đạn theo hướng bay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Bỏ qua các collider của enemy khác
        if (hitInfo.CompareTag("Enemy")) return;

        // Gây sát thương cho player
        PlayerController player = hitInfo.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
