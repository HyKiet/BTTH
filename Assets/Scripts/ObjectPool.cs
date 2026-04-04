using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Object Pool tổng quát — tái sử dụng GameObject thay vì Instantiate/Destroy.
/// Giải quyết vấn đề Out of Memory bằng cách giảm GC pressure.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
    }

    [Header("Pool Definitions")]
    public List<Pool> pools = new List<Pool>();

    // Internal storage
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, Pool> poolConfigDict = new Dictionary<string, Pool>();
    private Dictionary<string, int> activeCount = new Dictionary<string, int>();
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializePools();
    }

    void InitializePools()
    {
        foreach (var pool in pools)
        {
            if (pool.prefab == null || string.IsNullOrEmpty(pool.tag)) continue;

            var queue = new Queue<GameObject>();
            poolDict[pool.tag] = queue;
            poolConfigDict[pool.tag] = pool;
            activeCount[pool.tag] = 0;

            // Tạo parent container để scene gọn gàng
            GameObject parent = new GameObject($"Pool_{pool.tag}");
            parent.transform.SetParent(transform);
            poolParents[pool.tag] = parent.transform;

            // Pre-warm
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.tag);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }
    }

    GameObject CreateNewObject(string tag)
    {
        if (!poolConfigDict.ContainsKey(tag)) return null;

        Pool config = poolConfigDict[tag];
        GameObject obj = Instantiate(config.prefab, poolParents[tag]);
        obj.name = config.prefab.name; // Bỏ "(Clone)" suffix

        // Gắn PoolTag để biết object thuộc pool nào
        PoolTag pt = obj.GetComponent<PoolTag>();
        if (pt == null) pt = obj.AddComponent<PoolTag>();
        pt.poolTag = tag;

        return obj;
    }

    /// <summary>
    /// Lấy object từ pool. Trả về null nếu pool đã đạt max capacity.
    /// </summary>
    public GameObject Get(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(tag)) return null;

        var queue = poolDict[tag];
        var config = poolConfigDict[tag];
        GameObject obj = null;

        // Lấy từ queue nếu có
        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null) break; // Bỏ qua null (đã bị destroy bất thường)
            obj = null;
        }

        // Nếu queue hết → tạo mới (nếu chưa đạt max)
        if (obj == null)
        {
            int totalCreated = activeCount[tag] + queue.Count;
            if (totalCreated >= config.maxSize) return null; // Hard cap

            obj = CreateNewObject(tag);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        activeCount[tag]++;

        return obj;
    }

    /// <summary>
    /// Trả object về pool thay vì Destroy.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        PoolTag pt = obj.GetComponent<PoolTag>();
        if (pt == null || !poolDict.ContainsKey(pt.poolTag))
        {
            // Không thuộc pool nào → destroy bình thường
            Destroy(obj);
            return;
        }

        string tag = pt.poolTag;
        obj.SetActive(false);

        if (poolParents.ContainsKey(tag))
            obj.transform.SetParent(poolParents[tag]);

        poolDict[tag].Enqueue(obj);
        activeCount[tag] = Mathf.Max(0, activeCount[tag] - 1);
    }

    /// <summary>
    /// Trả tất cả active objects của một pool type về pool.
    /// Dùng khi restart game hoặc game over.
    /// </summary>
    public void ReturnAll(string tag)
    {
        if (!poolDict.ContainsKey(tag) || !poolParents.ContainsKey(tag)) return;

        Transform parent = poolParents[tag];
        var queue = poolDict[tag];

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child.activeSelf)
            {
                child.SetActive(false);
                queue.Enqueue(child);
            }
        }
        activeCount[tag] = 0;
    }

    /// <summary>
    /// Đăng ký pool mới at runtime (cho enemy prefabs dynamic).
    /// </summary>
    public void RegisterPool(string tag, GameObject prefab, int initialSize = 5, int maxSize = 30)
    {
        if (poolDict.ContainsKey(tag)) return; // Đã có rồi

        var pool = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            maxSize = maxSize
        };

        pools.Add(pool);
        poolDict[tag] = new Queue<GameObject>();
        poolConfigDict[tag] = pool;
        activeCount[tag] = 0;

        GameObject parent = new GameObject($"Pool_{tag}");
        parent.transform.SetParent(transform);
        poolParents[tag] = parent.transform;

        // Pre-warm
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject(tag);
            obj.SetActive(false);
            poolDict[tag].Enqueue(obj);
        }
    }

    /// <summary>
    /// Kiểm tra pool có tồn tại không
    /// </summary>
    public bool HasPool(string tag) => poolDict.ContainsKey(tag);

    /// <summary>
    /// Lấy số lượng active objects
    /// </summary>
    public int GetActiveCount(string tag) => activeCount.ContainsKey(tag) ? activeCount[tag] : 0;
}

/// <summary>
/// Component nhỏ gắn vào pooled objects để track chúng thuộc pool nào.
/// </summary>
public class PoolTag : MonoBehaviour
{
    [HideInInspector] public string poolTag;
}
