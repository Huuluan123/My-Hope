using UnityEngine;
using UnityEngine.Playables;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }
    
    public PlayableDirector director; // Kéo thả Timeline của bạn vào đây

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private void OnEnable()
    {
        // Đăng ký sự kiện: Khi Timeline bắt đầu chạy -> Gọi hàm khóa Player
        director.played += LockPlayer;

        // Đăng ký sự kiện: Khi Timeline dừng lại -> Gọi hàm mở khóa Player
        director.stopped += UnlockPlayer;
    }

    private void OnDisable()
    {
        director.played -= LockPlayer;
        director.stopped -= UnlockPlayer;
    }

    private void LockPlayer(PlayableDirector obj)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetState(PlayerState.ScriptedMove);
            // Xóa lực quán tính (dùng public method hoặc property nếu cần)
        }
    }

    private void UnlockPlayer(PlayableDirector obj)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetState(PlayerState.Idle);
        }
    }
}