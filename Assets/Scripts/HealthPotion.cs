using UnityEngine;

/// <summary>
/// Quản lý vật phẩm phục hồi máu (Health Potion)
/// Tính năng: Animation trôi nổi (floating) nhẹ nhàng và hồi máu khi Player chạm vào.
/// </summary>
public class HealthPotion : MonoBehaviour
{
    [Header("Settings")]
    public int healAmount = 25;
    public float floatSpeed = 2f;
    public float floatHeight = 0.5f;

    private float initialY;
    private bool hasPickedUp = false;

    void Start()
    {
        initialY = transform.position.y;
    }

    void Update()
    {
        // Hiệu ứng lơ lửng
        transform.position = new Vector3(
            transform.position.x,
            initialY + Mathf.Sin(Time.time * floatSpeed) * floatHeight,
            transform.position.z
        );
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (hasPickedUp) return;

        if (col.CompareTag("Player"))
        {
            PlayerController player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                hasPickedUp = true;
                player.Heal(healAmount);
                Destroy(gameObject);
                
                // Hiển thị popup text hồi máu (+25)
                DamageNumber.Spawn(transform.position, healAmount, Color.green);
            }
        }
    }
}
