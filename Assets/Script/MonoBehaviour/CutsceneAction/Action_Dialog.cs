using UnityEngine;
using System.Collections;

public class Action_Dialog : CutsceneAction
{
    [Header("Ink Story")]
    [Tooltip("File JSON chứa nội dung hội thoại xuất ra từ Inky")]
    public TextAsset inkJSON;
    [Header("Ink Settings")]
    public string knotName;

    public override IEnumerator Execute()
    {
        // 1. Kiểm tra an toàn
        if (inkJSON == null || DialogueManager.Instance == null)
        {
            Debug.LogError("Thiếu file Ink JSON hoặc DialogueManager chưa có trong Scene!");
            yield break; // Thoát lỗi ngay lập tức
        }

        // 2. Kích hoạt Hội thoại (Giống cách DialogueTrigger làm)
        DialogueManager.Instance.StartDialogue(inkJSON, knotName);

        // 3. VÒNG LẶP CHỜ ĐỢI
        // Dừng toàn bộ chuỗi Cutscene lại để chờ người chơi đọc và bấm nút qua thoại.
        // Vòng lặp này sẽ liên tục kiểm tra biến isDialogueActive ở Bước 1.
        while (DialogueManager.Instance.isDialogueActive)
        {
            yield return null; // Đợi sang frame kế tiếp rồi kiểm tra lại
        }

        // 4. Hội thoại đã kết thúc (Bao gồm cả thời gian chờ UI thu nhỏ lại 0.3s)
        // Hệ thống sẽ tự động nhả lệnh để chạy khối Action tiếp theo của Cutscene!
    }
}