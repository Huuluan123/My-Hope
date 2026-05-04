using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // Thư viện để quản lý thiết bị
using UnityEngine.SceneManagement;
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

    public bool IsUsingPointer { get; private set; }
    private float lastMouseWarpTime;

    // 🌟 THÊM MỚI: Các biến để chứa Bản đồ hành động
    private InputActionMap playerMap;
    private InputActionMap uiMap;

    private bool isInMainMenu;

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

        ApplyDeviceResolutionAndFPS();
        State = GameState.Running;

        // Khởi tạo Input
        pauseAction = InputSystem.actions.FindAction("Pause");

        // THÊM MỚI: Tìm và lưu lại 2 Action Map
        playerMap = InputSystem.actions.FindActionMap("Player");
        uiMap = InputSystem.actions.FindActionMap("UI");

        // Đảm bảo trạng thái mặc định khi mới vào game: Bật Player, Tắt UI
        playerMap?.Enable();
        uiMap?.Disable();
    }

    //private void Start()
    //{
    //    Cursor.visible = false;
    //    Cursor.lockState = CursorLockMode.Locked;
    //}

#if UNITY_EDITOR
    //private void Update()
    //{
    //    if (State != GameState.Pause) Time.timeScale = gameSpeed;
    //}
#endif

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
        InputSystem.onActionChange += OnInputDeviceChanged;
        InputSystem.onDeviceChange += OnDeviceChange;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
        InputSystem.onActionChange -= OnInputDeviceChanged;
        InputSystem.onDeviceChange -= OnDeviceChange;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Phân xử logic chuột mỗi khi chuyển Scene
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Giả định Title Screen của bạn luôn nằm ở vị trí số 0 trong File -> Build Settings
        isInMainMenu = (scene.buildIndex == 0);

        if (isInMainMenu)
        {
            // Ở Title Screen: Ẩn chuột, nhưng KHÔNG khóa cứng (None) để nó không bị giật về giữa màn hình
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            IsUsingPointer = true;
        }
        else
        {
            // Ở trong Game: Khóa cứng và giấu chuột đi
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
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
            Time.timeScale = 0f;

            // LỚP PHÒNG THỦ: Ngắt điện Nhân vật, Cấp điện cho UI
            playerMap?.Disable();
            uiMap?.Enable();

            // HIỆN LẠI CHUỘT ĐỂ BẤM MENU
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (State == GameState.Pause)
        {
            Debug.Log("Game resumed!");
            State = GameState.Running;
            Time.timeScale = 1f;

            // LỚP PHÒNG THỦ: Trả lại quyền điều khiển cho Nhân vật, Tắt UI
            playerMap?.Enable();
            uiMap?.Disable();

            // GIẤU CHUỘT ĐI KHI CHƠI TIẾP
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Báo cho PauseMenuManager biết để bung/ẩn giao diện
        OnGameStateChanged?.Invoke(State);
    }

    private void OnInputDeviceChanged(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            //Debug.Log(device.name);

            if (State == GameState.Pause)
            {
                if (device is Gamepad || device is Keyboard)
                {
                    // CHỈ thực hiện khi đang từ Chuột chuyển sang Tay cầm (Chốt khóa Transition)
                    if (IsUsingPointer)
                    {
                        IsUsingPointer = false;
                        Cursor.visible = false;

                        if (Mouse.current != null)
                        {
                            // Đánh dấu mốc thời gian vừa ném chuột đi (Dùng unscaledTime vì timeScale đang = 0)
                            lastMouseWarpTime = Time.unscaledTime;
                            Mouse.current.WarpCursorPosition(new Vector2(Screen.width, 0));
                        }
                    }
                }

                else if (device is Pointer)
                {
                    // BỎ QUA tín hiệu chuột nếu nó vừa bị Code dịch chuyển trong vòng 0.1 giây trước đó (Chống Warp Echo)
                    if (Time.unscaledTime - lastMouseWarpTime < 0.1f) return;

                    // CHỈ thực hiện khi đang từ Tay cầm chuyển sang Chuột
                    if (!IsUsingPointer)
                    {
                        if (Mouse.current != null)
                        {
                            Debug.Log("Switching to mouse input, warping cursor to center.");
                            Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2f, Screen.height / 2f));
                        }

                        IsUsingPointer = true;
                        Cursor.visible = true;
                    }
                }
            }
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // Kiểm tra nếu thiết bị bị gỡ bỏ (Removed) hoặc mất tín hiệu (Disconnected - thường do hết pin)
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
        {
            // Chỉ Pause nếu Game đang chạy 
            // và thiết bị bị mất là Tay cầm hoặc Bàn phím
            if (State == GameState.Running && (device is Gamepad || device is Keyboard))
            {
                Debug.LogWarning($"Detected disconnection of: {device.name}. Automatically pause game!");
                TogglePause();
            }
        }
    }

    private void ApplyDeviceResolutionAndFPS()
    {
        // 1. Lấy thông tin độ phân giải và tần số quét gốc của màn hình hiện tại
        Resolution nativeRes = Screen.currentResolution;
        int width = nativeRes.width;
        int height = nativeRes.height;

        // Xử lý lấy tần số quét (Hỗ trợ cả Unity bản mới và cũ)
#if UNITY_2022_2_OR_NEWER
        int refreshRate = Mathf.RoundToInt((float)nativeRes.refreshRateRatio.value);
#else
        int refreshRate = nativeRes.refreshRate;
#endif

        Debug.Log($"[GameManager] Phát hiện màn hình: {width}x{height} @ {refreshRate}Hz");

        // 2. Gắn độ phân giải Fullscreen (Không viền) với tần số quét tương ứng
#if UNITY_2022_2_OR_NEWER
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow, nativeRes.refreshRateRatio);
#else
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow, refreshRate);
#endif

        // 3. QUAN TRỌNG: Tắt VSync của Unity để targetFrameRate có thể hoạt động chính xác
        // Nếu vSyncCount = 1, Unity sẽ bỏ qua targetFrameRate và ép dính theo màn hình. 
        // Tuy nhiên, việc ép bằng code (vSync = 0 + targetFrameRate) sẽ mang lại độ trễ Input (Input Delay) thấp hơn cho game Platformer!
        QualitySettings.vSyncCount = 0;

        // 4. Khóa FPS bằng đúng với tần số quét của màn hình
        Application.targetFrameRate = refreshRate;
    }
}