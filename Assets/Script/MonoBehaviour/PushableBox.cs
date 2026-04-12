using UnityEngine;

public class PushableBox : MonoBehaviour, IPushable, IRoomElement
{
    [Header("Physics Settings")]
    [SerializeField] private float gravity = -60f; // Giống gravity của player
    [SerializeField] private float maxFallSpeed = -25f;

    [Tooltip("Choose the layers that the box can collide with. WARNING: Do NOT include the Player layer here!")]
    [SerializeField] private LayerMask collisionMask; // LƯU Ý: Tuyệt đối KHÔNG CHỌN Layer của Player ở đây!

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Vector3 initialPosition;
    private float velocityY;
    private const float skinWidth = 0.015f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        initialPosition = transform.position; // Lưu vị trí gốc
    }

    // KẾT NỐI VỚI ROOM MANAGER
    public void ResetState()
    {
        transform.position = initialPosition;
        if (rb != null) rb.position = initialPosition;
        velocityY = 0f;
    }

    private void FixedUpdate()
    {
        // 1. Áp dụng trọng lực
        if (velocityY > maxFallSpeed) velocityY += gravity * Time.fixedDeltaTime;

        // 2. Kiểm tra va chạm mặt đất bằng BoxCast (Dễ hơn bắn nhiều tia Raycast)
        float distance = Mathf.Abs(velocityY * Time.fixedDeltaTime);
        RaycastHit2D hitDown = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, distance + skinWidth, collisionMask);

        if (hitDown)
        {
            // Nếu chạm đất
            transform.position += Vector3.down * (hitDown.distance - skinWidth);
            velocityY = 0f;
        }
        else
        {
            // Rơi tự do
            transform.position += Vector3.up * (velocityY * Time.fixedDeltaTime);
            Vector3 moveDelta = Vector3.up * (velocityY * Time.fixedDeltaTime);
        }

        rb.position = transform.position;
    }

    // LOGIC KHI BỊ NGƯỜI CHƠI KÉO/ĐẨY
    public bool TryMove(float amount)
    {
        Debug.Log("Try move box with amount: " + amount / Time.fixedDeltaTime);
        if (amount == 0) return true;

        float direction = Mathf.Sign(amount);
        float distance = Mathf.Abs(amount);

        // Quét xem phía trước hộp có bức tường nào không
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.right * direction, distance + skinWidth, collisionMask);

        if (hit)
        {
            // Bị kẹt tường! Chỉ nhích đến sát tường rồi dừng lại
            transform.position += Vector3.right * direction * (hit.distance - skinWidth);
            rb.position = transform.position;
            return false; // Báo cho Player biết là bị kẹt rồi, phải dừng lại!
        }
        else
        {
            // Trống trải, thoải mái di chuyển
            transform.position += Vector3.right * amount;
            rb.position = transform.position;
            return true;
        }
    }
}