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
    public float TryMove(float amount)
    {
        if (amount == 0) return 0f;

        float direction = Mathf.Sign(amount);
        float distance = Mathf.Abs(amount);

        // Quét xem phía trước hộp có bức tường nào không
        RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.right * direction, distance + skinWidth, collisionMask);

        float actualMove = amount;

        if (hit)
        {
            // Bị kẹt tường! Tính toán quãng đường thực tế chỉ nhích đến sát tường
            actualMove = (hit.distance - skinWidth) * direction;
        }

        // Cập nhật vị trí của hộp
        transform.position += Vector3.right * actualMove;
        rb.position = transform.position;

        return actualMove; // Trả về quãng đường thực tế hộp đi được!
    }
}