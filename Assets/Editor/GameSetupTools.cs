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

    [MenuItem("Tools/Fix Build References (Âm thanh & Quái vật)")]
    public static void FixBuildReferences()
    {
        // 1. Fix AudioManager
        AudioManager am = Object.FindFirstObjectByType<AudioManager>();
        if (am != null)
        {
            string audioPath = "Assets/Mad Doctor Assets/Audio";
            am.bgMusic = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/BG Music.mp3");
            
            var shoots = new List<AudioClip>();
            for (int i = 1; i <= 6; i++) {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/Shoot{i}.wav");
                if (clip) shoots.Add(clip);
            }
            am.shootClips = shoots.ToArray();
            
            var hits = new List<AudioClip>();
            for (int i = 1; i <= 4; i++) {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/Hit{i}.wav");
                if (clip) hits.Add(clip);
            }
            am.hitClips = hits.ToArray();
            
            am.punchClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/punch.wav");
            am.punch2Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/Punch2.wav");
            am.laserClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/Laser.wav");
            am.bonusClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioPath}/Bonus.wav");
            
            EditorUtility.SetDirty(am);
            Debug.Log("[FixBuild] Đã nạp tất cả âm thanh cho AudioManager thành công!");
        }

        // 2. Fix WaveManager
        WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
        if (wm != null)
        {
            var m_list = new List<GameObject>();
            string[] m_paths = { "Enemy_1", "Enemy03", "Enemy05" };
            foreach (var name in m_paths) {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Mad Doctor Assets/Prefabs/{name}.prefab");
                if (go) m_list.Add(go);
            }
            wm.meleePrefabs = m_list.ToArray();

            var r_list = new List<GameObject>();
            string[] r_paths = { "Enemy02", "Enemy04", "Enemy06" };
            foreach (var name in r_paths) {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Mad Doctor Assets/Prefabs/{name}.prefab");
                if (go) r_list.Add(go);
            }
            wm.rangedPrefabs = r_list.ToArray();
            
            EditorUtility.SetDirty(wm);
            Debug.Log("[FixBuild] Đã nạp Prefab quái vật cho WaveManager thành công!");
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("✅ [HOÀN TẤT] Bạn hãy bấm 'Ctrl + S' để lưu Scene và có thể Build lại game!");
    }
}
