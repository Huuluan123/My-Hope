using UnityEngine;

public static class SystemBootstrapper
{
    // Cờ đặc biệt này bảo Unity: "Hãy chạy hàm này NGAY TRƯỚC KHI bất kỳ Scene nào được load!"
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ExecuteRoutine()
    {
        // Kiểm tra xem đã có GameManager nào trong cảnh chưa (để tránh tạo đúp)
        if (GameManager.Instance == null)
        {
            // Tự động thò tay vào thư mục Resources, lấy Prefab và đẻ ra đời thực!
            GameObject managerPrefab = Resources.Load<GameObject>("[GAME_MANAGERS]");

            if (managerPrefab != null)
            {
                Object.Instantiate(managerPrefab);
                Debug.Log("Hệ thống Managers đã được TỰ ĐỘNG triệu hồi thành công!");
            }
            else
            {
                Debug.LogError("Không tìm thấy Prefab [GAME_MANAGERS] trong thư mục Resources!");
            }
        }
        else Debug.Log("Đã có GameManager tồn tại, không cần tạo thêm.");
    }
}