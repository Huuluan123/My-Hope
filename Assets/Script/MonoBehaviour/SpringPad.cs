using UnityEngine;
using UnityEngine.Events;

public class SpringPad : MonoBehaviour
{
    [Header("Bouncer Settings")]
    [Tooltip("Lực nảy đẩy nhân vật")]
    public float bounceForce = 30f;

    [Header("Events (Animation & Audio)")]
    public UnityEvent onBounce;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                onBounce?.Invoke();

                // TÍNH TOÁN HƯỚNG ĐẨY LÝ TƯỞNG:
                // transform.up sẽ tự động trả về Vector hướng lên của lò xo.
                // Ví dụ: Xoay Z = 90 độ -> transform.up sẽ chỉ sang TRÁI (-1, 0)
                Vector2 appliedForce = (Vector2)transform.up * bounceForce;

                // Gửi nguyên cái Vector2 này cho Player
                player.ApplyBounce(appliedForce);
            }
        }
    }
}