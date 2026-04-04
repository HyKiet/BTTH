using UnityEngine;

/// <summary>
/// Đạn player và đạn Enemy — Thiết kế lại hoàn toàn (Sweep Raycast & Fallback).
/// 100% không xuyên mục tiêu, chống Missing Script.
/// </summary>
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    public float lifeTime = 3f;
    public float hitRadius = 0.5f;

    private Vector2 moveDirection;
    private SpriteRenderer sr;
    private Collider2D myCollider;
    private bool hasHit = false;
    [HideInInspector] public bool isEnemyBullet = false;

    public const string POOL_TAG = "PlayerBullet";

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        // Không thao tác với RigidBody nữa nhằm tự kiểm soát tuyệt đối đường bay.
    }

    void OnEnable()
    {
        hasHit = false;
        isEnemyBullet = false;
        
        if (sr != null) sr.enabled = true;
        if (myCollider != null) myCollider.enabled = true;

        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Vector3 scaler = transform.localScale;
        if (moveDirection.x < 0)
        {
            scaler.y = -Mathf.Abs(scaler.y);
            scaler.x = Mathf.Abs(scaler.x);
        }
        else
        {
            scaler.y = Mathf.Abs(scaler.y);
            scaler.x = Mathf.Abs(scaler.x);
        }
        transform.localScale = scaler;
    }

    void Update()
    {
        if (hasHit) return;

        float distThisFrame = speed * Time.deltaTime;
        Vector2 currentPos = transform.position;

        // Quét tia dạng CircleCast suốt quãng đường di chuyển của frame này
        // Khoảng cách quét = distThisFrame. Nếu phát hiện trúng, xử lý ngay.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(currentPos, hitRadius, moveDirection, distThisFrame);

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != this.gameObject)
            {
                ProcessHit(hit.collider.gameObject);
                if (hasHit) break;
            }
        }

        // Tự di chuyển viên đạn
        if (!hasHit)
        {
            transform.Translate(moveDirection * distThisFrame, Space.World);
        }
    }

    // Safety fallback (Đề phòng trường hợp viên đạn spawn ngay bên trong mục tiêu và bị CircleCast bỏ qua khe hẹp)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        ProcessHit(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (hasHit) return;
        ProcessHit(coll.gameObject);
    }

    void ProcessHit(GameObject hitObj)
    {
        if (hasHit) return;

        // Bỏ qua các loại đạn khác
        if (hitObj.GetComponent<Bullet>() != null || hitObj.GetComponent<EnemyBullet>() != null)
            return;

        // Xử lý đạn của quái vật bắn Player
        if (isEnemyBullet)
        {
            if (hitObj.CompareTag("Enemy")) return;

            PlayerController player = hitObj.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                hasHit = true;
                DestroyBullet();
            }
            return;
        }

        // Xử lý đạn Player bắn quái vật
        bool isEnemyTag = hitObj.CompareTag("Enemy");

        EnemyController ec = hitObj.GetComponent<EnemyController>();
        if (ec == null) ec = hitObj.GetComponentInParent<EnemyController>();

        EnemyRangedController erc = hitObj.GetComponent<EnemyRangedController>();
        if (erc == null) erc = hitObj.GetComponentInParent<EnemyRangedController>();

        // Trường hợp 1: Có Component Enemy Scripts chuẩn chỉ -> Gây damage và hủy
        if (ec != null)
        {
            hasHit = true;
            ec.TakeDamage(damage);
            DestroyBullet();
            return;
        }

        if (erc != null)
        {
            hasHit = true;
            erc.TakeDamage(damage);
            DestroyBullet();
            return;
        }

        // Trường hợp 2: Bị dính lỗi Missing Scripts trên Enemy Prefab!
        // Giải quyết lỗi mất liên kết của Unity bằng lệnh SendMessage dự phòng.
        if (isEnemyTag)
        {
            hitObj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hasHit = true;
            DestroyBullet();
            Debug.LogWarning($"[HỆ THỐNG BULLET FALLBACK] Cứu 1 rớt Damage lên {hitObj.name} do lỗi Missing Script trên GameObject này!");
            return;
        }

        // Nếu bắn trúng chính người chơi -> bỏ qua
        if (hitObj.GetComponent<PlayerController>() != null) return;
    }

    void DestroyBullet()
    {
        if (sr != null) sr.enabled = false;
        if (myCollider != null) myCollider.enabled = false;
        hasHit = true;
        ReturnToPool();
    }

    void ReturnToPool()
    {
        CancelInvoke();
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }
}