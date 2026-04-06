using UnityEngine;
using Unity.Cinemachine;

public class RoomTransitionScript : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera roomCamera;

    [Header("Auto Walk Settings")]
    [Tooltip("Time to push player (Seconds)")]
    [SerializeField] private float pushDuration = 0.2f;
    [SerializeField] private float horizontalPushSpeed = 8f;   // Tốc độ lướt ngang
    [SerializeField] private float downwardPushSpeed = 8f;     // Tốc độ rớt xuống
    [SerializeField] private float upwardPushSpeed = 18f;      // Lực đẩy lên cực mạnh để qua mép

    private Collider2D roomCollider;
    private RoomManager myRoomManager;
    private bool isPlayerInside = false; // Biến ghi nhớ trạng thái
    public bool drawRoomBoudary = true;

    private void Awake()
    {
        roomCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        myRoomManager = GetComponentInParent<RoomManager>();

        // KIỂM TRA LÚC SPAWN: Tìm người chơi xem có đang đứng trong phòng này không
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Nếu tọa độ người chơi nằm gọn bên trong Collider của phòng này
            if (roomCollider.bounds.Contains(player.transform.position))
            {
                isPlayerInside = true;

                // (Tùy chọn) Ép camera chuyển luôn sang phòng này ngay lúc bắt đầu game
                CameraManager.SwitchCamera(roomCamera);

                //Reset Parallax lúc mới vào game
                ParallaxCamera pCam = Camera.main.GetComponent<ParallaxCamera>();
                if (pCam != null) pCam.ResetOldPosition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Nếu nhân vật ĐÃ Ở SẴN trong phòng từ đầu, BỎ QUA lệnh đẩy!
            if (isPlayerInside) return;

            // Đánh dấu là nhân vật vừa mới bước vào
            isPlayerInside = true;

            // 1. Chuyển Camera
            CameraManager.SwitchCamera(roomCamera);

            //Reset Parallax khi đổi phòng
            ParallaxCamera pCam = Camera.main.GetComponent<ParallaxCamera>();
            if (pCam != null) pCam.ResetOldPosition();

            // 2. Reset các toàn bộ vật thể trong phòng (nếu có)
            if (myRoomManager != null)
            {
                myRoomManager.OnRoomEntered();
            }

            // 3. Ép đi lọt vào phòng (TÍNH THEO 4 CẠNH CỦA PHÒNG)
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                Bounds bounds = roomCollider.bounds;
                Vector3 playerPos = collision.transform.position;

                // Đo khoảng cách từ nhân vật đến 4 vách tường
                float distToLeft = Mathf.Abs(playerPos.x - bounds.min.x);
                float distToRight = Mathf.Abs(playerPos.x - bounds.max.x);
                float distToBottom = Mathf.Abs(playerPos.y - bounds.min.y);
                float distToTop = Mathf.Abs(playerPos.y - bounds.max.y);

                // Tìm cạnh gần nhất
                float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

                Vector2 pushVelocity = Vector2.zero;

                if (minDist == distToLeft)
                {
                    pushVelocity = new Vector2(horizontalPushSpeed, 0f); // Vào mép trái -> Đẩy sang phải
                }
                else if (minDist == distToRight)
                {
                    pushVelocity = new Vector2(-horizontalPushSpeed, 0f); // Vào mép phải -> Đẩy sang trái
                }
                else if (minDist == distToBottom)
                {
                    pushVelocity = new Vector2(0f, upwardPushSpeed); // Vào mép dưới -> Vọt mạnh lên trên
                }
                else if (minDist == distToTop)
                {
                    pushVelocity = new Vector2(0f, -downwardPushSpeed); // Vào mép trên -> Rớt xuống dưới
                }

                // Đọc thời gian blend của camera để đồng bộ với thời gian đẩy
                float cameraBlendTime = 0.75f; // Giá trị dự phòng
                CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    cameraBlendTime = brain.DefaultBlend.Time;
                }

                // Gửi lệnh đẩy đi kèm theo thời gian chờ Camera
                player.ForceWalkIntoRoom(pushVelocity, pushDuration, cameraBlendTime);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Khi nhân vật bước TRỞ RA khỏi phòng, reset lại trí nhớ của phòng
            isPlayerInside = false;
        }
    }

    // Hàm vẽ viền phòng
    private void OnDrawGizmos()
    {
        if (drawRoomBoudary)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                // Chọn một màu nổi bật (VD: Màu vàng trong suốt)
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);

                // Vẽ một cái khung viền bao quanh toàn bộ Collider của phòng
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}