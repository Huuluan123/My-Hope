using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    /*
    public PlayerProgressSO playerProgress;
    public WorldStateSO worldState;

    private string savePath;

    private void Awake()
    {
        // Đường dẫn chuẩn, an toàn trên Windows, Mac, Android, iOS
        savePath = Application.persistentDataPath + "/savegame.json";
    }

    // Gọi hàm này khi người chơi ngồi vào Trạm Save (Checkpoint)
    public void SaveGame()
    {
        // Tạo một class tạm để bọc cả 2 SO lại
        SaveData data = new SaveData();

        // Copy dữ liệu từ SO sang class tạm
        data.health = playerProgress.currentHealth;
        data.hasDash = playerProgress.hasDash;
        // ... copy các kỹ năng khác ...
        data.savedEvents = worldState.completedEvents;

        // Ép kiểu thành JSON và lưu vào ổ cứng
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log("Game Saved at: " + savePath);
    }

    // Gọi hàm này khi bấm "Continue" ở Main Menu
    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // Ghi đè dữ liệu từ ổ cứng ngược trở lại SO trên RAM
            playerProgress.currentHealth = data.health;
            playerProgress.hasDash = data.hasDash;
            worldState.completedEvents = data.savedEvents;

            Debug.Log("Game Loaded!");
        }
    }

    // Class bọc dữ liệu (Data Wrapper) dùng để tạo file JSON
    [System.Serializable]
    private class SaveData
    {
        public int health;
        public bool hasDash;
        public List<string> savedEvents;
    }
    */
}