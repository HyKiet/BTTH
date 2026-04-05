using UnityEngine;
using UnityEditor;

public class FixWeaponSprites
{
    [MenuItem("Tools/Fix Weapons")]
    public static void Fix()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        WeaponManager wm = player.GetComponent<WeaponManager>();
        if (wm == null)
        {
            Debug.LogError("WeaponManager not found!");
            return;
        }

        // Load Sprites
        Sprite bullet1 = GetFirstSprite("Assets/Mad Doctor Assets/Sprites/Bullets/Bullet 1.png");
        Sprite bullet2 = GetFirstSprite("Assets/Mad Doctor Assets/Sprites/Bullets/Bullet 2.png");
        Sprite laser = GetFirstSprite("Assets/Mad Doctor Assets/Sprites/Laser/skeleton-animation_0.png");

        // We only need 3 weapons
        if (wm.weapons != null && wm.weapons.Count > 3)
        {
            wm.weapons.RemoveRange(3, wm.weapons.Count - 3);
        }

        if (wm.weapons.Count >= 1) wm.weapons[0].weaponIcon = bullet1;
        if (wm.weapons.Count >= 2) wm.weapons[1].weaponIcon = bullet2;
        if (wm.weapons.Count >= 3) wm.weapons[2].weaponIcon = laser;

        EditorUtility.SetDirty(wm);
        Debug.Log("Successfully fixed weapons! Kept 3 weapons and updated icons.");
    }

    static Sprite GetFirstSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }
        return null; // Fallback
    }
}
