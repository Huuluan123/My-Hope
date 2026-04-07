using UnityEngine;
using Unity.Cinemachine;

public class RoomTransitionScript : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera roomCamera;

    [Header("Auto Walk Settings")]
    [SerializeField] private float pushDuration = 0.2f;
    [SerializeField] private float horizontalPushSpeed = 8f;
    [SerializeField] private float downwardPushSpeed = 8f;
    [SerializeField] private float upwardPushSpeed = 18f;

    [Header("Spawn Protection")]
    [SerializeField] private float safeTimeAfterSpawn = 0.5f;

    private Collider2D roomCollider;
    private RoomManager myRoomManager;
    public bool drawRoomBoudary = true;

    public static RoomTransitionScript ActiveRoom;

    private void Awake()
    {
        roomCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        myRoomManager = GetComponentInParent<RoomManager>();

        // KIỂM TRA LÚC SPAWN
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (roomCollider.bounds.Contains(player.transform.position))
            {
                ActiveRoom = this; // Đăng ký đây là phòng hiện tại
                CameraManager.SwitchCamera(roomCamera);
                ParallaxCamera pCam = Camera.main.GetComponent<ParallaxCamera>();
                if (pCam != null) pCam.ResetOldPosition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player == null) return;

            // 1. TẤM KHIÊN BẢO VỆ SPAWN/RESPAWN
            if (Time.unscaledTime - player.lastSpawnTime < safeTimeAfterSpawn)
            {
                ActiveRoom = this;
                CameraManager.SwitchCamera(roomCamera);
                ParallaxCamera pCam = Camera.main.GetComponent<ParallaxCamera>();
                if (pCam != null) pCam.ResetOldPosition();
                return;
            }

            // Nếu người chơi ĐÃ Ở SẴN trong phòng này rồi -> Bỏ qua
            if (ActiveRoom == this) return;

            // 2. 🌟 KIỂM TRA XEM CÓ ĐẾN TỪ PHÒNG KHÁC KHÔNG
            // Nếu ActiveRoom != null, tức là người chơi vừa bước từ phòng A sang phòng B
            // Nếu ActiveRoom == null, tức là người chơi vừa rơi từ vùng không gian trống vào
            bool cameFromAnotherRoom = (ActiveRoom != null);

            // Cập nhật lại định vị: Phòng hiện tại giờ là phòng này
            ActiveRoom = this;

            // 3. Chuyển Camera & Reset vật thể
            float cameraBlendTime = 0.75f;
            CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null) cameraBlendTime = brain.DefaultBlend.Time;

            CameraManager.SwitchCamera(roomCamera);
            ParallaxCamera pCam2 = Camera.main.GetComponent<ParallaxCamera>();
            if (pCam2 != null) pCam2.ResetOldPosition();

            if (myRoomManager != null) myRoomManager.OnRoomEntered();

            // 4. CHỈ ÉP ĐẨY NẾU NGƯỜI CHƠI ĐẾN TỪ PHÒNG KHÁC
            if (cameFromAnotherRoom)
            {
                Bounds bounds = roomCollider.bounds;
                Vector3 playerPos = collision.transform.position;

                float distToLeft = Mathf.Abs(playerPos.x - bounds.min.x);
                float distToRight = Mathf.Abs(playerPos.x - bounds.max.x);
                float distToBottom = Mathf.Abs(playerPos.y - bounds.min.y);
                float distToTop = Mathf.Abs(playerPos.y - bounds.max.y);

                float minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

                Vector2 pushVelocity = Vector2.zero;

                if (minDist == distToLeft) pushVelocity = new Vector2(horizontalPushSpeed, 0f);
                else if (minDist == distToRight) pushVelocity = new Vector2(-horizontalPushSpeed, 0f);
                else if (minDist == distToBottom) pushVelocity = new Vector2(0f, upwardPushSpeed);
                else if (minDist == distToTop) pushVelocity = new Vector2(0f, -downwardPushSpeed);

                player.ForceWalkIntoRoom(pushVelocity, pushDuration, cameraBlendTime);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 🌟 LOGIC CHỐNG XUNG ĐỘT (Race Condition):
            // Nếu người chơi rời khỏi phòng này, VÀ phòng này vẫn đang được ghi nhận là phòng hiện tại
            // -> Nghĩa là người chơi đã bước ra ngoài một "vùng trống" không thuộc về bất kỳ phòng nào.
            if (ActiveRoom == this)
            {
                ActiveRoom = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (drawRoomBoudary)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}