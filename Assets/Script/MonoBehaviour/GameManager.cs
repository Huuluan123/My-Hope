using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Running,
    Pause
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState State { get; private set; }

    public float gameSpeed = 1f;

    private InputAction pauseAction;

    void Awake()
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

        Application.targetFrameRate = 165;
        State = GameState.Running;
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (State != GameState.Pause) Time.timeScale = gameSpeed;
    }
#endif

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (State == GameState.Running)
        {
            Debug.Log("Game Paused!");
            State = GameState.Pause;
            Time.timeScale = 0f; // Chỉ dừng thời gian 1 LẦN DUY NHẤT khi bấm Pause

            // HIỆN LẠI CHUỘT ĐỂ BẤM MENU
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; // Mở khóa cho chuột di chuyển tự do
        }
        else if (State == GameState.Pause)
        {
            Debug.Log("Game resumed!");
            State = GameState.Running;
            Time.timeScale = 1f; // Chỉ trả lại thời gian 1 LẦN DUY NHẤT khi bỏ Pause

            // GIẤU CHUỘT ĐI KHI CHƠI TIẾP
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}