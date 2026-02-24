using UnityEngine;

public class RobotHand : MonoBehaviour
{
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
    }

    // Hàm này được Player gọi khi ấn nút bắn
    public void Shoot(Vector2 direction, PlayerController controller)
    {
        if (GameManager.state == GameState.Pause) return;

        playerController = controller;
        targetDirection = direction.normalized;
        startPos = launchPosition.position;

        transform.position = startPos;

        // 1. Tính toán góc xoay cho 8 hướng
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 2. Sửa lỗi lộn ngược (Upside Down) khi bắn sang trái
        Vector3 handScale = transform.localScale;

        // Đảm bảo trục X luôn dương để tránh lật kép
        handScale.x = Mathf.Abs(handScale.x);

        if (targetDirection.x < 0)
        {
            // Bắn sang nửa bên TRÁI (chéo trái lên, chéo trái xuống, thẳng trái)
            // Lúc này góc xoay là 135, 180, -135 độ khiến ảnh bị lộn ngược.
            // -> Ta lật trục Y lại để bù trừ.
            handScale.y = -Mathf.Abs(handScale.y);
        }
        else
        {
            // Bắn sang nửa bên PHẢI (hoặc bắn thẳng lên/xuống khi x = 0)
            // Góc xoay bình thường, giữ nguyên trục Y.
            handScale.y = Mathf.Abs(handScale.y);
        }

        transform.localScale = handScale;

        // 3. Kích hoạt bay
        isFlying = true;
        isAttached = false;
        isReturning = false;

        gameObject.SetActive(true);
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