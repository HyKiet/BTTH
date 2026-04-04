using UnityEngine;

/// <summary>
/// Hoạt ảnh lặp vô tận (đảo frame liên tục) cho Đạn/Projectile.
/// Dùng để tạo cảm giác giật điện, chớp sáng, hoặc quả cầu lửa đang cháy.
/// </summary>
public class ProjectileAnimator : MonoBehaviour
{
    public Sprite[] animationFrames;
    public float frameRate = 0.05f;

    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float timer = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        currentFrame = 0;
        timer = 0f;
        if (sr != null && animationFrames != null && animationFrames.Length > 0)
        {
            sr.sprite = animationFrames[0];
        }
    }

    void Update()
    {
        if (sr == null || animationFrames == null || animationFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            sr.sprite = animationFrames[currentFrame];
        }
    }
}
