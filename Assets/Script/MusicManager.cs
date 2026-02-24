using System.Collections;
using UnityEngine;
using FMODUnity; // Thư viện kết nối Unity
using FMOD.Studio; // Thư viện quản lý các Instance của FMOD

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    // Thay thế MusicLibrary cũ bằng một struct List để map tên với Event FMOD
    [System.Serializable]
    public struct MusicTrack
    {
        public string trackName;          // Ví dụ: "Main Theme"
        public EventReference fmodEvent;  // Kéo thả Event từ FMOD vào đây
    }

    [Header("FMOD Music Settings")]
    [SerializeField] private MusicTrack[] musicTracks;

    // EventInstance thay thế cho AudioSource
    private EventInstance currentMusicInstance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        // 1. Tìm EventReference dựa trên tên track
        EventReference eventToPlay = new EventReference();
        bool found = false;

        foreach (var track in musicTracks)
        {
            if (track.trackName == trackName)
            {
                eventToPlay = track.fmodEvent;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("FMOD MusicManager: Không tìm thấy track nhạc tên -> " + trackName);
            return;
        }

        // 2. Chạy Coroutine đổi nhạc
        StartCoroutine(AnimateMusicCrossfade(eventToPlay, fadeDuration));
    }

    IEnumerator AnimateMusicCrossfade(EventReference nextTrackEvent, float fadeDuration)
    {
        float percent = 0;

        // BƯỚC 1: Fade out (Giảm âm lượng) bài nhạc ĐANG PHÁT (nếu có)
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.getVolume(out float startVolume);

            while (percent < 1)
            {
                percent += Time.deltaTime / fadeDuration;
                // Hạ volume từ từ xuống 0
                currentMusicInstance.setVolume(Mathf.Lerp(startVolume, 0, percent));
                yield return null;
            }

            // Dừng hẳn bài cũ và giải phóng bộ nhớ (Cực kỳ quan trọng trong FMOD)
            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusicInstance.release();
        }

        // BƯỚC 2: Tạo Instance mới cho bài nhạc TIẾP THEO
        currentMusicInstance = RuntimeManager.CreateInstance(nextTrackEvent);

        // Ép volume về 0 trước khi phát để chuẩn bị Fade in
        currentMusicInstance.setVolume(0f);
        currentMusicInstance.start();

        // BƯỚC 3: Fade in (Tăng âm lượng) bài nhạc MỚI
        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            currentMusicInstance.setVolume(Mathf.Lerp(0, 1f, percent));
            yield return null;
        }

        // Đảm bảo âm lượng đạt mức tối đa (1.0f) khi kết thúc vòng lặp
        currentMusicInstance.setVolume(1f);
    }

    // Dọn dẹp bộ nhớ khi tắt game hoặc chuyển scene phá hủy Manager
    private void OnDestroy()
    {
        if (currentMusicInstance.isValid())
        {
            currentMusicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentMusicInstance.release();
        }
    }
}