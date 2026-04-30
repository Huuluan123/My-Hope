using UnityEngine;

public class RobotHand : MonoBehaviour
{
    private Vector3 baseScale;

    [Header("Settings")]
    public float flySpeed = 40f;
    public float maxDistance = 10f;
    public LayerMask hitLayer;

    [Header("References")]
    public Transform launchPosition;
    private LineRenderer lineRenderer;
    private PlayerController playerController;
    private BoxCollider2D handCollider;

    private Vector2 startPos;
    private Vector2 targetDirection;
    private bool isFlying = false;
    private bool isAttached = false;
    private bool isReturning = false;

    void Awake()
    {
        // 1. Khởi tạo Collider ở dạng Trigger
        handCollider = GetComponent<BoxCollider2D>();
        handCollider.isTrigger = true;
        handCollider.enabled = true; // Luôn bật để bounds.size không bị bằng 0

        lineRenderer = GetComponent<LineRenderer>();

        // Tách khỏi cha để không bị rung lắc khi Player di chuyển
        transform.SetParent(null);

        // Lưu scale gốc để tính toán lật mặt (Flip)
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }

    public void Shoot(Vector2 direction, PlayerController controller)
    {
        if (GameManager.Instance.State == GameState.Pause) return;
        gameObject.SetActive(true);

        // Quét trước điểm xuất phát (Pre-scan)
        Collider2D initialOverlap = Physics2D.OverlapBox(launchPosition.position, handCollider.bounds.size, 0f, hitLayer);
        if (initialOverlap != null && !initialOverlap.CompareTag("Player") && !initialOverlap.CompareTag("OneWay"))
        {
            // Nếu họng súng đang kẹt trong tường -> TẮT BÀN TAY ĐI VÀ HỦY LỆNH
            gameObject.SetActive(false);
            controller.CancelGrapple();
            return;
        }

        // Nếu an toàn, tiếp tục thiết lập thông số bay
        playerController = controller;
        targetDirection = direction.normalized;
        startPos = launchPosition.position;
        transform.position = startPos;

        // Cập nhật hướng xoay của bàn tay theo hướng bắn
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Xử lý lật mặt sprite (Flip Y) khi bắn sang trái
        Vector3 handScale = baseScale;
        handScale.y = (targetDirection.x < 0) ? -baseScale.y : baseScale.y;
        transform.localScale = handScale;

        isFlying = true;
        isAttached = false;
        isReturning = false;
        lineRenderer.positionCount = 2;

        // Cập nhật vị trí dây NGAY LẬP TỨC tại frame số 0
        // để đè lên ký ức dây của lần bắn trước đó
        lineRenderer.SetPosition(0, launchPosition.position);
        lineRenderer.SetPosition(1, transform.position);
    }

    void Update()
    {
        if (GameManager.Instance.State == GameState.Pause) return;

        // Vẽ dây nối giữa người chơi và bàn tay
        lineRenderer.SetPosition(0, launchPosition.position);
        lineRenderer.SetPosition(1, transform.position);

        // --- CƠ CHẾ AN TOÀN: ĐỨT DÂY DO QUÁ XA ---
        // Cho phép dây dãn thêm một chút (ví dụ x1.2) so với maxDistance để tạo cảm giác dây thun
        // Tránh việc đứt ngay lập tức khi nhân vật mới nhích ra 1 pixel
        float currentDistance = Vector2.Distance(transform.position, playerController.transform.position);
        if (currentDistance > maxDistance * 1.2f)
        {
            // Gọi hàm Cancel của Player thay vì tự tắt, để Player xả trạng thái rơi tự do
            playerController.CancelGrapple();
            FinishGrapple();
            return;
        }
        // -------------------------------------------

        if (isAttached) return;

        // Xử lý khi tay đang thu hồi
        if (isReturning)
        {
            transform.position = Vector2.MoveTowards(transform.position, launchPosition.position, flySpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, launchPosition.position) < 0.5f) FinishGrapple();
            return;
        }

