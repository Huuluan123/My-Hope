using System.Collections.Generic;
using UnityEngine;
using System.IO;

// 1. Tạo một cấu trúc dữ liệu cơ bản để Unity và JSON có thể đọc được
[System.Serializable]
public class StateEntry
{
    public string stateName;
    public bool stateValue;

    public StateEntry(string name, bool value)
    {
        stateName = name;
        stateValue = value;
    }
}

// 2. Tạo một lớp "Gói" chứa danh sách các trạng thái để lưu thành file
[System.Serializable]
public class WorldStateData
{
    public List<StateEntry> states = new List<StateEntry>();
}

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance;

    // Bộ nhớ RAM (Từ điển): Chạy rất nhanh, dùng trong lúc chơi game
    private Dictionary<string, bool> globalStates = new Dictionary<string, bool>();

    [Header("Quản lý Dữ Liệu (Editor)")]
    [Tooltip("Danh sách này hiển thị các trạng thái để bạn dễ theo dõi và chỉnh sửa")]
    public List<StateEntry> visibleStates = new List<StateEntry>();

    // Đường dẫn lưu file trên ổ cứng
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Thiết lập đường dẫn an toàn (Hoạt động trên cả PC, Mac, Android, iOS)
            saveFilePath = Path.Combine(Application.persistentDataPath, "WorldState.json");

            // Tự động tải dữ liệu khi mở game
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // CÁC HÀM TƯƠNG TÁC CHÍNH TRONG GAME
    // ==========================================

    public void SetState(string stateName, bool stateValue)
    {
        // 1. Cập nhật vào Từ điển (Bộ nhớ nhanh)
        if (globalStates.ContainsKey(stateName))
        {
            globalStates[stateName] = stateValue;
        }
        else
        {
            globalStates.Add(stateName, stateValue);
        }

        // 2. Cập nhật vào Danh sách (Để xem trên Editor và lưu File)
        SyncDictionaryToList();

        Debug.Log($"[WorldState] Đã cập nhật sự kiện: {stateName} = {stateValue}");
    }

    public bool CheckState(string stateName)
    {
        if (globalStates.ContainsKey(stateName))
        {
            return globalStates[stateName];
        }
        return false; // Mặc định là chưa xảy ra
    }

    // ==========================================
    // HỆ THỐNG LƯU / ĐỌC FILE JSON
    // ==========================================

    [ContextMenu("Lưu Game (Save to JSON)")]
    public void SaveGame()
    {
        // Đồng bộ trước khi lưu cho chắc chắn
        SyncDictionaryToList();

        // Gói dữ liệu lại
        WorldStateData dataToSave = new WorldStateData();
        dataToSave.states = visibleStates;

        // Chuyển thành chuỗi văn bản JSON và format đẹp mắt (true)
        string json = JsonUtility.ToJson(dataToSave, true);

        // Ghi ra file
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[WorldState] Đã LƯU thành công tại: {saveFilePath}");
    }

    [ContextMenu("Đọc Game (Load from JSON)")]
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            // Đọc văn bản từ file
            string json = File.ReadAllText(saveFilePath);

            // Chuyển từ JSON ngược lại thành dữ liệu
            WorldStateData loadedData = JsonUtility.FromJson<WorldStateData>(json);

            // Cập nhật Danh sách trên Editor
            visibleStates = loadedData.states;

            // Bơm ngược dữ liệu vào Từ điển để game sử dụng
            SyncListToDictionary();

            Debug.Log("[WorldState] Đã ĐỌC dữ liệu thành công!");
        }
        else
        {
            Debug.Log("[WorldState] Không tìm thấy file Save cũ. Bắt đầu game mới!");
        }
    }

    [ContextMenu("Xóa toàn bộ Dữ liệu (Clear Data)")]
    public void ClearAllData()
    {
        globalStates.Clear();
        visibleStates.Clear();

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        Debug.Log("[WorldState] Đã xóa trắng mọi dữ liệu!");
    }

    // ==========================================
    // HÀM HỖ TRỢ ĐỒNG BỘ
    // ==========================================

    private void SyncDictionaryToList()
    {
        visibleStates.Clear();
        foreach (KeyValuePair<string, bool> pair in globalStates)
        {
            visibleStates.Add(new StateEntry(pair.Key, pair.Value));
        }
    }

    private void SyncListToDictionary()
    {
        globalStates.Clear();
        foreach (StateEntry entry in visibleStates)
        {
            if (!globalStates.ContainsKey(entry.stateName))
            {
                globalStates.Add(entry.stateName, entry.stateValue);
            }
        }
    }
}