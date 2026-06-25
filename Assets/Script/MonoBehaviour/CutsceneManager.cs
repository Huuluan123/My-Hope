using UnityEngine;
using UnityEngine.Playables;

// Bắt buộc Object gắn script này phải có PlayableDirector
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneManager : MonoBehaviour
{
    public CutsceneManager Instance { get; private set; }

    private PlayableDirector director;

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

        // Tự động tìm Timeline trên cùng GameObject, không cần kéo thả!
        director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện tự động
        director.played += LockPlayer;
        director.stopped += UnlockPlayer;
    }

    private void OnDisable()
    {
        director.played -= LockPlayer;
        director.stopped -= UnlockPlayer;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LockPlayer(PlayableDirector obj)
    {
        if (PlayerController.Instance != null)
        {
            // Gọi hàm OnTimelineStart đã có trong CharacterController
            PlayerController.Instance.OnTimelineStart();
        }
    }

    private void UnlockPlayer(PlayableDirector obj)
    {
        if (PlayerController.Instance != null
            && PlayerController.Instance.GetCurrentState() == PlayerState.ScriptedMove)
        {
            // Chốt chặn an toàn: Nếu nhân vật đang bị chết (ví dụ rớt xuống vực trong cutscene),
            // thì không được phép ép nó sống dậy (Idle)
            // LƯU Ý: Đổi currentState trong CharacterController thành public để dùng dòng lệnh dưới, 
            // HOẶC nếu không muốn public, bạn có thể xóa dòng if này đi.
            PlayerController.Instance.OnTimelineEnd();
        }
    }
}