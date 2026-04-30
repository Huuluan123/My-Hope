using UnityEngine;
using UnityEngine.Events;

public class SpringPad : MonoBehaviour
{
    [Header("Bouncer Settings")]
    [Tooltip("Lực nảy chính đẩy nhân vật theo hướng của lò xo")]
    public float bounceForce = 30f;

    [Header("Horizontal Assist")]
    [Tooltip("Lực đẩy hất lên trên (trục Y) cộng thêm khi lò xo được đặt nằm ngang")]
    public float verticalAssistForce = 15f;

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

                // 1. Tính toán hướng đẩy gốc dựa vào góc xoay của lò xo
                Vector2 direction = (Vector2)transform.up;
                Vector2 appliedForce = direction * bounceForce;

                // 2. CƠ CHẾ HỖ TRỢ PHƯƠNG NGANG (Horizontal Assist)
                // Kiểm tra xem lò xo có đang hướng sang ngang hay không (trị tuyệt đối của x > 0.5)
                // và đảm bảo nó không phải là lò xo gắn trên trần nhà cố tình bắn xuống đất (y >= -0.1f)
                if (Mathf.Abs(direction.x) > 0.5f && direction.y >= -0.1f)
                {
                    // Cộng thêm một lực hất lên trên để tạo quỹ đạo hình vòng cung
                    appliedForce.y += verticalAssistForce;
                }

                // 3. Gửi nguyên Vector2 hoàn chỉnh này cho Player xử lý
                player.ApplyBounce(appliedForce);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 1. TÁI TẠO LẠI CÔNG THỨC TOÁN HỌC CỦA LỰC ĐẨY
        Vector2 direction = (Vector2)transform.up;
        Vector2 appliedForce = direction * bounceForce;

        if (Mathf.Abs(direction.x) > 0.5f && direction.y >= -0.1f)
        {
            appliedForce.y += verticalAssistForce;
        }

        // Hướng bay thực tế (đã chuẩn hóa) và độ dài hiển thị của mũi tên
        Vector3 actualDirection = appliedForce.normalized;
        float arrowLength = 2f; // Bạn có thể tăng/giảm độ dài mũi tên ở đây

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + actualDirection * arrowLength;

        // 2. VẼ MŨI TÊN
        // Chọn màu xanh lục nổi bật
        Gizmos.color = Color.green;

        // Vẽ thân mũi tên
        Gizmos.DrawLine(startPos, endPos);

        // Tính toán 2 râu của đầu mũi tên (Tạo góc 35 độ so với thân)
        float arrowHeadLength = 0.4f;
        Vector3 rightWing = Quaternion.Euler(0, 0, 35f) * (-actualDirection) * arrowHeadLength;
        Vector3 leftWing = Quaternion.Euler(0, 0, -35f) * (-actualDirection) * arrowHeadLength;

        // Vẽ 2 râu
        Gizmos.DrawLine(endPos, endPos + rightWing);
        Gizmos.DrawLine(endPos, endPos + leftWing);
    }
}