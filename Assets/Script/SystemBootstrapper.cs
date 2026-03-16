using UnityEngine;

public static class SystemBootstrapper
{
    // Script load GAME_MANAGERS vào mọi scene
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
                Debug.Log("Succesfully instantiate [GAME_MANAGERS]");
            }
            else
            {
                Debug.LogError("Cannot find [GAME_MANAGERS] in Resources!!!");
            }
        }
    }
}