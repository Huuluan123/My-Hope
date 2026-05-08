using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // Thư viện để quản lý thiết bị
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;

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

    private float lastToggleTime = 0f;

    public event Action<GameState> OnGameStateChanged;

    private InputAction pauseAction;

    public bool IsUsingPointer { get; private set; }

#if UNITY_EDITOR
    public bool isUsingPointer;
#endif

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
    private void Update()
    {
        //if (State != GameState.Pause) Time.timeScale = gameSpeed;
        isUsingPointer = IsUsingPointer; // Cập nhật giá trị từ Inspector mỗi frame (Chỉ dùng trong Editor)
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
            IsUsingPointer = false; // Mặc định tắt chuột trong gameplay, chỉ bật khi phát hiện thiết bị Pointer
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        // Thêm lớp phòng thủ: Chỉ nhận tín hiệu khi phím bắt đầu được ấn XUỐNG, 
        // bỏ qua tín hiệu khi phím được thả LÊN hoặc bị nhiễu do map switch.
        if (context.ReadValueAsButton() == false) return;
        TogglePause();
        Debug.Log("Toogle Pause");
    }

    public void TogglePause()
    {
        // 🌟 TẤM KHIÊN BẤT TỬ: Chặn mọi sự kiện Pause/Resume bị gọi liên tiếp nhau
        // Sử dụng unscaledTime vì Time.time sẽ bị kẹt lại khi timeScale = 0
        if (Time.unscaledTime - lastToggleTime < 0.2f)
        {
            return; // Đuổi về ngay lập tức, không cho chạy code bên dưới
        }

        lastToggleTime = Time.unscaledTime; // Cập nhật lại mốc thời gian

        if (State == GameState.Running)
        {
            // Pause game
            Debug.Log("Game Paused!");
            State = GameState.Pause;
            Time.timeScale = 0f;

            playerMap?.Disable();
            uiMap?.Enable(); // Không cần dùng IEnumerator Delay nữa, cứ Enable thẳng!

            // CẬP NHẬT: Phân xử hiện thị chuột dựa vào thiết bị đang dùng
            if (IsUsingPointer)
            {
                // Nếu đang dùng chuột: Bật chuột và mở khóa
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // Nếu đang dùng tay cầm/bàn phím: Giấu chuột đi
                Cursor.visible = false;

                // BẮT BUỘC dùng None (Không dùng Locked vì Locked sẽ ép chuột ra giữa màn hình)
                Cursor.lockState = CursorLockMode.None;

                // Ném chuột ra góc dưới cùng bên phải màn hình (ẩn đi)
                if (Mouse.current != null)
                {
                    lastMouseWarpTime = Time.unscaledTime; // Chống dội ngược tín hiệu Warp
                    Mouse.current.WarpCursorPosition(new Vector2(Screen.width, 0));
                }
            }
        }

        // Resume game
        else if (State == GameState.Pause)
        {
            Debug.Log("Game resumed!");
            State = GameState.Running;
            Time.timeScale = 1f;

            uiMap?.Disable();
            playerMap?.Enable(); // Không cần Delay nữa

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            IsUsingPointer = false; // Mặc định tắt Pointer khi đang trong game
        }

        OnGameStateChanged?.Invoke(State);
    }

    private void OnInputDeviceChanged(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            // 1. KHI ĐANG MỞ MENU (Pause Menu HOẶC Title Menu)
            // Lúc này chúng ta cần xử lý cả việc Ẩn/Hiện con trỏ chuột
            if (State == GameState.Pause || isInMainMenu)
            {
                if (device is Gamepad || device is Keyboard)
                {
                    // CHỈ thực hiện khi đang từ Chuột chuyển sang Tay cầm (Chốt khóa Transition)
                    if (IsUsingPointer)
                    {
                        IsUsingPointer = false;
                        Cursor.visible = false; // Giấu chuột đi

                        if (Mouse.current != null)
                        {
                            // Đánh dấu mốc thời gian vừa ném chuột đi
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
                        Cursor.visible = true; // Hiện chuột lại
                    }
                }
            }

            // 2. KHI ĐANG TRONG GAMEPLAY (Đang chơi bình thường)
            // Chỉ cập nhật biến IsUsingPointer ngầm để biết đang dùng thiết bị gì, KHÔNG đụng đến Cursor (vì Cursor luôn bị khóa khi chơi)
            else if (State == GameState.Running && !isInMainMenu)
            {
                if (device is Gamepad || device is Keyboard)
                {
                    IsUsingPointer = false;
                }
                else if (device is Pointer)
                {
                    IsUsingPointer = true;
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
        QualitySettings.vSyncCount = 1;

        // 4. Khóa FPS bằng đúng với tần số quét của màn hình
        //Application.targetFrameRate = refreshRate;
    }
}