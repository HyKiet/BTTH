using UnityEngine;

/// <summary>
/// Makes this GameObject follow the camera position exactly.
/// Used for static background borders that should always stay on screen.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    private Camera targetCamera;

    void Start()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}
