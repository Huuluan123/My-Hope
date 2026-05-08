using UnityEngine;
using System.Collections;

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

    public void OnRoomExited(float delayTime = 0f)
    {
        // LỚP PHÒNG THỦ: Kiểm tra xem Object có đang bị tắt (do tắt game) hay không?
        // Nếu đã bị tắt -> Lập tức thoát hàm, không chạy Coroutine nữa!
        if (!gameObject.activeInHierarchy) return;

        if (delayTime > 0f)
        {
            // Bắt đầu tiến trình đếm ngược ngầm
            StartCoroutine(DisableRoomRoutine(delayTime));
        }
        else
        {
            // Nếu không có delay (như lúc mới bật game), tắt ngay lập tức
            PerformDisable();
        }
    }

    // Đây là Coroutine: Tiến trình đếm thời gian
    private IEnumerator DisableRoomRoutine(float delayTime)
    {
        // Đợi đúng bằng thời gian Blend của Camera
        yield return new WaitForSeconds(delayTime);

        // Sau khi đợi xong, mới thực sự tắt đồ vật
        PerformDisable();
    }

    // Đưa logic tắt đồ vật vào một hàm riêng biệt để code gọn gàng, tái sử dụng được
    private void PerformDisable()
    {
        IRoomElement[] allElements = GetComponentsInChildren<IRoomElement>(true);

        foreach (IRoomElement element in allElements)
        {
            MonoBehaviour mono = element as MonoBehaviour;
            if (mono != null)
            {
                mono.gameObject.SetActive(false);
            }
        }
    }
}