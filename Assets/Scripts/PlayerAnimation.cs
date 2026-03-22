using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] deathSprites;
    
    public float frameRate = 0.1f;
    private float timer;
    private int currentFrame;
    private Rigidbody2D rb;
    
    private bool isPlayingDeath = false;
    private bool deathFinished = false;
    private System.Action onDeathAnimationComplete;

    void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (spriteRenderer == null) return;
        
        // Khi đang chơi animation chết
        if (isPlayingDeath)
        {
            PlayDeathAnimation();
            return;
        }

        if (idleSprites == null || idleSprites.Length == 0) return;
        if (walkSprites == null || walkSprites.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            currentFrame++;

            bool isWalking = rb != null && (Mathf.Abs(rb.linearVelocity.x) > 0.1f || Mathf.Abs(rb.linearVelocity.y) > 0.1f);
            
            Sprite[] currentArray = isWalking ? walkSprites : idleSprites;
            if (currentFrame >= currentArray.Length) currentFrame = 0;
            
            spriteRenderer.sprite = currentArray[currentFrame];
        }
    }

    void PlayDeathAnimation()
    {
        if (deathFinished) return;
        if (deathSprites == null || deathSprites.Length == 0)
        {
            deathFinished = true;
            onDeathAnimationComplete?.Invoke();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            
            if (currentFrame < deathSprites.Length)
            {
                spriteRenderer.sprite = deathSprites[currentFrame];
                currentFrame++;
            }
            else
            {
                // Animation chết đã hoàn tất
                deathFinished = true;
                onDeathAnimationComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// Bắt đầu phát animation chết. Gọi callback khi xong.
    /// </summary>
    public void PlayDeath(System.Action onComplete = null)
    {
        isPlayingDeath = true;
        deathFinished = false;
        currentFrame = 0;
        timer = 0f;
        onDeathAnimationComplete = onComplete;
    }

    public bool IsDeathFinished => deathFinished;
}