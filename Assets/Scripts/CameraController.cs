using UnityEngine;

/// <summary>
/// Hệ thống Camera cải tiến mới (Tối ưu cho Game Đi Cảnh / Side-Scroller).
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Tracking Settings")]
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);
    
    [Header("Axis Locks")]
    [Tooltip("Khóa Camera ở trục Y cố định để hình nền (Background) không bao giờ bị lệch ra khỏi màn hình.")]
    public bool lockYAxis = true;
    [Tooltip("Vị trí Y cố định của Camera (Thường là 0 để khớp với tâm khung gốc)")]
    public float fixedY = 0f;

    [Header("Zoom")]
    public float cameraZoom = 4.0f;

    private bool targetSearched = false;

    void Start()
    {
        if (target == null) FindPlayer();

        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.orthographicSize = cameraZoom;
    }

    void FindPlayer()
    {
        if (targetSearched) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
        targetSearched = true;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindPlayer();
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        
        // CƠ CHẾ MỚI: Khóa cứng trục Y của Camera = không bao giờ nhìn lên trời hay lòng đất
        if (lockYAxis)
        {
            desiredPosition.y = fixedY;
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
