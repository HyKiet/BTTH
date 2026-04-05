using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoFixWeapons
{
    static AutoFixWeapons()
    {
        EditorApplication.delayCall += Fix;
    }

    public static void Fix()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        WeaponManager wm = player.GetComponent<WeaponManager>();
        if (wm == null) return;

        bool changed = false;

        Sprite bullet1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mad Doctor Assets/Sprites/Bullets/Bullet 1.png");
        Sprite bullet2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mad Doctor Assets/Sprites/Bullets/Bullet 2.png");
        Sprite laser = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mad Doctor Assets/Sprites/Laser/skeleton-animation_0.png");

        if (wm.weapons.Count > 3)
        {
            wm.weapons.RemoveRange(3, wm.weapons.Count - 3);
            changed = true;
        }

        if (wm.weapons.Count >= 1 && wm.weapons[0].weaponIcon != bullet1) 
        { 
            wm.weapons[0].weaponIcon = bullet1; 
            changed = true; 
        }
        if (wm.weapons.Count >= 2 && wm.weapons[1].weaponIcon != bullet2) 
        { 
            wm.weapons[1].weaponIcon = bullet2; 
            changed = true; 
        }
        if (wm.weapons.Count >= 3 && wm.weapons[2].weaponIcon != laser) 
        { 
            wm.weapons[2].weaponIcon = laser; 
            changed = true; 
        }

        if (changed)
        {
            EditorUtility.SetDirty(wm);
            EditorSceneManager.MarkSceneDirty(wm.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("McpForUnity: Auto-fixed WeaponManager sprites and Saved Scene permanently!");
        }
    }
}
