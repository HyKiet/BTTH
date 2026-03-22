using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs (kéo 6 Prefabs vào đây)")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    public float spawnInterval = 2.5f;
    public int maxEnemiesAtOnce = 12;

    void Start()
    {
        // Tự động tìm SpawnPoints nếu mảng trống hoặc chứa phần tử null
        bool needsSpawnPoints = false;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            needsSpawnPoints = true;
        }
        else
        {
            foreach (var pt in spawnPoints)
            {
                if (pt == null)
                {
                    needsSpawnPoints = true;
                    Debug.LogWarning("[EnemySpawner] Có SpawnPoint bị để trống (null) trong Inspector. Sẽ thử tự động tìm lại!");
                    break;
                }
            }
        }

        if (needsSpawnPoints)
        {
            string[] spawnNames = {
                "SpawnPointLeft", "SpawnPointRight",
                "SpawnPointTop", "SpawnPointBottom",
                "SpawnPointTopLeft", "SpawnPointTopRight"
            };
            var list = new List<Transform>();
            foreach (var n in spawnNames)
            {
                var go = GameObject.Find(n);
                if (go != null) list.Add(go.transform);
            }
            
            if (list.Count > 0)
                spawnPoints = list.ToArray();
                
            Debug.Log($"[EnemySpawner] Auto-Found {spawnPoints?.Length ?? 0} spawn points.");
        }

        // Tự động load Prefab Enemy từ AssetDatabase nếu chưa gán
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            LoadPrefabsFromAssets();
        }

        Debug.Log($"[EnemySpawner] {enemyPrefabs?.Length} prefabs / {spawnPoints?.Length} spawn points. Starting spawn loop.");
        StartCoroutine(SpawnLoop());
    }

    void LoadPrefabsFromAssets()
    {
#if UNITY_EDITOR
        string[] paths = {
            "Assets/Mad Doctor Assets/Prefabs/Enemy_1.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Enemy02.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Enemy03.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Enemy04.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Enemy05.prefab",
            "Assets/Mad Doctor Assets/Prefabs/Enemy06.prefab",
        };
        var list = new List<GameObject>();
        foreach (var p in paths)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) list.Add(go);
            else Debug.LogWarning($"[EnemySpawner] Prefab not found: {p}");
        }
        enemyPrefabs = list.ToArray();
        Debug.Log($"[EnemySpawner] Loaded {enemyPrefabs.Length} prefabs from AssetDatabase.");
#endif
    }

    IEnumerator SpawnLoop()
    {
        // Chờ 1s trước khi bắt đầu spawn
        yield return new WaitForSeconds(1f);
        
        while (true)
        {
            // Đếm số lượng enemy hiện tại (cả melee + ranged)
            int meleeCount = FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
            int rangedCount = FindObjectsByType<EnemyRangedController>(FindObjectsSortMode.None).Length;
            int currentCount = meleeCount + rangedCount;
            
            if (currentCount < maxEnemiesAtOnce)
            {
                Spawn();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Spawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySpawner] Không có SpawnPoint!");
            return;
        }
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[EnemySpawner] Không có Enemy Prefab!");
            return;
        }

        Transform pt = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject pf = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        if (pt == null || pf == null) return;
        
        Instantiate(pf, pt.position, Quaternion.identity);
        Debug.Log($"[EnemySpawner] Spawned {pf.name} at {pt.name}");
    }
}