using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;

public class LocalizationPuller : EditorWindow
{
    // 🌟 THAY THẾ: Dán cái SPREADSHEET ID bạn vừa copy ở Bước 1 vào chỗ [NHẬP_ID_VÀO_ĐÂY]
    private static string spreadsheetId = "1a02MeofYypO5dNoKIV92XfXrJt4vnwT4N5lPv0JuFUQ";

    // Đường dẫn file đích trong máy tính của bạn (Sửa lại cho đúng với dự án của bạn)
    private static string savePath = "Assets/Localization/UI_Text.tsv";

    // Tạo một nút bấm trên thanh Menu của Unity
    [MenuItem("Tools/Localization/Pull From Google Sheets")]
    public static void PullDataFromGoogleSheets()
    {
        // Tạo đường link bí mật để ép Google Sheets xuất file dạng TSV (Tab delimited)
        string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=tsv";

        Debug.Log("<color=#FFFF00>[Localization]</color> Connecting to Google Sheets...");

        // Gửi yêu cầu tải file qua Internet
        UnityWebRequest www = UnityWebRequest.Get(url);
        var operation = www.SendWebRequest();

        // Vì chạy trong môi trường Editor nên ta ép luồng dừng chờ tải xong (mất khoảng 1-2 giây)
        while (!operation.isDone) { }

        if (www.result == UnityWebRequest.Result.Success)
        {
            string downloadedText = www.downloadHandler.text;

            // Kiểm tra xem file tải về có bị rỗng không
            if (string.IsNullOrEmpty(downloadedText) || downloadedText.StartsWith("<!DOCTYPE html>"))
            {
                Debug.LogError("[Localization] LỖI: Không thể tải dữ liệu. Hãy kiểm tra lại quyền Chia Sẻ của file Google Sheets!");
                return;
            }

            // 1. Tạo thư mục nếu chưa tồn tại
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // 2. Ghi đè nội dung mới xuống file .txt trên ổ cứng máy tính
            File.WriteAllText(savePath, downloadedText);

            // 3. 🌟 CÚ HÍCH QUAN TRỌNG: Ép Unity Refresh lại Database để nhận diện file mới ngay lập tức
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00FF00>[Localization] THÀNH CÔNG!</color> Đã cập nhật file tại: {savePath}");
        }
        else
        {
            Debug.LogError("[Localization] Lỗi kết nối mạng: " + www.error);
        }
    }
}