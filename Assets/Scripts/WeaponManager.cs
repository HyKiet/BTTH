using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý vũ khí player — Lazy Loading Edition.
/// CHỈ load sprites của vũ khí đang dùng, giải phóng sprites cũ khi đổi vũ khí.
/// Fix lỗi Out of Memory từ việc serialize 280+ sprites vào Inspector.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponInfo
    {
        public string weaponName;
        public GameObject bulletPrefab;
        public Sprite weaponIcon;
        public float fireRate = 0.5f;

        // KHÔNG serialize sprite arrays — load runtime từ Resources
        [System.NonSerialized] public Sprite[] idleSprites;
        [System.NonSerialized] public Sprite[] walkSprites;
        [System.NonSerialized] public Sprite[] deathSprites;
    }

    public List<WeaponInfo> weapons = new List<WeaponInfo>();
    public Transform firePoint;

    private int currentWeaponIndex = 0;
    private float nextFireTime = 0f;
    private bool facingRight = true;
    private PlayerAnimation playerAnimation;
    private bool poolsRegistered = false;

    // ── Cache cho sprites đã load ──
    private Dictionary<int, bool> spritesLoaded = new Dictionary<int, bool>();

    // ── Quản lý Súng Điện Cầm Tay ──
    private GameObject activeBeamInstance;

    void Start()
    {
        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        playerAnimation = GetComponent<PlayerAnimation>();

        // Load sprites CHỈ cho vũ khí đầu tiên
        if (weapons.Count > 0)
        {
            LoadWeaponSprites(0);
            ApplyWeaponSprites(0);
            if (GameManager.Instance != null)
                GameManager.Instance.UpdateWeaponUI(weapons[0].weaponIcon);
        }

        RegisterBulletPools();
    }

    /// <summary>
    /// Load sprites từ Resources cho 1 vũ khí cụ thể.
    /// Giải phóng sprites của vũ khí trước đó.
    /// </summary>
    void LoadWeaponSprites(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        if (spritesLoaded.ContainsKey(index) && spritesLoaded[index]) return;

        string gunFolder = $"Gun{(index + 1):D2}";
        string basePath = $"Mad Doctor - Main Character/{gunFolder}";

        var weapon = weapons[index];

        // Load Idle sprites
        weapon.idleSprites = Resources.LoadAll<Sprite>($"{basePath}/Idle");

        // Load Walk sprites
        weapon.walkSprites = Resources.LoadAll<Sprite>($"{basePath}/Walk");

        // Load Death sprites
        weapon.deathSprites = Resources.LoadAll<Sprite>($"{basePath}/Death");

        spritesLoaded[index] = true;
    }

    /// <summary>
    /// Giải phóng sprites của vũ khí không dùng.
    /// </summary>
    void UnloadWeaponSprites(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        if (!spritesLoaded.ContainsKey(index) || !spritesLoaded[index]) return;

        var weapon = weapons[index];
        weapon.idleSprites = null;
        weapon.walkSprites = null;
        weapon.deathSprites = null;
        spritesLoaded[index] = false;
        // Đã xóa UnloadUnusedAssets vì gây lag (stutter) khi dọn rác System
    }

    void RegisterBulletPools()
    {
        if (poolsRegistered || ObjectPool.Instance == null) return;

        HashSet<string> registered = new HashSet<string>();
        foreach (var w in weapons)
        {
            if (w.bulletPrefab == null) continue;
            string tag = "Bullet_" + w.bulletPrefab.name;
            if (!registered.Contains(tag) && !ObjectPool.Instance.HasPool(tag))
            {
                ObjectPool.Instance.RegisterPool(tag, w.bulletPrefab, 10, 40);
                registered.Add(tag);
            }
        }

        poolsRegistered = true;
    }

    void ApplyWeaponSprites(int index)
    {
        if (playerAnimation == null) return;
        var w = weapons[index];

        if (w.idleSprites != null && w.idleSprites.Length > 0)
            playerAnimation.idleSprites = w.idleSprites;
        if (w.walkSprites != null && w.walkSprites.Length > 0)
            playerAnimation.walkSprites = w.walkSprites;
        if (w.deathSprites != null && w.deathSprites.Length > 0)
            playerAnimation.deathSprites = w.deathSprites;
    }

    public void TriggerShoot(bool isPressed, bool isHeld)
    {
        if (weapons.Count == 0 || firePoint == null) return;
        var weapon = weapons[currentWeaponIndex];
        
        bool isBeamWeapon = weapon.weaponName.ToLower().Contains("electric") || weapon.weaponName.ToLower().Contains("laser");

        if (isBeamWeapon)
        {
            // Đảm bảo tạo ra chùm sét dính trên nòng súng 1 lần duy nhất
            if (activeBeamInstance == null && weapon.bulletPrefab != null)
            {
                activeBeamInstance = Instantiate(weapon.bulletPrefab, firePoint.position, firePoint.rotation, firePoint);
                
                // Dời tia laser lùi về phía trươc nòng súng (căn theo viền trái thay vì tâm)
                SpriteRenderer sr = activeBeamInstance.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float width = sr.sprite.bounds.size.x;
                    // Đưa tia sét xích về phía trước đúng bằng nửa chiều dài của nó
                    activeBeamInstance.transform.localPosition = new Vector3(width / 2f, 0, 0);
                }
            }

            if (activeBeamInstance != null)
            {
                // Bật/tắt tùy theo việc có đè nút hay không
                if (activeBeamInstance.activeSelf != isHeld)
                {
                    activeBeamInstance.SetActive(isHeld);
                    if (isHeld && AudioManager.Instance != null) 
                        AudioManager.Instance.PlayShoot(); 
                }

                // KHÔNG càn lật Scale thủ công nữa vì PlayerScale đổi âm dương đã tự lật Child!
            }
        }
        else
        {
            // Nếu đổi qua súng lục mà quên tắt beam thì tắt đi
            if (activeBeamInstance != null) activeBeamInstance.SetActive(false);

            // Bắn súng cũ (Lặp theo FireRate nếu đè, hoặc bấm 1 phát)
            if (isHeld && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + weapon.fireRate;
            }
        }
    }

    void Shoot()
    {
        var weapon = weapons[currentWeaponIndex];
        if (weapon.bulletPrefab == null) return;

        string poolTag = "Bullet_" + weapon.bulletPrefab.name;

        GameObject bullet = null;
        if (ObjectPool.Instance != null && ObjectPool.Instance.HasPool(poolTag))
            bullet = ObjectPool.Instance.Get(poolTag, firePoint.position, firePoint.rotation);

        if (bullet == null)
            bullet = Instantiate(weapon.bulletPrefab, firePoint.position, firePoint.rotation);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            bulletScript.SetDirection(direction);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
    }

    public void SwitchWeapon()
    {
        if (weapons.Count <= 1) return;

        int oldIndex = currentWeaponIndex;

        currentWeaponIndex++;
        if (currentWeaponIndex >= weapons.Count) currentWeaponIndex = 0;

        // Tắt sét chớp nếu đang đè nút mà bấm đổi súng
        if (activeBeamInstance != null) activeBeamInstance.SetActive(false);

        // Unload sprites cũ → giải phóng bộ nhớ
        UnloadWeaponSprites(oldIndex);

        // Load sprites mới
        LoadWeaponSprites(currentWeaponIndex);
        ApplyWeaponSprites(currentWeaponIndex);

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateWeaponUI(weapons[currentWeaponIndex].weaponIcon);
    }

    public void UpdateWeaponDirection(bool isFacingRight) => facingRight = isFacingRight;

    public WeaponInfo GetCurrentWeapon()
    {
        if (weapons.Count > 0) return weapons[currentWeaponIndex];
        return null;
    }
}