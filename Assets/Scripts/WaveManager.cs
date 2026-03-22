using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Hệ thống Wave: spawn enemy theo từng đợt, độ khó tăng dần.
/// Thay thế EnemySpawner đơn giản bằng wave-based spawning.
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
    public float spawnDelay = 0.5f;         // Delay giữa mỗi enemy trong wave
    public float timeBetweenWaves = 5f;     // Thời gian nghỉ giữa các wave
    public int maxEnemiesAtOnce = 15;

    [Header("Difficulty Scaling")]
    public float healthMultiplierPerWave = 0.1f;  // +10% HP mỗi wave
    public float speedMultiplierPerWave = 0.05f;  // +5% speed mỗi wave
    public float rangedStartWave = 3;             // Wave bắt đầu có enemy ranged

    [Header("UI")]
    public TextMeshProUGUI waveText;

    // ─── Internal ──────────────────────────────────────────────────
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn = 0;
    private bool isSpawning = false;
    private bool waveActive = false;

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
        StartCoroutine(StartNextWave());
    }

    void AutoFindSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0) return;
        
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
        // Chờ ban đầu
        yield return new WaitForSeconds(2f);

        while (true)
        {
            currentWave++;
            int totalEnemies = startEnemyCount + (currentWave - 1) * enemiesPerWaveIncrease;
            enemiesToSpawn = totalEnemies;
            waveActive = true;

            // Hiển thị Wave text
            UpdateWaveUI();

            yield return StartCoroutine(ShowWaveAnnouncement());

            // Spawn từng enemy với delay
            yield return StartCoroutine(SpawnWaveEnemies(totalEnemies));

            // Chờ cho đến khi tất cả enemy trong wave bị tiêu diệt
            yield return new WaitUntil(() => enemiesAlive <= 0 && enemiesToSpawn <= 0);

            waveActive = false;

            // Nghỉ giữa wave
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
        isSpawning = true;
        
        for (int i = 0; i < count; i++)
        {
            // Chờ nếu đã đạt max enemy cùng lúc
            while (enemiesAlive >= maxEnemiesAtOnce)
            {
                yield return new WaitForSeconds(0.5f);
            }

            SpawnOneEnemy();
            enemiesToSpawn--;
            
            yield return new WaitForSeconds(spawnDelay);
        }
        
        isSpawning = false;
    }

    void SpawnOneEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform pt = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (pt == null) return;

        // Quyết định spawn melee hay ranged
        bool shouldSpawnRanged = currentWave >= rangedStartWave 
                                 && rangedPrefabs.Length > 0 
                                 && Random.value < GetRangedChance();

        GameObject prefab;
        if (shouldSpawnRanged)
        {
            prefab = rangedPrefabs[Random.Range(0, rangedPrefabs.Length)];
        }
        else
        {
            if (meleePrefabs.Length == 0) return;
            prefab = meleePrefabs[Random.Range(0, meleePrefabs.Length)];
        }

        // Spawn với offset ngẫu nhiên nhỏ
        Vector3 spawnPos = pt.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0);
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Áp dụng scaling độ khó
        ApplyDifficultyScaling(enemy);

        enemiesAlive++;
    }

    void ApplyDifficultyScaling(GameObject enemy)
    {
        float hpMult = 1f + (currentWave - 1) * healthMultiplierPerWave;
        float speedMult = 1f + (currentWave - 1) * speedMultiplierPerWave;

        EnemyController melee = enemy.GetComponent<EnemyController>();
        if (melee != null)
        {
            melee.maxHealth = Mathf.RoundToInt(melee.maxHealth * hpMult);
            melee.moveSpeed *= speedMult;
        }

        EnemyRangedController ranged = enemy.GetComponent<EnemyRangedController>();
        if (ranged != null)
        {
            ranged.maxHealth = Mathf.RoundToInt(ranged.maxHealth * hpMult);
            ranged.moveSpeed *= speedMult;
        }
    }

    float GetRangedChance()
    {
        // Wave 3: 20%, Wave 5: 30%, Wave 10: 50%, capped at 60%
        return Mathf.Clamp(0.2f + (currentWave - rangedStartWave) * 0.05f, 0.2f, 0.6f);
    }

    // ─── Gọi từ enemy khi chết ────────────────────────────────────
    public void OnEnemyDeath()
    {
        enemiesAlive--;
        enemiesAlive = Mathf.Max(0, enemiesAlive);
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
