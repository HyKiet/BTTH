using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy AI cận chiến — tối ưu bộ nhớ với Object Pool.
/// Cache tất cả components, dùng pool thay Destroy.
/// </summary>
public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public int maxHealth = 60;
    public int damageToPlayer = 10;

    [Header("Combat Options")]
    public float attackRange = 0.8f;
    public bool isRanged = false;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;

    [HideInInspector] public int currentHealth;
    private Transform player;
    private bool isFacingLeft = true;

    [Header("Electric Combat Animation")]
    public Sprite[] electricHitSprites;

    // ── Cached Components ──
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Color originalColor;
    private bool isDead = false;

    private float nextAttackTime = 0f;
    public float attackCooldown = 1.25f;

    // ── Electric Stun State ──
    private float electricStunTimer = 0f;
    private int electricFrameIndex = 0;
    private float electricAnimTimer = 0f;

    // ── Pool ──
    [HideInInspector] public string poolTag; // Được WaveManager gán khi spawn

    // ── Giá trị gốc (để reset khi reuse từ pool) ──
    private float baseMaxHealth;
    private float baseMoveSpeed;

    void Awake()
    {
        // Cache components 1 lần duy nhất
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (sr != null) originalColor = sr.color;

        baseMaxHealth = maxHealth;
        baseMoveSpeed = moveSpeed;

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");
    }

    void OnEnable()
    {
        // Reset state khi lấy từ pool
        isDead = false;
        currentHealth = maxHealth;
        nextAttackTime = 0f;
        isFacingLeft = true;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true; // Xóa bỏ va chạm vật lý thô cứng (đẩy nhau)
        }
        if (sr != null) sr.color = originalColor;

        if (attackRange < 2.5f) attackRange = 2.5f;

        // Tìm player (cache static)
        FindPlayer();
    }

    // ── Cache player reference static ──
    private static Transform _cachedPlayer;
    private static bool _playerSearched = false;

    void FindPlayer()
    {
        if (_cachedPlayer != null && _cachedPlayer.gameObject.activeInHierarchy)
        {
            player = _cachedPlayer;
            return;
        }

        _playerSearched = false;
        if (!_playerSearched)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                _cachedPlayer = playerObj.transform;
                player = _cachedPlayer;
            }
            _playerSearched = true;
        }
    }

    void Update()
    {
        if (isDead) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        // Xử lý Stun điện giật
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
            
            // Phục hồi Animator ngay khi tắt hiệu ứng Stun
            if (electricStunTimer <= 0)
            {
                if (anim != null && !anim.enabled) anim.enabled = true;
                if (sr != null) sr.color = originalColor;
            }
            
            return; // Khóa di chuyển và các logic khác khi bị giật
        }

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (anim != null) anim.SetBool("isWalking", false);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        Vector2 direction = (player.position - transform.position).normalized;

        if (distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
            if (anim != null) anim.SetBool("isWalking", true);
        }
        else
        {
            if (anim != null) anim.SetBool("isWalking", false);

            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        // Flip sprite
        if (direction.x > 0 && isFacingLeft)
        {
            isFacingLeft = false;
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (direction.x < 0 && !isFacingLeft)
        {
            isFacingLeft = true;
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void AttackPlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (anim != null) anim.SetTrigger("Attack");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPunch();

        if (isRanged)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

            if (projectilePrefab != null)
            {
                // Dùng pool nếu có
                GameObject bullet = null;
                if (ObjectPool.Instance != null && ObjectPool.Instance.HasPool(EnemyBullet.POOL_TAG))
                    bullet = ObjectPool.Instance.Get(EnemyBullet.POOL_TAG, spawnPos, Quaternion.identity);

                if (bullet == null)
                    bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

                Vector2 shootDir = isFacingLeft ? Vector2.left : Vector2.right;

                EnemyBullet enemyBulletScript = bullet.GetComponent<EnemyBullet>();
                if (enemyBulletScript != null)
                {
                    enemyBulletScript.SetDirection(shootDir);
                }
                else
                {
                    Bullet bulletScript = bullet.GetComponent<Bullet>();
                    if (bulletScript != null)
                    {
                        bulletScript.isEnemyBullet = true;
                        bulletScript.SetDirection(shootDir);
                    }
                }
            }
            else
            {
                // Melee fallback
                DealMeleeDamage();
            }
        }
        else
        {
            DealMeleeDamage();
        }
    }

    void DealMeleeDamage()
    {
        if (player == null) return;
        PlayerController playerScript = player.GetComponent<PlayerController>();
        if (playerScript != null) playerScript.TakeDamage(damageToPlayer);
    }

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
        
        // Quái vật sẽ bị tê liệt 0.3s. (Tickrate của lazer là 0.25s, nên bắn trúng liên tục là tê liệt vĩnh viễn)
        electricStunTimer = 0.3f;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator HitFlash()
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
            // Chờ animation rồi return về pool
            StartCoroutine(DelayedReturn(1.5f));
        }
        else
        {
            StartCoroutine(DeathFade());
        }
    }

    IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    IEnumerator DeathFade()
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
        // Reset visual trước khi trả về pool
        if (sr != null) sr.color = originalColor;

        // Reset health về base (WaveManager sẽ scale lại khi spawn)
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
}