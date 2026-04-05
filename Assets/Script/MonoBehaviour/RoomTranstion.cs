using UnityEngine;
using Unity.Cinemachine;

public class RoomTransitionScript : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera roomCamera;

    [Header("Auto Walk Settings")]
    [Tooltip("Time to push player (Seconds)")]
    [SerializeField] private float pushDuration = 0.2f;

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

            // 3. Ép đi lọt vào phòng
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // Lấy tọa độ tâm phòng và tọa độ hiện tại của người chơi
                Vector3 roomCenter = roomCollider.bounds.center;
                Vector3 playerPos = collision.transform.position;

                // Tính khoảng cách từ người chơi nhìn về hướng tâm phòng
                float diffX = roomCenter.x - playerPos.x;
                float diffY = roomCenter.y - playerPos.y;

                Vector2 autoDirection = Vector2.zero;

                // Trục nào có khoảng cách chênh lệch lớn hơn thì ta ưu tiên đẩy theo trục đó
                if (Mathf.Abs(diffX) > Mathf.Abs(diffY))
                {
                    // Đi vào từ cửa ngang (Trái hoặc Phải)
                    // Mathf.Sign sẽ trả về 1 nếu diffX dương (đẩy sang phải), và -1 nếu diffX âm (đẩy sang trái)
                    autoDirection = new Vector2(Mathf.Sign(diffX), 0f);
                }
                else
                {
                    // Đi vào từ cửa dọc (Rớt từ trên trần xuống, hoặc nảy từ dưới hố lên)
                    autoDirection = new Vector2(0f, Mathf.Sign(diffY));
                }

                // Gọi lệnh ép di chuyển bên script Player
                player.ForceWalkIntoRoom(autoDirection, pushDuration);
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