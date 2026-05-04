using UnityEngine;
using System.Collections;

public class Action_ChangeMusic : CutsceneAction
{
    [Header("Music Settings")]
    [Tooltip("Kéo thả Object có gắn script MusicTrigger vào đây")]
    public MusicTrigger musicTrigger;

    [Tooltip("Tick chọn nếu muốn hệ thống Cutscene thủ công đứng chờ chuyển nhạc xong mới chạy hành động tiếp theo.")]
    public bool waitForCrossfade = false;

    // --- 1. DÀNH CHO TIMELINE (KÍCH HOẠT QUA SIGNAL) ---
    public void ActivateMusicChange()
    {
        // Timeline thường có timeline tự chạy, nên ta chỉ cần trigger chứ không cần ép Timeline đợi
        StartCoroutine(Execute());
    }

    // --- 2. DÀNH CHO HỆ THỐNG CUTSCENE THỦ CÔNG ---
    public override IEnumerator Execute()
    {
        // Kiểm tra an toàn để tránh lỗi NullReference
        if (musicTrigger == null)
        {
            Debug.LogError("Action_ChangeMusic: Chưa gán tham chiếu MusicTrigger!");
            yield break; // Thoát ngay lập tức
        }

        // Gọi hàm đổi nhạc đã đóng gói từ MusicTrigger mà chúng ta làm trước đó
        musicTrigger.ExecuteTrigger();

        // Xử lý thời gian chờ cho Cutscene thủ công
        if (waitForCrossfade)
        {
            // Nếu muốn đợi, ta sẽ lấy luôn thời gian crossfadeDuration từ MusicTrigger để chờ
            // Dùng WaitForSeconds bình thường vì cutscene thường chạy ở TimeScale = 1
            yield return new WaitForSeconds(musicTrigger.crossfadeDuration);
        }
        else
        {
            // Trả về ngay lập tức để hệ thống Cutscene gọi khối lượng Action tiếp theo
            yield return null;
        }
    }
}