#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LaserWeaponSetup
{
    [MenuItem("Tools/Mad Doctor/Setup Laser Weapon")]
    public static void SetupElectricGun()
    {
        Debug.Log("== BẮT ĐẦU TẠO VŨ KHÍ SÚNG ĐIỆN (LASER/ELECTRIC GUN) ==");

        // 1. Load Animation Sprites
        Sprite[] laserSprites = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            string path = $"Assets/Mad Doctor Assets/Sprites/Laser/skeleton-animation_{i}.png";
            laserSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (laserSprites[i] == null)
            {
                Debug.LogError($"[LaserWeaponSetup] Lỗi: Không tìm thấy Sprite tia sét tại {path}!");
                return;
            }
        }
        
        // 2. Load Icon Súng (Do cấu trúc thư mục, ta tạm lấy Icon của Gun02 hoặc tạo null)
        Sprite gunIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mad Doctor Assets/Sprites/Mad Doctor - Main Character/Gun02/Idle/1.png");

        // 3. Tạo Base GameObject cho đạn
        GameObject laserObj = new GameObject("LaserBullet");
        
        // 4. Gắn các Components
        SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
        sr.sprite = laserSprites[0];
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5;

        BoxCollider2D col = laserObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.4f);

        // Bỏ xài Bullet, xài thẳng LaserBlast cho đúng cơ chế ép sát tường
        System.Type bulletType = System.Type.GetType("LaserBlast, Assembly-CSharp");
        if (bulletType != null)
        {
            Component bulletComp = laserObj.AddComponent(bulletType);
            var damageProp = bulletType.GetField("damagePerTick");
            var tickProp = bulletType.GetField("tickRate");
            if (damageProp != null) damageProp.SetValue(bulletComp, 25); // DMG 25 x 4 giật 1 giây = 100 DMG!!
            if (tickProp != null) tickProp.SetValue(bulletComp, 0.25f);
        }

        System.Type animType = System.Type.GetType("ProjectileAnimator, Assembly-CSharp");
        if (animType != null)
        {
            Component animComp = laserObj.AddComponent(animType);
            var framesProp = animType.GetField("animationFrames");
            var rateProp = animType.GetField("frameRate");
            if (framesProp != null) framesProp.SetValue(animComp, laserSprites);
            if (rateProp != null) rateProp.SetValue(animComp, 0.05f);
        }

        // 5. Lưu thành Prefab
        string prefabPath = "Assets/Mad Doctor Assets/Prefabs/LaserBullet.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(laserObj, prefabPath);
        Object.DestroyImmediate(laserObj); // Xóa rác khỏi scene

        // 6. Tiêm vũ khí học vào con Player
        GameObject player = GameObject.Find("Player") ?? GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            System.Type wmType = System.Type.GetType("WeaponManager, Assembly-CSharp");
            if (wmType != null)
            {
                Component wmComp = player.GetComponent(wmType);
                if (wmComp != null)
                {
                    var weaponsField = wmType.GetField("weapons");
                    if (weaponsField != null)
                    {
                        // Chúng ta đang làm việc với kiểu List của Unity thông qua Reflection
                        object weaponsListInfo = weaponsField.GetValue(wmComp);
                        if (weaponsListInfo is System.Collections.IList list)
                        {
                            // Tạo System.Object chứa instance mới của WeaponInfo
                            System.Type infoType = wmType.GetNestedType("WeaponInfo");
                            object newWeapon = System.Activator.CreateInstance(infoType);

                            // Gán dữ liệu cho Súng số 3 (Electric Gun)
                            infoType.GetField("weaponName").SetValue(newWeapon, "Electric Gun");
                            infoType.GetField("bulletPrefab").SetValue(newWeapon, prefab);
                            infoType.GetField("fireRate").SetValue(newWeapon, 0.3f);
                            if (gunIcon != null)
                                infoType.GetField("weaponIcon").SetValue(newWeapon, gunIcon);

                            // Nếu user chưa có 3 vũ khí, ta Add. Nếu có rồi, ta đè lên vũ khí thứ 3.
                            if (list.Count >= 3)
                            {
                                list[2] = newWeapon;
                                Debug.Log("[LaserWeaponSetup] Đã đè vũ khí số 3 thành Súng Điện!");
                            }
                            else
                            {
                                list.Add(newWeapon);
                                Debug.Log("[LaserWeaponSetup] Đã mở khóa vũ khí thứ " + list.Count + " ! Vừa tiêm Súng Điện vào tay Mad Doctor.");
                            }
                            
                            EditorUtility.SetDirty(wmComp);
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[LaserWeaponSetup] Không tìm thấy Player trên Map, hãy kéo Prefab Súng Điện vào WeaponManager một cách thủ công.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("== HOÀN TẤT THIẾT LẬP SÚNG ĐIỆN! ==");
    }
}
#endif
