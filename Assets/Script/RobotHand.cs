using UnityEngine;

public class RobotHand : MonoBehaviour
{
    private Vector3 baseScale;

    [Header("Settings")]
    public float flySpeed = 40f;
    public float maxDistance = 10f;
    public LayerMask wallLayer;

    [Header("References")]
    public Transform launchPosition; // Vị trí bắt đầu của tay robot
    private LineRenderer lineRenderer;
    private PlayerController playerController;

    private Vector2 startPos;
    private Vector2 targetDirection;
    private bool isFlying = false;
    private bool isAttached = false;
    private bool isReturning = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        transform.SetParent(null); // Tách khỏi Player để không di chuyển theo khi Player bay đến
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }

    // Hàm này được Player gọi khi ấn nút bắn
    public void Shoot(Vector2 direction, PlayerController controller)
    {
        if (GameManager.state == GameState.Pause) return;

        // Bật Active ngay đầu tiên để Awake() chạy và lấy baseScale (nếu chưa lấy)
        gameObject.SetActive(true);

        playerController = controller;
        targetDirection = direction.normalized;
        startPos = launchPosition.position;

        transform.position = startPos;

        // 1. Tính toán góc xoay đơn giản dựa trên Vector đã được Controller làm tròn chuẩn
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 2. Xử lý lật hình (Tránh lỗi lộn ngược tay khi bắn sang nửa bên trái)
        Vector3 handScale = baseScale;
        handScale.x = Mathf.Abs(handScale.x); // Luôn giữ X dương

        if (targetDirection.x < 0)
        {
            // Bắn sang trái -> lật trục Y lại để bù trừ cho góc xoay
            handScale.y = -baseScale.y;
        }
        else
        {
            // Bắn sang phải/lên/xuống -> giữ nguyên trục Y
            handScale.y = Mathf.Abs(handScale.y);
        }

        transform.localScale = handScale;

        // 3. Kích hoạt trạng thái bay
        isFlying = true;
        isAttached = false;
        isReturning = false;

        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (GameManager.state == GameState.Pause) return;

        // Luôn cập nhật điểm đầu của dây xích là vị trí Player
        lineRenderer.SetPosition(0, launchPosition.position);
        lineRenderer.SetPosition(1, transform.position);

        if (isAttached) return; // Nếu đã bám tường thì đứng im đợi Player kéo tới

        if (isReturning)
        {
            // Thu hồi tay về
            transform.position = Vector2.MoveTowards(transform.position, launchPosition.position, flySpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, launchPosition.position) < 0.5f)
            {
                FinishGrapple(); // Về tới nơi thì tắt
            }
            return;
        }

        if (isFlying)
        {
            // Bàn tay bay đi
            transform.Translate(targetDirection * flySpeed * Time.deltaTime, Space.World);

            // Xoay bàn tay theo hướng bay (tuỳ chọn)
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Kiểm tra va chạm tường bằng OverlapCircle
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.2f, wallLayer);
            if (hit)
            {
                // Trúng tường!
                isFlying = false;
                isAttached = true;
                playerController.StartPullingToHand(transform.position, hit.transform);
            }
            // Kiểm tra quá tầm bay
            else if (Vector2.Distance(startPos, transform.position) > maxDistance)
            {
                isFlying = false;
                isReturning = true; // Thu hồi tay lại
                playerController.CancelGrapple();
            }
        }
    }

    public void FinishGrapple()
    {
        gameObject.SetActive(false);
    }
}