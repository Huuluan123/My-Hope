using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public void OnRoomEntered()
    {
        // 1. Tìm TẤT CẢ các vật thể con (kể cả những đứa đang bị SetActive(false))
        IRoomElement[] allElements = GetComponentsInChildren<IRoomElement>(true);

        // 2. Bật chúng lên và ra lệnh tự reset
        foreach (IRoomElement element in allElements)
        {
            // Ép kiểu an toàn để lấy GameObject
            MonoBehaviour mono = element as MonoBehaviour;
            if (mono != null)
            {
                mono.gameObject.SetActive(true); // BẬT VẬT THỂ
            }

            element.ResetState(); // RESET TRẠNG THÁI
        }
    }

    public void OnRoomExited()
    {
        // 1. Tìm tất cả các vật thể con mang hợp đồng
        IRoomElement[] allElements = GetComponentsInChildren<IRoomElement>(true);

        // 2. Tắt chúng đi để tiết kiệm CPU
        foreach (IRoomElement element in allElements)
        {
            MonoBehaviour mono = element as MonoBehaviour;
            if (mono != null)
            {
                mono.gameObject.SetActive(false); // TẮT VẬT THỂ
            }
        }
    }
}