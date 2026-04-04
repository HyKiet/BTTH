using UnityEngine;

/// <summary>
/// Player Controller — movement, health, damage, death.
/// Tối ưu: cache SpriteRenderer, dùng cached WaitForSeconds.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isFacingRight = true;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Map Boundaries")]
    [Tooltip("Y position of the bottom walkable edge (floor)")]
    public float boundaryMinY = -4.5f;
    [Tooltip("Y position of the top walkable edge (ceiling)")]
    public float boundaryMaxY = -1.5f;
    [Tooltip("X position of the left boundary")]
    public float boundaryMinX = -50f;
    [Tooltip("X position of the right boundary")]
    public float boundaryMaxX = 50f;
    private float minX, maxX, minY, maxY;

    // ── Cached Components ──
    private WeaponManager weaponManager;
    private PlayerAnimation playerAnim;
    private SpriteRenderer sr;
    private Collider2D col;

    // ── Cached WaitForSeconds ──
    private static readonly WaitForSeconds hitFlashWait = new WaitForSeconds(0.15f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0f;
        }
        weaponManager = GetComponent<WeaponManager>();
        playerAnim = GetComponent<PlayerAnimation>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        // Khóa cứng giá trị để cho dù sếp quên chỉnh trên Editor thì Code vẫn auto sửa lại
        // Mở rộng đường đi cho vừa mép mép hình nền
        boundaryMinY = -3.8f;   // Chân đường mấp mé dưới cùng màn hình
        boundaryMaxY = 0.0f;    // Sát vạch kẻ vỉa hè phía trên

        // Giới hạn trái/phải — vừa khít vùng chơi (3 tile sàn, mỗi tile ~32.6 units)
        boundaryMinX = -35f;
        boundaryMaxX = 35f;

        // Set movement boundaries
        float playerWidth = sr != null ? sr.bounds.extents.x : 0.5f;
        float playerHeight = sr != null ? sr.bounds.extents.y : 0.5f;
        minX = boundaryMinX + playerWidth;
        maxX = boundaryMaxX - playerWidth;
        minY = boundaryMinY + playerHeight;
        maxY = boundaryMaxY - playerHeight;
    }

    void Update()
    {
        if (isDead) return;

        movement = Vector2.zero;
        bool shootPressed = false;
        bool shootHeld = false;
        bool switchWeaponPressed = false;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) movement.x -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) movement.x += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) movement.y -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) movement.y += 1f;

            shootPressed = UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame;
            shootHeld = UnityEngine.InputSystem.Keyboard.current.kKey.isPressed;
            switchWeaponPressed = UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame;
        }

        if (movement.x > 0 && !isFacingRight) Flip();
        else if (movement.x < 0 && isFacingRight) Flip();

        if (weaponManager != null)
            weaponManager.TriggerShoot(shootPressed, shootHeld);

        if (switchWeaponPressed && weaponManager != null)
            weaponManager.SwitchWeapon();
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movement.normalized * moveSpeed;

        Vector2 clampedPosition = rb.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        rb.position = clampedPosition;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;

        if (weaponManager != null) weaponManager.UpdateWeaponDirection(isFacingRight);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (GameManager.Instance != null)
            GameManager.Instance.UpdateHP(currentHealth, maxHealth);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHit();

        StartCoroutine(HitFlash());

        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (GameManager.Instance != null)
            GameManager.Instance.UpdateHP(currentHealth, maxHealth);

        // Hiệu ứng nháy xanh khi hồi máu (tùy chọn)
        StartCoroutine(HealFlash());
    }

    System.Collections.IEnumerator HealFlash()
    {
        if (sr != null)
        {
            sr.color = Color.green;
            yield return hitFlashWait; // Dùng chung cache thời gian
            if (sr != null) sr.color = Color.white;
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        if (sr != null)
        {
            sr.color = Color.red;
            yield return hitFlashWait; // Cached WaitForSeconds
            if (sr != null) sr.color = Color.white;
        }
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (weaponManager != null) weaponManager.enabled = false;
        if (col != null) col.enabled = false;

        if (playerAnim != null && playerAnim.deathSprites != null && playerAnim.deathSprites.Length > 0)
        {
            playerAnim.PlayDeath(() => OnDeathAnimationComplete());
        }
        else
        {
            if (sr != null) sr.enabled = false;
            OnDeathAnimationComplete();
        }
    }

    void OnDeathAnimationComplete()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}