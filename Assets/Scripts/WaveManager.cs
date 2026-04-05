using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Hệ thống Wave: spawn enemy theo từng đợt, độ khó tăng dần.
/// Tối ưu: dùng Object Pool, track enemy count bằng biến thay vì FindObjectsByType.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject[] meleePrefabs;   // Enemy melee (01, 03, 05)
    public GameObject[] rangedPrefabs;  // Enemy ranged (02, 04, 06)

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Wave Settings")]
    public int startEnemyCount = 3;
    public int enemiesPerWaveIncrease = 2;
    public float spawnDelay = 0.5f;
    public float timeBetweenWaves = 5f;
    public int maxEnemiesAtOnce = 15;
    public int maxWaveEnemyCap = 40; // Hard cap enemy mỗi wave

    [Header("Difficulty Scaling")]
    public float healthMultiplierPerWave = 0.1f;
    public float speedMultiplierPerWave = 0.05f;
    public float rangedStartWave = 3;

    [Header("UI")]
    public TextMeshProUGUI waveText;

    // ─── Internal ──────────────────────────────────────────────────
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn = 0;
    private bool poolsRegistered = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        AutoFindSpawnPoints();
        AutoLoadPrefabs();
        AutoFindUI();

        // Chờ ObjectPool sẵn sàng rồi đăng ký pool cho enemies
        StartCoroutine(InitAndStart());
    }

    IEnumerator InitAndStart()
    {
        // Chờ ObjectPool.Instance sẵn sàng
        yield return null;
        RegisterEnemyPools();
        StartCoroutine(StartNextWave());
    }

    void RegisterEnemyPools()
    {
        if (poolsRegistered || ObjectPool.Instance == null) return;

        // Đăng ký pool cho từng loại enemy prefab
        if (meleePrefabs != null)
        {
            foreach (var prefab in meleePrefabs)
            {
                if (prefab == null) continue;
                string tag = "Enemy_" + prefab.name;
                if (!ObjectPool.Instance.HasPool(tag))
                    ObjectPool.Instance.RegisterPool(tag, prefab, 3, 15);
            }
        }

        if (rangedPrefabs != null)
        {
            foreach (var prefab in rangedPrefabs)
            {
                if (prefab == null) continue;
                string tag = "Enemy_" + prefab.name;
                if (!ObjectPool.Instance.HasPool(tag))
                    ObjectPool.Instance.RegisterPool(tag, prefab, 2, 10);
            }
        }

        poolsRegistered = true;
    }

    void AutoFindSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Kiểm tra null entries
            bool hasNull = false;
            foreach (var pt in spawnPoints)
                if (pt == null) { hasNull = true; break; }
            if (!hasNull) return;
        }

        string[] names = {
            "SpawnPointLeft", "SpawnPointRight",
            "SpawnPointTop", "SpawnPointBottom",
            "SpawnPointTopLeft", "SpawnPointTopRight"
        };
        var list = new List<Transform>();
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) list.Add(go.transform);
        }
        if (list.Count > 0) spawnPoints = list.ToArray();
    }

    void AutoLoadPrefabs()
    {
#if UNITY_EDITOR
        if (meleePrefabs == null || meleePrefabs.Length == 0)
        {
            var list = new List<GameObject>();
            string[] paths = {
                "Assets/Mad Doctor Assets/Prefabs/Enemy_1.prefab",
                "Assets/Mad Doctor Assets/Prefabs/Enemy03.prefab",
                "Assets/Mad Doctor Assets/Prefabs/Enemy05.prefab",
            };
            foreach (var p in paths)
            {
                var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            meleePrefabs = list.ToArray();
        }
        if (rangedPrefabs == null || rangedPrefabs.Length == 0)
        {
            var list = new List<GameObject>();
            string[] paths = {
                "Assets/Mad Doctor Assets/Prefabs/Enemy02.prefab",
                "Assets/Mad Doctor Assets/Prefabs/Enemy04.prefab",
                "Assets/Mad Doctor Assets/Prefabs/Enemy06.prefab",
            };
            foreach (var p in paths)
            {
                var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            rangedPrefabs = list.ToArray();
        }
#endif
    }

    void AutoFindUI()
    {
        if (waveText == null)
        {
            GameObject obj = GameObject.Find("WaveText");
            if (obj != null) waveText = obj.GetComponent<TextMeshProUGUI>();
        }
    }

    // ─── Wave Flow ─────────────────────────────────────────────────

    IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // Kiểm tra game over
            if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            currentWave++;
            int totalEnemies = Mathf.Min(
                startEnemyCount + (currentWave - 1) * enemiesPerWaveIncrease,
                maxWaveEnemyCap
            );
            enemiesToSpawn = totalEnemies;

            UpdateWaveUI();
            yield return StartCoroutine(ShowWaveAnnouncement());
            yield return StartCoroutine(SpawnWaveEnemies(totalEnemies));

            // Chờ cho đến khi tất cả enemy trong wave bị tiêu diệt
            yield return new WaitUntil(() => enemiesAlive <= 0 && enemiesToSpawn <= 0);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    IEnumerator ShowWaveAnnouncement()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(true);
            waveText.text = $"WAVE {currentWave}";
            waveText.fontSize = 60;
            yield return new WaitForSeconds(2f);
            waveText.fontSize = 24;
            waveText.text = $"Wave {currentWave}";
        }
        else
        {
            yield return null;
        }
    }

    IEnumerator SpawnWaveEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Kiểm tra game over giữa chừng
            if (GameManager.Instance != null && GameManager.Instance.isGameOver)
                yield break;

            // Chờ nếu đã đạt max enemy cùng lúc
            while (enemiesAlive >= maxEnemiesAtOnce)
            {
                if (GameManager.Instance != null && GameManager.Instance.isGameOver)
                    yield break;
                yield return new WaitForSeconds(0.5f);
            }

            SpawnOneEnemy();
            enemiesToSpawn--;

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnOneEnemy()
    {
        // Quyết định spawn melee hay ranged
        bool shouldSpawnRanged = currentWave >= rangedStartWave
                                 && rangedPrefabs != null && rangedPrefabs.Length > 0
                                 && Random.value < GetRangedChance();

        GameObject prefab = null;
        if (shouldSpawnRanged)
        {
            prefab = rangedPrefabs[Random.Range(0, rangedPrefabs.Length)];
        }
        else
        {
            if (meleePrefabs != null && meleePrefabs.Length > 0)
                prefab = meleePrefabs[Random.Range(0, meleePrefabs.Length)];
        }
        if (prefab == null) return;

        Vector3 spawnPos = Vector3.zero;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            // Quái sẽ xuất hiện ngoài màn hình của Player (cách 12-20 đơn vị trên trục X)
            float offsetX = Random.Range(12f, 20f);
            if (Random.value > 0.5f) offsetX = -offsetX; // Quái ập tới từ 2 phía
            
            spawnPos.x = playerObj.transform.position.x + offsetX;
            // Khóa tọa độ Y của bãi đáp quái vật để chỉ nằm trong vùng đường rải nhựa
            spawnPos.y = Random.Range(-3.8f, 0.0f);
            
            // Không cho rớt ra vùng map quá giới hạn 
            spawnPos.x = Mathf.Clamp(spawnPos.x, -35f, 35f);
        }
        else
        {
            spawnPos = new Vector3(Random.Range(-15f, 15f), Random.Range(-3.8f, 0.0f), 0);
        }

        string poolTag = "Enemy_" + prefab.name;

        GameObject enemy = null;
        if (ObjectPool.Instance != null && ObjectPool.Instance.HasPool(poolTag))
            enemy = ObjectPool.Instance.Get(poolTag, spawnPos, Quaternion.identity);

        // Fallback nếu pool đầy
        if (enemy == null)
            enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Áp dụng scaling + gán pool tag
        ApplyDifficultyScaling(enemy, poolTag);
        enemiesAlive++;
    }

    void ApplyDifficultyScaling(GameObject enemy, string tag)
    {
        float hpMult = 1f + (currentWave - 1) * healthMultiplierPerWave;
        float speedMult = 1f + (currentWave - 1) * speedMultiplierPerWave;

        EnemyController melee = enemy.GetComponent<EnemyController>();
        if (melee != null)
        {
            melee.poolTag = tag;
            melee.ApplyScaling(hpMult, speedMult);
        }

        EnemyRangedController ranged = enemy.GetComponent<EnemyRangedController>();
        if (ranged != null)
        {
            ranged.poolTag = tag;
            ranged.ApplyScaling(hpMult, speedMult);
        }
    }

    float GetRangedChance()
    {
        return Mathf.Clamp(0.2f + (currentWave - rangedStartWave) * 0.05f, 0.2f, 0.6f);
    }

    // ─── Gọi từ enemy khi chết ────────────────────────────────────
    public void OnEnemyDeath()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    // ─── UI ────────────────────────────────────────────────────────
    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"Wave {currentWave}";
    }

    public int GetCurrentWave() => currentWave;
    public int GetEnemiesAlive() => enemiesAlive;
}
