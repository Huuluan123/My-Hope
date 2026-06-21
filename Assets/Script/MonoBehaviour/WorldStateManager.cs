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

    [Header("Data Manager (Editor)")]
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

            // Tự động tải dữ liệu khi mở game
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        // Đồng bộ liên tục từ Inspector vào RAM bất kể game đang chạy hay tắt
        // để đảm bảo dữ liệu gõ tay luôn được ghi nhận vào bộ nhớ ngầm
        SyncListToDictionary();
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

    public int GetStateCount()
    {
        return globalStates.Count;
    }

    private string GetSaveFilePath()
    {
        return Path.Combine(Application.dataPath, "WorldState.json");
    }

    // ==========================================
    // HỆ THỐNG LƯU / ĐỌC FILE JSON
    // ==========================================

    [ContextMenu("Save Game (Save to JSON)")]
    public void SaveGame()
    {
        if (Application.isPlaying)
        {
            // Nếu GAME ĐANG CHẠY: RAM (Dictionary) là gốc -> Đè dữ liệu vào List để lưu
            SyncDictionaryToList();
        }
        else
        {
            // Nếu GAME ĐANG TẮT (Edit Mode): Inspector (List) là gốc -> Nạp vào RAM trước khi lưu
            SyncListToDictionary();
        }

        // Gói dữ liệu lại
        WorldStateData dataToSave = new WorldStateData();
        dataToSave.states = visibleStates;

        // Chuyển thành chuỗi văn bản JSON và format đẹp mắt (true)
        string json = JsonUtility.ToJson(dataToSave, true);

        // Ghi ra file
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log($"[WorldState] Đã LƯU thành công tại: {GetSaveFilePath()}");
    }

    [ContextMenu("Load Game (Load from JSON)")]
    public void LoadGame()
    {
        string path = GetSaveFilePath(); // Lấy đường dẫn
        if (File.Exists(path))
        {
            // Đọc văn bản từ file
            string json = File.ReadAllText(path);

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

    [ContextMenu("Clear all data (Clear Data)")]
    public void ClearAllData()
    {
        globalStates.Clear();
        visibleStates.Clear();

        string path = GetSaveFilePath(); // Lấy đường dẫn
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