using UnityEngine;

public class FallingRock : MonoBehaviour
{
    [Header("Settings")]
    public float fallSpeed = 15f; // Tốc độ rơi của đá
    public LayerMask groundLayer; // Định nghĩa layer mặt đất để đá biết đường biến mất
    
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError("FallingRock requires a BoxCollider2D component.");
        }
    }

    private void FixedUpdate()
    {
        // Rơi thẳng xuống dưới mỗi frame
        transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Kiểm tra chạm mặt đất (Dùng LayerMask)
        bool hitGround = ((1 << collision.gameObject.layer) & groundLayer) != 0;

        // 2. Kiểm tra chạm người chơi (Script Hazard của bạn sẽ lo việc gọi hàm Die() của Player)
        bool hitPlayer = collision.CompareTag("Player");

        if (hitGround || hitPlayer)
        {
            // Thay vì Destroy, ta tắt nó đi để trả về Pool cho Spawner dùng lại
            gameObject.SetActive(false);

            // TODO: Ở đây bạn có thể gọi thêm hiệu ứng vỡ vụn hoặc âm thanh "Bịch"
        }
    }
}