        // Xử lý khi tay đang bay đi tìm mục tiêu
        if (isFlying)
        {
            float step = flySpeed * Time.deltaTime;
            float angle = transform.eulerAngles.z;

            // SỬ DỤNG BOXCAST VỚI BOUNDS.SIZE (Chính xác 100% với Scale trong Editor)
            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                transform.position,
                handCollider.bounds.size,
                angle,
                targetDirection,
                step,
                hitLayer
            );

            bool validHitFound = false;

            foreach (RaycastHit2D hit in hits)
            {
                // Bỏ qua bản thân người chơi và sàn xuyên thấu (OneWay)
                if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("OneWay")) continue;

                // KIỂM TRA NẾU TRÚNG VẬT THỂ CÓ THỂ KÉO (NPC)
                IPullable pullable = hit.collider.GetComponent<IPullable>();
                if (pullable != null)
                {
                    Debug.Log("Pullable hit: " + hit.collider.name);
                    isFlying = false;
                    pullable.OnPulled(launchPosition, flySpeed);
                    isReturning = true;
                    break;
                }

                // TÍNH TOÁN VỊ TRÍ BÁM (Centroid giúp deltaMove của Player cực kỳ chuẩn)
                // hit.centroid là vị trí tâm của BoxCollider tại thời điểm va chạm
                Vector2 attachPos = (hit.distance > 0) ? hit.centroid : (Vector2)transform.position;

                // Kiểm tra nếu trúng nút bấm Grapple (GrappleNode)
                GrappleNode node = hit.collider.GetComponent<GrappleNode>();
                if (node != null)
                {
                    if (!node.isActive) continue; // Bỏ qua nếu node đang trong thời gian hồi (cooldown)

                    node.OnGrappleSuccess();
                    AttachToTarget(attachPos, hit.transform, node);
                    validHitFound = true;
                    break;
                }

                // Kiểm tra nếu trúng tường không thể leo (Unclimbable)
                if (hit.collider.CompareTag("Unclimbable"))
                {
                    isFlying = false;
                    isReturning = true;
                    playerController.CancelGrapple();
                    validHitFound = true;
                    break;
                }

                // Trúng tường bình thường
                AttachToTarget(attachPos, hit.transform, null);
                validHitFound = true;
                break;
            }

            // Nếu chưa đụng gì, tiếp tục bay
            if (!validHitFound)
            {
                transform.Translate(targetDirection * step, Space.World);

                // Kiểm tra nếu bay quá tầm tối đa
                if (Vector2.Distance(startPos, transform.position) > maxDistance)
                {
                    isFlying = false;
                    isReturning = true;
                    playerController.CancelGrapple();
                }
            }
        }
    }

    private void AttachToTarget(Vector2 hitPosition, Transform hitTransform, GrappleNode node)
    {
        transform.position = hitPosition;
        isFlying = false;
        isAttached = true;

        // TÍNH TOÁN QUÃNG ĐƯỜNG DỊCH CHUYỂN (Delta Move)
        // Gửi quãng đường từ lúc bắn đến lúc đụng tường về cho PlayerController xử lý

        // 1. Quãng đường tay bay
        Vector2 pullDelta = hitPosition - startPos;

        // 2. Khoảng cách bù trừ (Từ tâm người chơi đến vị trí bắn tay)
        Vector2 offset = startPos - (Vector2)playerController.transform.position;

        // 3. Đoạn đẩy thêm để ép dính tường
        Vector2 extraPush = targetDirection * 0.6f;

        // Gộp tất cả lại thành Vector tổng!
        Vector2 finalDelta = pullDelta + offset + extraPush;

        // Gửi lệnh kéo về cho Player
        playerController.StartPullingToHand(finalDelta, hitTransform, node);
    }

    public void FinishGrapple()
    {
        gameObject.SetActive(false);

        // Báo cho PlayerController biết là tay đã thu về xong
        // để giải phóng nhân vật khỏi trạng thái Grappling/GrappleFreeze
        if (playerController != null)
        {
            playerController.CancelGrapple();
        }
    }
}