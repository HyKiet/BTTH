using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameSetupTools : Editor
{
    [MenuItem("Tools/Regenerate Enemy Prefabs")]
    public static void RegenerateEnemyPrefabs()
    {
        GameObject temp = new GameObject("TempEnemySetup");
        EnemySetup setup = temp.AddComponent<EnemySetup>();
        setup.CreateEnemyPrefabs();
        DestroyImmediate(temp);
        Debug.Log("[GameSetupTools] Enemy prefabs regenerated!");
    }

    [MenuItem("Tools/Load Player Death Sprites")]
    public static void LoadPlayerDeathSprites()
    {
        // Tìm Player trong scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("[GameSetupTools] Không tìm thấy Player trong scene!");
            return;
        }

        WeaponManager wm = playerObj.GetComponent<WeaponManager>();
        if (wm == null)
        {
            Debug.LogError("[GameSetupTools] Player không có WeaponManager!");
            return;
        }

        string basePath = "Assets/Mad Doctor Assets/Sprites/Mad Doctor - Main Character";

        for (int i = 0; i < wm.weapons.Count; i++)
        {
            var weapon = wm.weapons[i];
            string gunFolder = $"Gun{(i + 1):D2}"; // Gun01, Gun02, ...
            string deathPath = $"{basePath}/{gunFolder}/Death";

            // Load death sprites
            var deathSprites = new List<Sprite>();
            for (int f = 0; f <= 50; f++) // Max 50 frames
            {
                string spritePath = $"{deathPath}/Death_{f:D2}.png";
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (s != null) deathSprites.Add(s);
            }

            if (deathSprites.Count > 0)
            {
                weapon.deathSprites = deathSprites.ToArray();
                Debug.Log($"[GameSetupTools] ✅ {weapon.weaponName}: Loaded {deathSprites.Count} death sprites from {gunFolder}");
            }
            else
            {
                Debug.LogWarning($"[GameSetupTools] ⚠️ {weapon.weaponName}: No death sprites found in {deathPath}");
            }
        }

        // Cũng load vào PlayerAnimation mặc định (weapon đầu tiên)
        PlayerAnimation pa = playerObj.GetComponent<PlayerAnimation>();
        if (pa != null && wm.weapons.Count > 0 && wm.weapons[0].deathSprites != null)
        {
            pa.deathSprites = wm.weapons[0].deathSprites;
        }

        EditorUtility.SetDirty(wm);
        EditorUtility.SetDirty(playerObj);
        
        Debug.Log("[GameSetupTools] ✅ Player death sprites loaded for all weapons!");
    }
}
