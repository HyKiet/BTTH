using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    public float lifeTime = 3f;
    
    [Tooltip("Bán kính detect va chạm thủ công (backup cho trigger)")]
    public float hitRadius = 0.3f;
    
    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Collider2D myCollider;
    private bool hasHit = false;

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
        
        myCollider = GetComponent<Collider2D>();
        if (myCollider != null) myCollider.isTrigger = true;

        // Bỏ qua collision với Player để đạn không tự bắn chính mình
        IgnorePlayerCollision();

        Destroy(gameObject, lifeTime);
    }

    void IgnorePlayerCollision()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            Collider2D playerCol = playerObj.GetComponent<Collider2D>();
            if (playerCol != null && myCollider != null)
            {
                Physics2D.IgnoreCollision(myCollider, playerCol);
            }
        }
    }

    void FixedUpdate()
    {
        if (hasHit) return;
        
        if (rb != null)
            rb.linearVelocity = moveDirection * speed;
    }

    void Update()
    {
        if (hasHit || isEnemyBullet) return;
        
        // ═══ BACKUP: Raycast + Overlap detection ═══
        // Dùng OverlapCircle để detect va chạm thủ công, phòng trường hợp
        // OnTriggerEnter2D không fire (do physics timing, fast moving, etc.)
        DetectEnemyManually();
    }

    void DetectEnemyManually()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        foreach (var hit in hits)
        {
            if (hit == myCollider) continue; // Bỏ qua chính mình
            if (hit.GetComponent<Bullet>() != null || hit.GetComponent<EnemyBullet>() != null) continue;
            if (hit.GetComponent<PlayerController>() != null) continue;

            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                Debug.Log($"[Bullet] OverlapCircle HIT EnemyController '{hit.name}'! Dmg={damage}");
                ApplyDamageToEnemy(enemy);
                return;
            }

            EnemyRangedController ranged = hit.GetComponent<EnemyRangedController>();
            if (ranged == null) ranged = hit.GetComponentInParent<EnemyRangedController>();
            if (ranged != null)
            {
                Debug.Log($"[Bullet] OverlapCircle HIT EnemyRangedController '{hit.name}'! Dmg={damage}");
                ApplyDamageToRanged(ranged);
                return;
            }
        }
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        Vector3 scaler = transform.localScale;
        if (moveDirection.x < 0)
        {
             scaler.y = -Mathf.Abs(scaler.y);
             scaler.x = Mathf.Abs(scaler.x);
        }
        else
        {
             scaler.y = Mathf.Abs(scaler.y);
             scaler.x = Mathf.Abs(scaler.x);
        }
        transform.localScale = scaler;
    }

    [HideInInspector] public bool isEnemyBullet = false;

    // ── Trigger detection ────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hasHit) return;
        Debug.Log($"[Bullet] OnTriggerEnter2D: '{hitInfo.gameObject.name}' tag='{hitInfo.tag}'");
        HandleHit(hitInfo.gameObject);
    }

    // ── Collision detection (fallback) ──
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        Debug.Log($"[Bullet] OnCollisionEnter2D: '{collision.gameObject.name}' tag='{collision.gameObject.tag}'");
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject hitObject)
    {
        // Bỏ qua đạn khác
        if (hitObject.GetComponent<Bullet>() != null || hitObject.GetComponent<EnemyBullet>() != null)
            return;

        if (isEnemyBullet)
        {
            if (hitObject.CompareTag("Enemy")) return;
            
            PlayerController player = hitObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                DestroyBullet();
            }
            return;
        }

        // ── Đạn player: check enemy ──
        EnemyController enemy = hitObject.GetComponent<EnemyController>();
        if (enemy == null) enemy = hitObject.GetComponentInParent<EnemyController>();
        if (enemy == null) enemy = hitObject.GetComponentInChildren<EnemyController>();
        if (enemy != null)
        {
            Debug.Log($"[Bullet] Trigger HIT EnemyController '{hitObject.name}'! Dmg={damage}");
            ApplyDamageToEnemy(enemy);
            return;
        }
        
        EnemyRangedController rangedEnemy = hitObject.GetComponent<EnemyRangedController>();
        if (rangedEnemy == null) rangedEnemy = hitObject.GetComponentInParent<EnemyRangedController>();
        if (rangedEnemy == null) rangedEnemy = hitObject.GetComponentInChildren<EnemyRangedController>();
        if (rangedEnemy != null)
        {
            Debug.Log($"[Bullet] Trigger HIT EnemyRangedController '{hitObject.name}'! Dmg={damage}");
            ApplyDamageToRanged(rangedEnemy);
            return;
        }

        // Nếu chạm player → bỏ qua
        if (hitObject.GetComponent<PlayerController>() != null) return;
    }

    void ApplyDamageToEnemy(EnemyController enemy)
    {
        if (hasHit) return;
        hasHit = true;
        enemy.TakeDamage(damage);
        DestroyBullet();
    }

    void ApplyDamageToRanged(EnemyRangedController enemy)
    {
        if (hasHit) return;
        hasHit = true;
        enemy.TakeDamage(damage);
        DestroyBullet();
    }

    void DestroyBullet()
    {
        // Tắt visual + collider ngay lập tức
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        if (myCollider != null) myCollider.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        Destroy(gameObject, 0.05f);
    }
}