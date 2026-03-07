using UnityEngine;
using Unity.Cinemachine;

public class RoomTransitionScript : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera roomCamera;

    [Header("Auto Walk Settings")]
    [Tooltip("Thời gian ép đi (Giây)")]
    [SerializeField] private float pushDuration = 0.2f;

    private Collider2D roomCollider;
    private bool isPlayerInside = false; // Biến ghi nhớ trạng thái

    private void Awake()
    {
        roomCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
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

            // 2. Ép đi lọt vào phòng
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                Vector3 roomCenter = roomCollider.bounds.center;
                float dirX = collision.transform.position.x < roomCenter.x ? 1f : -1f;
                Vector2 autoDirection = new Vector2(dirX, 0f);

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