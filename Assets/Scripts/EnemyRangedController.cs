using UnityEngine;

/// <summary>
/// Enemy AI dạng Ranged: đuổi player đến tầm stopRange, rồi đứng yên bắn đạn.
/// Tối ưu: cache components, dùng Object Pool.
/// </summary>
public class EnemyRangedController : MonoBehaviour
{
    // ─── Stats ─────────────────────────────────────────────────────
    [Header("Stats")]
    public int maxHealth = 40;
    public float moveSpeed = 2.5f;

    // ─── Ranged Settings ───────────────────────────────────────────
    [Header("Ranged Settings")]
    [Tooltip("Khoảng cách dừng lại để bắn (units)")]
    public float stopRange = 5f;
    [Tooltip("Thời gian giữa 2 lần bắn (giây)")]
    public float fireRate = 1.5f;
    [Tooltip("Prefab đạn EnemyBullet")]
    public GameObject bulletPrefab;
    [Tooltip("Điểm xuất phát đạn (child object)")]
    public Transform firePoint;

    [Header("Melee Fallback (va chạm)")]
    public int contactDamage = 5;

    // ─── Internal ──────────────────────────────────────────────────
    private int currentHealth;
    private Transform player;
    private bool isFacingRight = true;
    private float nextFireTime = 0f;
    private bool isDead = false;

    [Header("Electric Combat Animation")]
    public Sprite[] electricHitSprites;                     

    // ── Electric Stun State ──
    private float electricStunTimer = 0f;
    private int electricFrameIndex = 0;
    private float electricAnimTimer = 0f;

    // ── Cached Components ──
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Color originalColor;

    // ── Pool ──
    [HideInInspector] public string poolTag;

    // ── Base values ──
    private float baseMaxHealth;
    private float baseMoveSpeed;

    private enum State { Chase, Shoot }
    private State currentState = State.Chase;

    void Awake()
    {
        // Cache components 1 lần
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (sr != null) originalColor = sr.color;
        baseMaxHealth = maxHealth;
        baseMoveSpeed = moveSpeed;

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");
        if (firePoint == null)
            firePoint = transform;
    }

    void OnEnable()
    {
        // Reset state khi lấy từ pool
        isDead = false;
        currentHealth = maxHealth;
        nextFireTime = 0f;
        currentState = State.Chase;
        isFacingRight = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        if (col != null) col.enabled = true;
        if (sr != null) sr.color = originalColor;

        FindPlayer();
    }

    // ── Cache player static ──
    private static Transform _cachedPlayer;

    void FindPlayer()
    {
        if (_cachedPlayer != null && _cachedPlayer.gameObject.activeInHierarchy)
        {
            player = _cachedPlayer;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            _cachedPlayer = playerObj.transform;
            player = _cachedPlayer;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        // Xử lý Stun điện giật (Tê liệt hoàn toàn)
        if (electricStunTimer > 0)
        {
            electricStunTimer -= Time.deltaTime;
            
            if (anim != null && anim.enabled) anim.enabled = false;
            
            if (electricHitSprites != null && electricHitSprites.Length > 0 && sr != null)
            {
                electricAnimTimer -= Time.deltaTime;
                if (electricAnimTimer <= 0)
                {
                    electricFrameIndex = (electricFrameIndex + 1) % electricHitSprites.Length;
                    sr.sprite = electricHitSprites[electricFrameIndex];
                    electricAnimTimer = 0.05f; 
                }
            }
            
            if (electricStunTimer <= 0)
            {
                if (anim != null && !anim.enabled) anim.enabled = true;
                if (sr != null) sr.color = originalColor;
            }
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        currentState = (dist <= stopRange) ? State.Shoot : State.Chase;

        switch (currentState)
        {
            case State.Chase:
                ChasePlayer();
                if (anim != null) anim.SetBool("isWalking", true);
                break;
            case State.Shoot:
                ShootAtPlayer();
                if (anim != null) anim.SetBool("isWalking", false);
                break;
        }

        FlipTowardPlayer();
    }

    void ChasePlayer()
    {
        transform.position = Vector2.MoveTowards(
            transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    void ShootAtPlayer()
    {
        if (Time.time >= nextFireTime)
        {
            FireBullet();
            nextFireTime = Time.time + fireRate;
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;

        if (anim != null) anim.SetTrigger("Attack");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();

        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

        // Dùng pool nếu có
        GameObject bullet = null;
        if (ObjectPool.Instance != null && ObjectPool.Instance.HasPool(EnemyBullet.POOL_TAG))
            bullet = ObjectPool.Instance.Get(EnemyBullet.POOL_TAG, firePoint.position, Quaternion.identity);

        if (bullet == null)
            bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
            bulletScript.SetDirection(direction);
    }

    void FlipTowardPlayer()
    {
        if (player == null) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    // ─── Health ────────────────────────────────────────────────────
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        DamageNumber.Spawn(transform.position, damage);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (anim != null) anim.SetTrigger("GetHit");
            StartCoroutine(HitFlash());
        }
    }

    public void TakeElectricDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        DamageNumber.Spawn(transform.position, damage);
        
        electricStunTimer = 0.3f;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (sr != null && !isDead) sr.color = originalColor;
        }
    }

    void Die()
    {
        isDead = true;

        if (col != null) col.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDeath();

        if (PotionManager.Instance != null)
            PotionManager.Instance.TryDropPotion(transform.position);

        if (anim != null)
        {
            anim.SetTrigger("Die");
            anim.SetBool("isWalking", false);
            StartCoroutine(DelayedReturn(1.5f));
        }
        else
        {
            StartCoroutine(DeathFade());
        }
    }

    System.Collections.IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    System.Collections.IEnumerator DeathFade()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Color startColor = sr != null ? sr.color : Color.white;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sr != null)
                sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.3f, t);
            yield return null;
        }
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (sr != null) sr.color = originalColor;
        maxHealth = Mathf.RoundToInt(baseMaxHealth);
        moveSpeed = baseMoveSpeed;

        if (ObjectPool.Instance != null && !string.IsNullOrEmpty(poolTag))
            ObjectPool.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Áp dụng difficulty scaling (gọi từ WaveManager)
    /// </summary>
    public void ApplyScaling(float hpMult, float speedMult)
    {
        maxHealth = Mathf.RoundToInt(baseMaxHealth * hpMult);
        currentHealth = maxHealth;
        moveSpeed = baseMoveSpeed * speedMult;
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (isDead) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (coll.gameObject.CompareTag("Player"))
        {
            PlayerController p = coll.gameObject.GetComponent<PlayerController>();
            if (p != null) p.TakeDamage(contactDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRange);
    }
}
