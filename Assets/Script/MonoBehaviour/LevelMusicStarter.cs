using UnityEngine;

public class LevelMusicStarter : MonoBehaviour
{
    [Header("Settings")]
    public string startingTrack = "Main Theme";
    public float fadeTime = 2.0f; // Fade chậm lúc mới vào game cho có không khí

    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            // Dùng PlayMusic bình thường để khởi tạo nhạc nền
            MusicManager.Instance.PlayMusic(startingTrack, fadeTime);
        }
    }
}