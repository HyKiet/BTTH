using UnityEngine;

/// <summary>
/// Script chạy một lần trong Editor để gán 10 bộ vũ khí (KHÔNG gán sprites).
/// Sprites sẽ được WeaponManager lazy-load từ Resources lúc runtime.
/// Gắn vào Player và chọn "Setup All 10 Weapons" từ context menu.
/// </summary>
public class WeaponSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Setup All 10 Weapons")]
    public void SetupAllWeapons()
    {
        WeaponManager wm = GetComponent<WeaponManager>();
        if (wm == null) { Debug.LogError("WeaponManager not found!"); return; }

        string bulletBasePath = "Assets/Mad Doctor Assets/Sprites/Bullets";

        // Bullet prefabs
        var bulletPaths = new string[]
        {
            "Assets/Mad Doctor Assets/Prefabs/Bullet_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_2.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_2.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_2.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_2.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Bullet_2.prefab",
        };

        float[] fireRates = { 0.5f, 0.3f, 0.4f, 0.25f, 0.6f, 0.2f, 0.45f, 0.35f, 0.55f, 0.15f };
        string[] weaponNames = {
            "Pistol Mark I", "Plasma Cannon", "Rifle Mark II", "Rapid Fire",
            "Shotgun", "Mini Gun", "Laser Blaster", "Burst Rifle",
            "Heavy Canon", "Death Ray"
        };

        string[] bulletIcons = {
            "Bullet 1.png", "Bullet 2.png", "Bullet 3.png", "Bullet 4.png", "Bullet 5.png",
            "Bullet 6.png", "Bullet 7.png", "Bullet 8.png", "Bullet 9.png", "Bullet 10.png"
        };

        wm.weapons.Clear();

        for (int gunIdx = 1; gunIdx <= 10; gunIdx++)
        {
            var weapon = new WeaponManager.WeaponInfo();
            weapon.weaponName = weaponNames[gunIdx - 1];
            weapon.fireRate = fireRates[gunIdx - 1];

            // Bullet prefab
            weapon.bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bulletPaths[gunIdx - 1]);

            // Weapon icon (bullet sprite) — chỉ 1 sprite nhỏ cho icon
            weapon.weaponIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{bulletBasePath}/{bulletIcons[gunIdx - 1]}");

            // KHÔNG gán idleSprites/walkSprites/deathSprites ở đây!
            // WeaponManager sẽ lazy-load từ Resources khi cần.
            // Điều này GIẢM ~90% memory vì không serialize 280+ sprites vào scene.

            wm.weapons.Add(weapon);
            Debug.Log($"[{weapon.weaponName}] Configured (sprites will lazy-load from Resources).");
        }

        UnityEditor.EditorUtility.SetDirty(wm);
        Debug.Log("All 10 weapons configured on WeaponManager! Sprites will load from Resources at runtime.");
    }
#endif
}
