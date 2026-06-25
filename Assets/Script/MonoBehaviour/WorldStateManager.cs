#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using System.IO;

// 1. Cấu trúc Cờ Sự Kiện (Giữ nguyên)
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

// 2. Cấu trúc Dữ liệu Người chơi
[System.Serializable]
public class PlayerSaveData
{
    public Vector2 savedRespawnPoint;
    public int savedMaxAirJumps;
    public int savedMaxGrappleCount;
}

// 3. Gộp cả dữ liệu của thế giới và người chơi
[System.Serializable]
public class MasterSaveData
{
    public PlayerSaveData playerData = new PlayerSaveData();
    public List<StateEntry> worldStates = new List<StateEntry>();
}

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance;

    // Bộ nhớ RAM
    private Dictionary<string, bool> globalStates = new Dictionary<string, bool>();

    [Header("World data manager (Editor)")]
    public List<StateEntry> visibleStates = new List<StateEntry>();

    [Header("Player data manager (Editor)")]
    [Tooltip("Bạn có thể chỉnh sửa các chỉ số này lúc tắt game để Test")]
    public PlayerSaveData currentPlayerSave = new PlayerSaveData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        SyncListToDictionary();
    }

    private string GetSaveFilePath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "MasterSave.json");
#else
        return Path.Combine(Application.persistentDataPath, "MasterSave.json");
#endif
    }

    // ==========================================
    // CÁC HÀM TƯƠNG TÁC THẾ GIỚI
    // ==========================================
    public void SetState(string stateName, bool stateValue)
    {
        if (globalStates.ContainsKey(stateName))
        {
            globalStates[stateName] = stateValue;
        }
        else
        {
            globalStates.Add(stateName, stateValue);
        }
        SyncDictionaryToList();
        Debug.Log($"[SaveManager] Cập nhật sự kiện: {stateName} = {stateValue}");
    }

    public bool CheckState(string stateName)
    {
        if (globalStates.ContainsKey(stateName))
        {
            return globalStates[stateName];
        }
        return false;
    }

    // ==========================================
    // HỆ THỐNG LƯU / ĐỌC TỔNG HỢP (MASTER SAVE)
    // ==========================================

    [ContextMenu("Lưu Toàn Bộ Game (Save Master)")]
    public void SaveGame()
    {
        if (Application.isPlaying)
        {
            // 1. Đồng bộ Thế giới từ RAM ra danh sách
            SyncDictionaryToList();

            // 2. Cập nhật Dữ liệu Người chơi MỚI NHẤT từ nhân vật đang chơi
            if (PlayerController.Instance != null)
            {
                currentPlayerSave.savedRespawnPoint = PlayerController.Instance.CurrentRespawnPoint;
                currentPlayerSave.savedMaxAirJumps = PlayerController.Instance.maxAirJumps;
                currentPlayerSave.savedMaxGrappleCount = PlayerController.Instance.maxGrappleCount;
            }
        }
        else
        {
            // Nếu tắt game thì lưu những gì gõ tay trên Inspector
            SyncListToDictionary();
        }

        // Đóng gói vào "Vali Lớn"
        MasterSaveData dataToSave = new MasterSaveData
        {
            playerData = currentPlayerSave,
            worldStates = visibleStates
        };

        string json = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log($"[SaveManager] Đã LƯU Toàn Bộ Game tại: {GetSaveFilePath()}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    [ContextMenu("Đọc Toàn Bộ Game (Load Master)")]
    public void LoadGame()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            MasterSaveData loadedData = JsonUtility.FromJson<MasterSaveData>(json);

            // 1. Tải dữ liệu Thế giới
            visibleStates = loadedData.worldStates;
            SyncListToDictionary();

            // 2. Tải dữ liệu Người chơi
            currentPlayerSave = loadedData.playerData;

            // 3. Nếu game đang chạy, BƠM dữ liệu vào nhân vật!
            if (PlayerController.Instance != null)
            {
                ApplyPlayerData();
            }

            Debug.Log("[SaveManager] Đã ĐỌC dữ liệu thành công!");
        }
        else
        {
            Debug.LogWarning("[SaveManager] Không tìm thấy file Save. Bắt đầu game mới!");
        }
    }

    // Hàm bơm dữ liệu ngược lại vào PlayerController
    public void ApplyPlayerData()
    {
        PlayerController.Instance.SetRespawnPoint(currentPlayerSave.savedRespawnPoint);
        PlayerController.Instance.maxAirJumps = currentPlayerSave.savedMaxAirJumps;
        PlayerController.Instance.maxGrappleCount = currentPlayerSave.savedMaxGrappleCount;

        // Dịch chuyển người chơi về đúng cái ghế đá đã lưu
        PlayerController.Instance.transform.position = currentPlayerSave.savedRespawnPoint;
    }

    [ContextMenu("Xóa Toàn Bộ Dữ Liệu (Clear Master)")]
    public void ClearAllData()
    {
        globalStates.Clear();
        visibleStates.Clear();
        currentPlayerSave = new PlayerSaveData(); // Trả về thông số rỗng

        string path = GetSaveFilePath();
        if (File.Exists(path)) File.Delete(path);

        Debug.Log("[SaveManager] Đã xóa trắng mọi dữ liệu Cốt truyện và Người chơi!");
    }

    // ==========================================
    // HÀM HỖ TRỢ ĐỒNG BỘ THẾ GIỚI
    // ==========================================
    private void SyncDictionaryToList()
    {
        visibleStates.Clear();
        foreach (KeyValuePair<string, bool> pair in globalStates)
            visibleStates.Add(new StateEntry(pair.Key, pair.Value));
    }

    private void SyncListToDictionary()
    {
        globalStates.Clear();
        foreach (StateEntry entry in visibleStates)
            if (!globalStates.ContainsKey(entry.stateName))
                globalStates.Add(entry.stateName, entry.stateValue);
    }
}