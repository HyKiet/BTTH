using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponInfo
    {
        public string weaponName;
        public GameObject bulletPrefab;
        public Sprite weaponIcon;
        public float fireRate = 0.5f;
        [Header("Animation Sprites")]
        public Sprite[] idleSprites;
        public Sprite[] walkSprites;
        public Sprite[] deathSprites;
    }

    public List<WeaponInfo> weapons = new List<WeaponInfo>();
    public Transform firePoint;
    
    private int currentWeaponIndex = 0;
    private float nextFireTime = 0f;
    private bool facingRight = true;
    private PlayerAnimation playerAnimation;

    void Start()
    {
        if (firePoint == null) 
            firePoint = transform.Find("FirePoint");

        playerAnimation = GetComponent<PlayerAnimation>();
        
        if (weapons.Count > 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.UpdateWeaponUI(weapons[currentWeaponIndex].weaponIcon);
            ApplyWeaponSprites(currentWeaponIndex);
        }
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

    public void TriggerShoot()
    {
        if (weapons.Count == 0 || firePoint == null) return;
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + weapons[currentWeaponIndex].fireRate;
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(weapons[currentWeaponIndex].bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            bulletScript.SetDirection(direction);
        }
        
        // Sound effect
        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
    }

    public void SwitchWeapon()
    {
        if (weapons.Count <= 1) return;

        currentWeaponIndex++;
        if (currentWeaponIndex >= weapons.Count) currentWeaponIndex = 0;
        
        ApplyWeaponSprites(currentWeaponIndex);

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateWeaponUI(weapons[currentWeaponIndex].weaponIcon);

        Debug.Log("Switched to: " + weapons[currentWeaponIndex].weaponName);
    }

    public void UpdateWeaponDirection(bool isFacingRight) => facingRight = isFacingRight;

    public WeaponInfo GetCurrentWeapon()
    {
        if (weapons.Count > 0) return weapons[currentWeaponIndex];
        return null;
    }
}