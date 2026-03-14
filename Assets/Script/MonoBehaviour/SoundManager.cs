using UnityEngine;
using FMODUnity;

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

    // ---------------------------------------------------
    // GỘP CHUNG THÀNH PlaySound BẰNG METHOD OVERLOADING
    // ---------------------------------------------------

    // Dùng cho âm thanh 2D (UI, Nhạc báo hiệu, Tiếng lồng tiếng...)
    public void PlaySound(string soundName)
    {
        EventReference evt = GetEventReference(soundName);
        if (!evt.IsNull)
        {
            RuntimeManager.PlayOneShot(evt);
        }
    }

    // Dùng cho âm thanh 3D (Tiếng bước chân, tiếng đập phá, tiếng bắn tay...)
    public void PlaySound(string soundName, Vector3 position)
    {
        EventReference evt = GetEventReference(soundName);
        if (!evt.IsNull)
        {
            RuntimeManager.PlayOneShot(evt, position);
        }
    }
}