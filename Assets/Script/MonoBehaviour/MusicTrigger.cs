using UnityEngine;

public enum MusicTriggerAction
{
    InterruptAndPlayNew, // Tạm dừng nhạc hiện tại, chèn nhạc mới vào (Bắt đầu cao trào)
    ResumePreviousMusic, // Tắt nhạc cao trào, quay về nhạc cũ (Kết thúc cao trào)
    PlayNormal,          // Đổi nhạc hoàn toàn, quên bài cũ đi (Chuyển sang Level 2)
    PauseOnly,           // Chỉ tạm dừng nhạc hiện tại mà không phát bài mới (Dành cho trường hợp đặc biệt, nếu cần)
    ResumeOnly           // Chỉ tiếp tục phát bài đã tạm dừng mà không cần phát bài mới (Dành cho trường hợp đặc biệt, nếu cần)
}

public class MusicTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public MusicTriggerAction actionType;

    [Tooltip("Tên bài hát mới (Chỉ cần điền nếu chọn Interrupt hoặc PlayNormal)")]
    public string targetTrackName;
    public float crossfadeDuration = 1.0f;
    public bool playOnlyOnce = true;

    private bool hasPlayed = false;

    // --- 1. LỐI VÀO TỰ ĐỘNG (Dành cho va chạm vật lý) ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ExecuteTrigger();
        }
    }

    // --- 2. HÀM ĐÓNG GÓI RIÊNG (Dành cho gọi từ bên ngoài) ---
    public void ExecuteTrigger()
    {
        // Chốt chặn Play Only Once
        if (hasPlayed && playOnlyOnce) return;

        if (MusicManager.Instance != null)
        {
            if (playOnlyOnce) hasPlayed = true;

            switch (actionType)
            {
                case MusicTriggerAction.InterruptAndPlayNew:
                    MusicManager.Instance.PauseCurrentAndPlayNew(targetTrackName, crossfadeDuration);
                    break;

                case MusicTriggerAction.ResumePreviousMusic:
                    MusicManager.Instance.ResumePausedMusic(crossfadeDuration);
                    break;

                case MusicTriggerAction.PlayNormal:
                    MusicManager.Instance.PlayMusic(targetTrackName, crossfadeDuration);
                    break;
                case MusicTriggerAction.PauseOnly:
                    MusicManager.Instance.PauseMusicOnly(crossfadeDuration);
                    break;
                case MusicTriggerAction.ResumeOnly:
                    MusicManager.Instance.ResumeMusicOnly(crossfadeDuration);
                    break;
            }

            Debug.Log($"[MusicTrigger] Đã thực thi hành động: {actionType} trên track: {targetTrackName}");
        }
    }

    // Hàm bổ trợ nếu bạn muốn reset trigger bằng code
    public void ResetTrigger()
    {
        hasPlayed = false;
    }
}