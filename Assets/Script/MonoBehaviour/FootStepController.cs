using UnityEngine;
using FMODUnity;

public class FootstepController : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference footstepEvent; // Kéo Event Footstep vào đây

    // Biến này để lưu trữ và quản lý âm thanh bước chân
    private FMOD.Studio.EventInstance footstepInstance;

    void Start()
    {
        // Tạo sẵn một Instance (băng đài) khi game vừa bắt đầu để dùng dần
        footstepInstance = RuntimeManager.CreateInstance(footstepEvent);
    }

    // Hàm này sẽ được gọi liên tục khi nhân vật đang di chuyển
    public void PlayFootstep()
    {
        // 1. Kiểm tra trạng thái hiện tại của âm thanh
        FMOD.Studio.PLAYBACK_STATE playbackState;
        footstepInstance.getPlaybackState(out playbackState);

        // 2. Nếu âm thanh KHÔNG phải đang phát (tức là đã chạy xong hoặc chưa chạy)
        if (playbackState != FMOD.Studio.PLAYBACK_STATE.PLAYING)
        {
            // Cập nhật vị trí 3D (nếu game bạn cần âm thanh không gian)
            footstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));

            // Phát âm thanh! FMOD sẽ tự động bốc ngẫu nhiên 1 trong 8 tiếng cỏ
            footstepInstance.start();
        }
    }

    void OnDestroy()
    {
        // RẤT QUAN TRỌNG: Giải phóng bộ nhớ khi nhân vật chết hoặc chuyển scene
        if (footstepInstance.isValid())
        {
            footstepInstance.release();
        }
    }
}