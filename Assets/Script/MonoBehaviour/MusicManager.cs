using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [System.Serializable]
    public struct MusicTrack
    {
        public string trackName;
        public EventReference fmodEvent;
    }

    [Header("FMOD Music Settings")]
    [SerializeField] private MusicTrack[] musicTracks;

    private EventInstance currentMusicInstance;
    private EventInstance pausedMusicInstance; // 🌟 BIẾN MỚI: Lưu trữ bài hát bị tạm dừng

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Hàm phát nhạc bình thường (Sẽ xóa bài cũ)
    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        EventReference eventToPlay = GetEventReference(trackName);
        if (eventToPlay.IsNull) return;
        StartCoroutine(AnimateMusicCrossfade(eventToPlay, fadeDuration, false));
    }

    // 🌟 HÀM MỚI 1: Tạm dừng bài hiện tại (Main Theme) và phát bài mới (Cao trào)
    public void PauseCurrentAndPlayNew(string trackName, float fadeDuration = 0.5f)
    {
        EventReference eventToPlay = GetEventReference(trackName);
        if (eventToPlay.IsNull) return;
        StartCoroutine(AnimateMusicCrossfade(eventToPlay, fadeDuration, true));
    }

    // 🌟 HÀM MỚI 2: Dừng bài Cao trào và tiếp tục phát lại bài Main Theme
    public void ResumePausedMusic(float fadeDuration = 0.5f)
    {
        if (!pausedMusicInstance.isValid())
        {
            Debug.LogWarning("Không có bài hát nào đang bị tạm dừng để Resume!");
            return;
        }
        StartCoroutine(AnimateResumeCrossfade(fadeDuration));
    }

    // Hàm tra cứu Event
    private EventReference GetEventReference(string trackName)
    {
        foreach (var track in musicTracks)
        {
            if (track.trackName == trackName) return track.fmodEvent;
        }
        Debug.LogWarning("FMOD MusicManager: Không tìm thấy track -> " + trackName);
        return new EventReference();
    }

    // Coroutine xử lý Fade và Pause
    private IEnumerator AnimateMusicCrossfade(EventReference nextTrackEvent, float fadeDuration, bool shouldPauseCurrent)
    {
        float percent = 0;

        // 1. FADE OUT BÀI ĐANG PHÁT
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.getVolume(out float startVolume);
            while (percent < 1)
            {
                percent += Time.deltaTime / fadeDuration;
                currentMusicInstance.setVolume(Mathf.Lerp(startVolume, 0, percent));
                yield return null;
            }

            if (shouldPauseCurrent)
            {
                // TẠM DỪNG: Đóng băng thay vì xóa
                currentMusicInstance.setPaused(true);
                pausedMusicInstance = currentMusicInstance;
            }
            else
            {
                // XÓA HẲN
                currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                currentMusicInstance.release();
            }
        }

        // 2. PHÁT BÀI MỚI
        currentMusicInstance = RuntimeManager.CreateInstance(nextTrackEvent);
        currentMusicInstance.setVolume(0f);
        currentMusicInstance.start();

        // 3. FADE IN BÀI MỚI
        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            currentMusicInstance.setVolume(Mathf.Lerp(0, 1f, percent));
            yield return null;
        }
        currentMusicInstance.setVolume(1f);
    }

    // Coroutine xử lý Resume
    private IEnumerator AnimateResumeCrossfade(float fadeDuration)
    {
        float percent = 0;

        // 1. FADE OUT VÀ TIÊU DIỆT BÀI CAO TRÀO
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.getVolume(out float startVolume);
            while (percent < 1)
            {
                percent += Time.deltaTime / fadeDuration;
                currentMusicInstance.setVolume(Mathf.Lerp(startVolume, 0, percent));
                yield return null;
            }
            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusicInstance.release();
        }

        // 2. ĐÁNH THỨC BÀI MAIN THEME BỊ TẠM DỪNG
        currentMusicInstance = pausedMusicInstance;
        currentMusicInstance.setPaused(false); // Bỏ Pause

        // 3. FADE IN BÀI MAIN THEME
        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            currentMusicInstance.setVolume(Mathf.Lerp(0, 1f, percent));
            yield return null;
        }
        currentMusicInstance.setVolume(1f);

        // Xóa biến tạm
        pausedMusicInstance.clearHandle();
    }

    // HÀM MỚI 3: Chỉ tạm dừng bài hiện tại có Fade Out (Không phát bài mới)
    public void PauseMusicOnly(float fadeDuration = 0.5f)
    {
        if (currentMusicInstance.isValid())
        {
            StartCoroutine(AnimatePauseOnly(fadeDuration));
        }
    }

    private IEnumerator AnimatePauseOnly(float fadeDuration)
    {
        float percent = 0;
        currentMusicInstance.getVolume(out float startVolume);

        // Fade out từ từ xuống 0
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            currentMusicInstance.setVolume(Mathf.Lerp(startVolume, 0, percent));
            yield return null;
        }

        // Đóng băng bài hát
        currentMusicInstance.setPaused(true);
        pausedMusicInstance = currentMusicInstance;
    }

    // HÀM MỚI 4: Tiếp tục phát lại bài nhạc đang bị PauseOnly (Không có bài mới để Fade Out)
    public void ResumeMusicOnly(float fadeDuration = 0.5f)
    {
        if (pausedMusicInstance.isValid())
        {
            StartCoroutine(AnimateResumeOnly(fadeDuration));
        }
        else
        {
            Debug.LogWarning("MusicManager: Không có bản nhạc nào đang bị tạm dừng để Resume!");
        }
    }

    private IEnumerator AnimateResumeOnly(float fadeDuration)
    {
        // 1. Đưa instance bị tạm dừng trở lại thành instance hiện tại
        currentMusicInstance = pausedMusicInstance;

        // 2. Gỡ bỏ trạng thái Pause
        currentMusicInstance.setPaused(false);

        // 3. Fade In âm lượng từ 0 lên 1
        float percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            currentMusicInstance.setVolume(Mathf.Lerp(0, 1f, percent));
            yield return null;
        }
        currentMusicInstance.setVolume(1f);

        // Xóa biến tạm để dọn dẹp bộ nhớ
        pausedMusicInstance.clearHandle();
    }

    private void OnDestroy()
    {
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentMusicInstance.release();
        }
        if (pausedMusicInstance.isValid())
        {
            pausedMusicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            pausedMusicInstance.release();
        }
    }
}