using UnityEngine;

[ExecuteInEditMode]
public class RoomAutoParent : MonoBehaviour
{
    private BoxCollider2D roomBounds;

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

        // 2. TÌM KIẾM: Tìm tất cả vật thể có tag là "LevelElement" (Quái, Bẫy, Tiền...)
        // LƯU Ý: Hãy nhớ tạo Tag "LevelElement" và gắn cho các Prefab bạn muốn phòng tự hút vào.
        GameObject[] elements = GameObject.FindGameObjectsWithTag("LevelElement");

        foreach (GameObject obj in elements)
        {
            // Kiểm tra xem tâm của vật thể có nằm TRONG vùng BoxCollider của phòng không
            if (roomBounds.bounds.Contains(obj.transform.position))
            {
                // Nếu nằm trong phòng, nhưng chưa làm con -> Nhận làm con ngay!
                if (obj.transform.parent != transform)
                {
                    obj.transform.SetParent(transform);
                }
            }
            else
            {
                // Nếu vật thể LÀ CON của phòng, nhưng bị Level Designer kéo ra NGOÀI vùng
                if (obj.transform.parent == transform)
                {
                    // Đá nó ra khỏi phòng (trả về Root Hierarchy)
                    obj.transform.SetParent(null);
                }
            }
        }
    }
}