using UnityEngine;

// Kế thừa MonoBehaviour để gắn được lên Inspector, và KÝ 2 HỢP ĐỒNG IPullable, IHoldable
public class NPCRescue : MonoBehaviour, IPullable, IHoldable
{
    private Rigidbody2D rb;
    private bool isBeingPulled = false;
    private Transform pullTarget;
    private float pullSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Thực thi logic bay về phía tay Robot ở đây
        if (isBeingPulled && pullTarget != null)
        {
            // Liên tục cập nhật toạ độ mới nhất của pullTarget.position
            transform.position = Vector3.MoveTowards(transform.position, pullTarget.position, pullSpeed * Time.deltaTime);

            // TỰ ĐỘNG BẮT: Nếu khoảng cách giữa NPC và bàn tay Robot < 0.5f -> Bắt lấy!
            if (Vector3.Distance(transform.position, pullTarget.position) < 0.5f)
            {
                PlayerController.Instance.CatchObject(this.gameObject);
            }
        }
    }

    // --- TRẢ LỢI HỢP ĐỒNG IPULLABLE ---
    public void OnPulled(Transform targetPosition, float speed)
    {
        isBeingPulled = true;
        pullTarget = targetPosition;
        pullSpeed = speed;
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic; // Tắt vật lý để NPC không bị rớt xuống đất trong lúc bay!
        Debug.Log("NPC đang bị xích kéo vèo vèo!");
    }

    // --- TRẢ LỢI HỢP ĐỒNG IHOLDABLE ---
    public void OnPickedUp(Transform holdSlot)
    {
        isBeingPulled = false; // Ngừng bay

        // Trở thành con của cái Slot trên tay Robot
        transform.SetParent(holdSlot);

        // Reset toạ độ để NPC nằm gọn gàng trên tay
        transform.localPosition = Vector3.zero;

        // Tắt vật lý để NPC không bị rớt xuống đất
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        Debug.Log("Đã nằm gọn trong vòng tay Robot!");
    }

    public void OnDropped()
    {
        // Rời khỏi tay Robot
        transform.SetParent(null);

        // Bật lại vật lý để rớt xuống đất
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
    }
}