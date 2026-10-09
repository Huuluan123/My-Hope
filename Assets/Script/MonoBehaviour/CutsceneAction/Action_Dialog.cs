using UnityEngine;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Action_Dialog : CutsceneAction
{
    [Header("Ink Story (Localized)")]
    [Tooltip("Gán Asset Table chứa các file INK vào đây")]
    // Thay thế TextAsset bằng LocalizedAsset
    public LocalizedAsset<TextAsset> localizedInkJSON;

    [Header("Ink Settings")]
    public string knotName;

    // Hàm này sẽ được gọi từ Timeline thông qua Signal
    public void ActivateDialogue()
    {
        // BẢO VỆ STATE NGAY LẬP TỨC: Chặn đứng DynamicCutscene nhả quyền về Idle[cite: 7]
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetState(PlayerState.Talking);
        }

        StartCoroutine(Execute());
    }

    public override IEnumerator Execute()
    {
        // 1. Kiểm tra an toàn
        if (localizedInkJSON == null || localizedInkJSON.IsEmpty)
        {
            Debug.LogError("Chưa cấu hình Localized Ink JSON cho Cutscene này!");
            yield break; // Thoát lỗi ngay lập tức
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager chưa có trong Scene!");
            yield break;
        }

        // 2. TẢI FILE INK THEO NGÔN NGỮ (BẤT ĐỒNG BỘ)
        // Yêu cầu Unity bốc đúng file Ink của ngôn ngữ hiện tại ra
        var asyncOperation = localizedInkJSON.LoadAssetAsync();

        // MA THUẬT CỦA COROUTINE: Bắt Cutscene tạm dừng 1-2 frame để chờ tải file xong
        yield return asyncOperation;

        // 3. KIỂM TRA TẢI THÀNH CÔNG VÀ CHẠY HỘI THOẠI
        if (asyncOperation.Status == AsyncOperationStatus.Succeeded)
        {
            TextAsset loadedInkFile = asyncOperation.Result;

            // Kích hoạt Hội thoại với file Ink vừa lấy được
            DialogueManager.Instance.StartDialogue(loadedInkFile, knotName);

            // 4. VÒNG LẶP CHỜ ĐỢI
            // Dừng toàn bộ chuỗi Cutscene lại để chờ người chơi đọc và bấm nút qua thoại.
            while (DialogueManager.Instance.isDialogueActive)
            {
                yield return null; // Đợi sang frame kế tiếp rồi kiểm tra lại
            }
        }
        else
        {
            Debug.LogError("Lỗi: Không thể tải file INK từ Asset Table!");
        }

        // 5. Hội thoại đã kết thúc (Bao gồm cả thời gian chờ UI thu nhỏ lại 0.3s)
        // Hệ thống sẽ tự động nhả lệnh để chạy khối Action tiếp theo của Cutscene!
    }
}