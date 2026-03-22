using UnityEngine;

/// <summary>
/// Script chạy một lần trong Editor để tạo 6 Enemy Prefab với Animator đầy đủ.
/// Gắn vào bất kỳ GameObject và chạy qua context menu.
/// </summary>
public class EnemySetup : MonoBehaviour
{
    [System.Serializable]
    public class EnemyConfig
    {
        public string enemyName;
        public string characterFolder; // "Enemy Character01", "Enemy Character02", ...
        public string controllerName;  // "Enemy_1_Controller", "Enemy02_Controller", ...
        public int health = 30;
        public float speed = 2f;
        public int damage = 10;
        public Color tintColor = Color.white;
        public bool isRanged = false;
        public float attackRange = 0.8f;
    }

    public EnemyConfig[] configs = new EnemyConfig[]
    {
        new EnemyConfig { 
            enemyName = "Enemy_1", characterFolder = "Enemy Character01", 
            controllerName = "Enemy_1_Controller",
            health = 30, speed = 2f, damage = 8, tintColor = Color.white,
            attackRange = 0.8f
        },
        new EnemyConfig { 
            enemyName = "Enemy02", characterFolder = "Enemy Character02", 
            controllerName = "Enemy02_Controller",
            health = 40, speed = 2.5f, damage = 12, tintColor = Color.white,
            attackRange = 0.8f
        },
        new EnemyConfig { 
            enemyName = "Enemy03", characterFolder = "Enemy Character03", 
            controllerName = "Enemy03_Controller",
            health = 60, speed = 2.0f, damage = 15, tintColor = new Color(1f, 0.8f, 0.8f),
            attackRange = 0.8f
        },
        new EnemyConfig { 
            enemyName = "Enemy04", characterFolder = "Enemy Character04", 
            controllerName = "Enemy04_Controller",
            health = 30, speed = 3.5f, damage = 10, tintColor = new Color(0.8f, 1f, 0.8f),
            attackRange = 0.8f
        },
        new EnemyConfig { 
            enemyName = "Enemy05", characterFolder = "Enemy Character05", 
            controllerName = "Enemy05_Controller",
            health = 80, speed = 1.5f, damage = 20, tintColor = new Color(0.8f, 0.8f, 1f),
            attackRange = 0.8f
        },
        new EnemyConfig { 
            enemyName = "Enemy06", characterFolder = "Enemy Character06", 
            controllerName = "Enemy06_Controller",
            health = 100, speed = 1.2f, damage = 25, tintColor = new Color(1f, 0.7f, 0.3f),
            attackRange = 0.8f
        },
    };

#if UNITY_EDITOR
    [ContextMenu("Create Enemy Prefabs")]
    public void CreateEnemyPrefabs()
    {
        string animBasePath = "Assets/Mad Doctor Assets/Animations/Enemy";
        string spriteBasePath = "Assets/Mad Doctor Assets/Sprites/Enemy";
        string prefabPath = "Assets/Mad Doctor Assets/Prefabs";

        // Đảm bảo thư mục Prefabs tồn tại
        if (!UnityEditor.AssetDatabase.IsValidFolder(prefabPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/Mad Doctor Assets", "Prefabs");
        }

        foreach (var cfg in configs)
        {
            // Tạo GameObject tạm thời
            GameObject enemy = new GameObject(cfg.enemyName);
            enemy.tag = "Enemy";

            // SpriteRenderer
            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            // Load Idle sprite đầu tiên làm sprite mặc định
            Sprite idleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{spriteBasePath}/{cfg.characterFolder}/Idle/Idle_00.png");
            if (idleSprite != null) sr.sprite = idleSprite;
            sr.color = cfg.tintColor;

            // Rigidbody2D
            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // BoxCollider2D
            BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();

            // ══════════ ANIMATOR (THAY THẾ PlayerAnimation) ══════════
            Animator animator = enemy.AddComponent<Animator>();
            // Load AnimatorController từ Animations folder
            string controllerPath = $"{animBasePath}/{cfg.characterFolder}/{cfg.controllerName}.controller";
            RuntimeAnimatorController controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log($"[EnemySetup] ✅ Assigned AnimatorController: {controllerPath}");
            }
            else
            {
                Debug.LogWarning($"[EnemySetup] ⚠️ AnimatorController not found: {controllerPath}");
            }

            // EnemyController
            EnemyController ec = enemy.AddComponent<EnemyController>();
            ec.maxHealth = cfg.health;
            ec.moveSpeed = cfg.speed;
            ec.damageToPlayer = cfg.damage;
            ec.attackRange = cfg.attackRange;
            ec.isRanged = cfg.isRanged;

            // FirePoint (child object cho ranged enemies)
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(enemy.transform);
            firePointObj.transform.localPosition = new Vector3(-0.5f, 0, 0); // Phía trước enemy

            // Lưu Prefab
            string fullPrefabPath = $"{prefabPath}/{cfg.enemyName}.prefab";
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(enemy, fullPrefabPath);
            DestroyImmediate(enemy);

            Debug.Log($"[EnemySetup] Created prefab: {fullPrefabPath}");
        }

        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("[EnemySetup] ✅ All 6 enemy prefabs created with Animator!");
    }
#endif
}
