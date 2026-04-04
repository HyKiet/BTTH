#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EnemyElectricitySetup
{
    [MenuItem("Tools/Mad Doctor/Setup Enemy Electric")]
    public static void SetupElectricSprites()
    {
        Debug.Log("== BẮT ĐẦU CẤY ẢNH ĐIỆN VÀO QUÁI VẬT ==");

        string baseEnemyPath = "Assets/Mad Doctor Assets/Prefabs";
        string baseSpritePath = "Assets/Mad Doctor Assets/Sprites/Enemy";
        
        // Quái vật có định dạng Enemy_1, Enemy02... Enemy06
        string[] enemyNames = new string[] { "Enemy_1", "Enemy02", "Enemy03", "Enemy04", "Enemy05", "Enemy06" };
        string[] folderNames = new string[] { "Enemy Character01", "Enemy Character02", "Enemy Character03", "Enemy Character04", "Enemy Character05", "Enemy Character06" };

        for (int i = 0; i < enemyNames.Length; i++)
        {
            string prefabPath = $"{baseEnemyPath}/{enemyNames[i]}.prefab";
            if (!System.IO.File.Exists(prefabPath))
            {
                prefabPath = $"{baseEnemyPath}/Bullet Prefabs/{enemyNames[i]}.prefab";
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Electric tool] Không tìm thấy Prefab {enemyNames[i]} tại {prefabPath}");
                continue;
            }

            // Load 3 frame ảnh
            Sprite[] frames = new Sprite[3];
            bool foundAll = true;
            for (int k = 0; k < 3; k++)
            {
                string spritePath = $"{baseSpritePath}/{folderNames[i]}/Get Electric/Get Electric_{k}.png";
                frames[k] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (frames[k] == null)
                {
                    Debug.LogWarning($"[Electric tool] Mất ảnh sét của {folderNames[i]} tại {spritePath}");
                    foundAll = false;
                }
            }

            if (!foundAll) continue;

            // Bơm mảng hình vào Script
            bool isModified = false;

            // Xử lý cận chiến
            System.Type ecType = System.Type.GetType("EnemyController, Assembly-CSharp");
            if (ecType != null)
            {
                Component ec = prefab.GetComponent(ecType);
                if (ec != null)
                {
                    var field = ecType.GetField("electricHitSprites");
                    if (field != null)
                    {
                        field.SetValue(ec, frames);
                        isModified = true;
                    }
                }
            }

            // Xử lý tầm xa
            System.Type ercType = System.Type.GetType("EnemyRangedController, Assembly-CSharp");
            if (ercType != null)
            {
                Component erc = prefab.GetComponent(ercType);
                if (erc != null)
                {
                    var field = ercType.GetField("electricHitSprites");
                    if (field != null)
                    {
                        field.SetValue(erc, frames);
                        isModified = true;
                    }
                }
            }

            if (isModified)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"✔ Đã tiêm thành công 3 ảnh điện vào {prefab.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("== HOÀN TẤT THIẾT LẬP! ==");
    }
}
#endif
