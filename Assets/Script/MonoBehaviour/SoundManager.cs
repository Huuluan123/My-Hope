using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [System.Serializable]
    public struct SoundTrack
    {
        public string soundName;
        public EventReference fmodEvent;
    }

    [Header("FMOD SFX Settings")]
    [SerializeField] private SoundTrack[] sfxTracks;

    // LỚP BẢO VỆ: Từ điển lưu trữ các âm thanh ĐANG PHÁT
    private Dictionary<string, FMOD.Studio.EventInstance> playingInstances = new Dictionary<string, FMOD.Studio.EventInstance>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private EventReference GetEventReference(string soundName)
    {
        foreach (var track in sfxTracks)
        {
            if (track.soundName == soundName)
            {
                return track.fmodEvent;
            }
        }

        Debug.LogWarning("FMOD SoundManager: Không tìm thấy SFX tên -> " + soundName);
        return new EventReference();
    }

    // ===================================================
    // HỆ THỐNG KIỂM TRA TRẠNG THÁI (ANTI-MACHINEGUN)
    // ===================================================
    private bool IsSoundPlaying(string soundName)
    {
        // Nếu âm thanh này có trong danh sách đang theo dõi
        if (playingInstances.TryGetValue(soundName, out FMOD.Studio.EventInstance instance))
        {
            // Kiểm tra xem Instance này còn hợp lệ không (FMOD tự hủy nó khi phát xong nếu có lệnh release)
            if (instance.isValid())
            {
                FMOD.Studio.PLAYBACK_STATE state;
                instance.getPlaybackState(out state);

                // Nếu đang phát hoặc đang chuẩn bị phát -> Trả về true (Bị chặn)
                return state == FMOD.Studio.PLAYBACK_STATE.PLAYING || state == FMOD.Studio.PLAYBACK_STATE.STARTING;
            }
        }
        return false; // Không có hoặc đã phát xong -> Trả về false (Được phép phát)
    }

    // ---------------------------------------------------
    // GỘP CHUNG THÀNH PlaySound BẰNG METHOD OVERLOADING
    // ---------------------------------------------------

    // Dùng cho âm thanh 2D
    public void PlaySound(string soundName)
    {
        // 1. CHỐNG SPAM: Nếu đang phát rồi thì hủy lệnh!
        if (IsSoundPlaying(soundName)) return;

        EventReference evt = GetEventReference(soundName);
        if (!evt.IsNull)
        {
            // 2. TẠO INSTANCE THỦ CÔNG THAY VÌ PLAYONESHOT
            FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(evt);
            instance.start();
            instance.release(); // Quan trọng: Báo cho FMOD tự động dọn dẹp bộ nhớ sau khi phát xong

            // 3. LƯU VÀO TỪ ĐIỂN ĐỂ THEO DÕI
            playingInstances[soundName] = instance;
        }
    }

    // Dùng cho âm thanh 3D
    public void PlaySound(string soundName, Vector3 position)
    {
        // 1. CHỐNG SPAM
        if (IsSoundPlaying(soundName)) return;

        EventReference evt = GetEventReference(soundName);
        if (!evt.IsNull)
        {
            FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(evt);

            // Gắn tọa độ 3D vào âm thanh
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

            instance.start();
            instance.release();

            playingInstances[soundName] = instance;
        }
    }
}