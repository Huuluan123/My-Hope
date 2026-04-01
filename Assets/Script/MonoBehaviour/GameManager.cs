using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // Thư viện để quản lý thiết bị
using System;

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

    public event Action<GameState> OnGameStateChanged;

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
            InputSystem.onActionChange += OnInputDeviceChanged;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
        InputSystem.onActionChange -= OnInputDeviceChanged;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
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
        OnGameStateChanged?.Invoke(State);
    }

    private void OnInputDeviceChanged(object obj, InputActionChange change)
    {
        // Chỉ quét khi có một nút thực sự được bấm/gạt
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            // CHỈ thay đổi chuột khi game ĐANG TẠM DỪNG (Mở Menu)
            if (State == GameState.Pause)
            {
                if (device is Gamepad) // Nếu phát hiện Tay Cầm
                {
                    Cursor.visible = false; // Tàng hình chuột

                    // Dịch chuyển chuột vào giữa góc dưới cùng bên phải để tránh vướng UI
                    if (Mouse.current != null)
                    {
                        Mouse.current.WarpCursorPosition(new Vector2(Screen.width, 0));
                    }
                }
                else if (device is Pointer || device is Keyboard) // Nếu lắc Chuột hoặc gõ Phím
                {
                    // Di chuyển chuột ra giữa màn hình nếu có input chuột để người chơi dễ tìm
                    if (!Cursor.visible && Mouse.current != null)
                    {
                        Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2f, Screen.height / 2f));
                    }

                    Cursor.visible = true; // Hiện chuột lại bình thường
                }
            }
        }
    }
}