#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

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
    public bool hasSavedPosition = false;
    public Vector2 savedRespawnPoint = Vector2.zero;
    public int savedMaxAirJumps = 0;
    public int savedMaxGrappleCount = 2;
    public int savedMaxDashes = 0;
    public int spawnScene = 1;
    public int savedCoins = 0;
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

    [SerializeField] private bool isEditiorDebugMode = false;

    // Bộ nhớ RAM
    private Dictionary<string, bool> globalStates = new Dictionary<string, bool>();

    [Header("Default New Game States")]
    public List<StateEntry> defaultStates = new List<StateEntry>();

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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

    [ContextMenu("Save game (Save Master)")]
    public void SaveGame()
    {
        if (Application.isPlaying)
        {
            // 1. Đồng bộ Thế giới từ RAM ra danh sách
            SyncDictionaryToList();

            // 2. Cập nhật Dữ liệu Người chơi MỚI NHẤT từ nhân vật đang chơi
            if (PlayerController.Instance != null)
            {
                currentPlayerSave.hasSavedPosition = true;

                currentPlayerSave.savedRespawnPoint = PlayerController.Instance.CurrentRespawnPoint;
                currentPlayerSave.savedMaxAirJumps = PlayerController.Instance.maxAirJumps;
                currentPlayerSave.savedMaxGrappleCount = PlayerController.Instance.maxGrappleCount;
                currentPlayerSave.savedMaxDashes = PlayerController.Instance.maxDashes;

                // SAVE ECONOMY DATA: Get the current coins from the player's wallet
                PlayerWallet wallet = PlayerController.Instance.GetComponent<PlayerWallet>();
                if (wallet != null)
                {
                    currentPlayerSave.savedCoins = wallet.currentCoins;
                }
            }

            // 3. TỰ ĐỘNG GHI ĐÈ SỐ HIỆU SCENE HIỆN TẠI KHI LƯU GAME
            currentPlayerSave.spawnScene = SceneManager.GetActiveScene().buildIndex;
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

    [ContextMenu("Load game (Load Master)")]
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

            globalStates.Clear();

            // Rút dữ liệu Mặc định (Default States) bơm thẳng vào RAM
            foreach (StateEntry entry in defaultStates)
            {
                if (!globalStates.ContainsKey(entry.stateName))
                {
                    globalStates.Add(entry.stateName, entry.stateValue);
                }
            }

            // Đồng bộ RAM ra lại Gương (visibleStates) để Inspector và SaveGame hoạt động đúng
            SyncDictionaryToList();
        }
    }

    // Hàm bơm dữ liệu ngược lại vào PlayerController
    public void ApplyPlayerData()
    {
#if UNITY_EDITOR
        if (isEditiorDebugMode)
        {
            Debug.Log("<color=yellow>[SaveManager] Đang ở chế độ Debug! Đã bỏ qua việc nạp vị trí và thông số cho Player.</color>");
            return; // Thoát hàm ngay lập tức, không gán gì cả!
        }
#endif
        // CHỈ DỊCH CHUYỂN KHI ĐÃ TỪNG SAVE
        if (currentPlayerSave.hasSavedPosition)
        {
            PlayerController.Instance.SetRespawnPoint(currentPlayerSave.savedRespawnPoint);
            PlayerController.Instance.transform.position = currentPlayerSave.savedRespawnPoint;
        }
        else
        {
            // Nếu chưa từng Save, cập nhật CurrentRespawnPoint bằng chính vị trí đặt trên Editor
            PlayerController.Instance.SetRespawnPoint(PlayerController.Instance.transform.position);
            Debug.Log("[SaveManager] New Game: Giữ nguyên vị trí Player trên Editor.");
        }

        PlayerController.Instance.maxAirJumps = currentPlayerSave.savedMaxAirJumps;
        PlayerController.Instance.maxGrappleCount = currentPlayerSave.savedMaxGrappleCount;
        PlayerController.Instance.maxDashes = currentPlayerSave.savedMaxDashes;
    }

    [ContextMenu("Clear all data (Clear Master)")]
    public void ClearAllData()
    {
        globalStates.Clear();
        visibleStates.Clear();
        currentPlayerSave = new PlayerSaveData(); // Trả về thông số rỗng

        string path = GetSaveFilePath();
        if (File.Exists(path)) File.Delete(path);

        foreach (StateEntry entry in defaultStates)
        {
            SetState(entry.stateName, entry.stateValue);
        }

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