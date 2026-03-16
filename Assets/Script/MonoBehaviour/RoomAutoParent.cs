using UnityEngine;

[ExecuteInEditMode]
public class RoomAutoParent : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    private float nextCheckTime = 0f;
    private float checkInterval = 0.5f; // Cứ 0.5 giây mới quét 1 lần thay vì quét mỗi khung hình

    private void OnEnable()
    {
        roomBounds = GetComponentInChildren<BoxCollider2D>();
    }

    private void Update()
    {
        // 1. CHỐNG LAG: Chỉ cho phép tự động bắt vật thể khi đang thiết kế (Edit Mode).
        // Khi người chơi đang chơi (Play Mode), tắt tính năng này đi để đỡ tốn CPU.
        if (Application.isPlaying) return;

        if (roomBounds == null) return;

        // TỐI ƯU HÓA: Chỉ quét 0.5 giây một lần để Editor không bị khựng (lag) khi kéo thả
        if (Time.realtimeSinceStartup < nextCheckTime) return;
        nextCheckTime = Time.realtimeSinceStartup + checkInterval;

        // 2. TÌM KIẾM BẰNG INTERFACE: Quét toàn bộ MonoBehaviour trong cảnh
        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MonoBehaviour script in allScripts)
        {
            // Nếu script này có ký hợp đồng IRoomElement
            if (script is IRoomElement)
            {
                Transform obj = script.transform;

                // Kiểm tra xem tâm của vật thể có nằm TRONG vùng BoxCollider của phòng không
                if (roomBounds.bounds.Contains(obj.position))
                {
                    // Nếu nằm trong phòng, nhưng chưa làm con -> Nhận làm con ngay!
                    if (obj.parent != transform)
                    {
                        obj.SetParent(transform);
                    }
                }
                else
                {
                    // Nếu vật thể LÀ CON của phòng, nhưng bị Level Designer kéo ra NGOÀI vùng
                    if (obj.parent == transform)
                    {
                        // Đá nó ra khỏi phòng (trả về Root Hierarchy)
                        obj.SetParent(null);
                    }
                }
            }
        }
    }
}