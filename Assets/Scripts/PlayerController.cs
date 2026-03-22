using UnityEngine;

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
    public SpriteRenderer mapBackground;
    private float minX, maxX, minY, maxY;

    private WeaponManager weaponManager;
    private PlayerAnimation playerAnim;

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
        currentHealth = maxHealth;

        if (mapBackground == null)
        {
            GameObject bgObj = GameObject.Find("Background");
            if (bgObj != null)
                mapBackground = bgObj.GetComponent<SpriteRenderer>();
        }

        if (mapBackground != null)
        {
            float playerWidth = GetComponent<SpriteRenderer>().bounds.extents.x;
            float playerHeight = GetComponent<SpriteRenderer>().bounds.extents.y;

            minX = mapBackground.bounds.min.x + playerWidth;
            maxX = mapBackground.bounds.max.x - playerWidth;
            minY = mapBackground.bounds.min.y + playerHeight;
            maxY = mapBackground.bounds.max.y - playerHeight;
        }
        else
        {
            minX = -8.5f; maxX = 8.5f;
            minY = -4.5f; maxY = 4.5f;
        }
    }

    void Update()
    {
        if (isDead) return;

        movement = Vector2.zero;
        bool shootPressed = false;
        bool switchWeaponPressed = false;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) movement.x -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) movement.x += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) movement.y -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) movement.y += 1f;

            shootPressed = UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame;
            switchWeaponPressed = UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame;
        }

        if (movement.x > 0 && !isFacingRight)
            Flip();
        else if (movement.x < 0 && isFacingRight)
            Flip();

        if (shootPressed)
        {
            if (weaponManager != null) weaponManager.TriggerShoot();
        }

        if (switchWeaponPressed)
        {
            if (weaponManager != null) weaponManager.SwitchWeapon();
        }
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
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        
        // Tắt input & movement
        rb.linearVelocity = Vector2.zero;
        
        // Tắt weapon
        if (weaponManager != null) weaponManager.enabled = false;
        
        // Tắt collider để enemy không còn tấn công
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // ═══ Phát animation chết ═══
        if (playerAnim != null && playerAnim.deathSprites != null && playerAnim.deathSprites.Length > 0)
        {
            playerAnim.PlayDeath(() =>
            {
                // Callback khi animation chết xong
                OnDeathAnimationComplete();
            });
        }
        else
        {
            // Không có death sprites → xử lý ngay
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            OnDeathAnimationComplete();
        }
    }

    void OnDeathAnimationComplete()
    {
        // Hiện Game Over UI
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}