using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Toolkit Reference")]
    public UIDocument uiDocument;

    private VisualElement pauseOverlay;
    private Button btnResume;
    private Button btnOptions;
    private Button btnExit;

    private InputAction cancelAction;
    private InputAction uiPauseAction;

    private bool isOptionsMenuOpen = false;

    // Dùng Start thay vì OnEnable để chắc chắn GameManager đã khởi tạo xong
    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        pauseOverlay = root.Q<VisualElement>("pause-overlay");

        btnResume = root.Q<Button>("btn-resume");
        btnOptions = root.Q<Button>("btn-options");
        btnExit = root.Q<Button>("btn-exit");

        // Gắn sự kiện Click cho các nút
        if (btnResume != null) btnResume.clicked += OnResumeClicked;
        if (btnOptions != null) btnOptions.clicked += OpenOptions;
        if (btnExit != null) btnExit.clicked += SaveAndExitGame;

        // ĐĂNG KÝ NGHE LỆNH TỪ GAMEMANAGER
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // 1. LẮP CẢM BIẾN NHẬN DIỆN DI CHUYỂN CHUỘT
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

        cancelAction = InputSystem.actions.FindAction("UI/Cancel");
        if (cancelAction != null)
        {
            cancelAction.performed += OnCancelPerformed;
        }

        uiPauseAction = InputSystem.actions.FindAction("UI/Pause");
        if (uiPauseAction != null)
        {
            uiPauseAction.performed += OnPauseResumePerformed;
        }

        // Mặc định ép ẩn UI khi mới vào game
        HideMenu();
    }

    // BẮT BUỘC PHẢI CÓ: Hủy đăng ký khi chuyển Scene hoặc xóa UI để tránh tràn RAM
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (btnResume != null) btnResume.clicked -= OnResumeClicked;
        if (btnOptions != null) btnOptions.clicked -= OpenOptions;
        if (btnExit != null) btnExit.clicked -= SaveAndExitGame;
        if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;
        if (uiPauseAction != null) uiPauseAction.performed -= OnPauseResumePerformed;

        // 2. GỠ CẢM BIẾN CHUỘT
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }
    }

    // 3. XỬ LÝ SỰ KIỆN CHUỘT DI CHUYỂN (TRÁNH XUNG ĐỘT FOCUS)
    private void OnPointerMove(PointerMoveEvent evt)
    {
        // Nếu KHÔNG xài con trỏ chuột thì phớt lờ mọi sự kiện nhúc nhích chuột giả (do lệnh Warp gây ra)
        if (GameManager.Instance != null && !GameManager.Instance.IsUsingPointer) return;

        // Nếu người chơi THỰC SỰ đang cầm chuột, tiến hành "tẩy não" Focus
        var focusController = uiDocument.rootVisualElement.focusController;

        if (focusController.focusedElement != null)
        {
            focusController.focusedElement.Blur();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // Chỉ xử lý khi Game ĐANG PAUSE (nghĩa là Menu đang được mở)
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause)
        {
            // KIỂM TRA LỚP 1: Có đang mở Menu Con (Options) không?
            if (isOptionsMenuOpen)
            {
                CloseOptions(); // Đóng Options, hiển thị lại Menu chính
            }
            // KIỂM TRA LỚP 2: Nếu đang ở Menu Chính -> Đóng Menu, quay lại Game!
            else
            {
                GameManager.Instance.TogglePause();
            }
        }
    }

    private void OnPauseResumePerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause)
        {
            // Nếu đang mở Menu chính (không phải Options) thì mới Resume
            if (!isOptionsMenuOpen)
            {
                GameManager.Instance.TogglePause();
            }
            else
            {
                CloseOptions(); // Nếu đang ở Options thì lùi về Menu chính
            }
        }
    }

    // Hàm này tự động chạy mỗi khi GameManager gọi lệnh TogglePause()
    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Pause)
        {
            ShowMenu();
        }
        else
        {
            HideMenu();
        }
    }

    // Khi người chơi dùng chuột click vào nút Resume
    private void OnResumeClicked()
    {
        // Yêu cầu GameManager tiếp tục game (nó sẽ tự lo việc ẩn chuột, chạy thời gian)
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void ShowMenu()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.style.display = DisplayStyle.Flex; // Bung UI ra

            // 4. NÂNG CẤP FOCUS: Trì hoãn 1 frame để đảm bảo UI Toolkit đã vẽ xong nút
            if (btnResume != null)
            {
                pauseOverlay.schedule.Execute(() =>
                {
                    // Chỉ tự động Focus vào nút Resume nếu người chơi ĐANG XÀI tay cầm hoặc bàn phím
                    if (GameManager.Instance != null && !GameManager.Instance.IsUsingPointer)
                    {
                        btnResume.Focus();
                    }
                });
            }
        }
    }

    private void HideMenu()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.style.display = DisplayStyle.None; // Xóa UI đi
        }
    }

    private void OpenOptions()
    {
        Debug.Log("Opening options Menu");
        isOptionsMenuOpen = true;
    }

    private void CloseOptions()
    {
        Debug.Log("Closing options Menu, back to Pause Menu...");
        isOptionsMenuOpen = false;

        // TODO sau này: Ẩn optionsOverlay, Hiện lại pauseOverlay
        // optionsOverlay.style.display = DisplayStyle.None;
        // pauseOverlay.style.display = DisplayStyle.Flex;

        // Nhớ Focus lại vào nút Options để người chơi biết họ vừa từ đâu chui ra
        if (btnOptions != null && !GameManager.Instance.IsUsingPointer)
        {
            btnOptions.Focus();
        }
    }

    private void SaveAndExitGame()
    {
        Debug.Log("Saving and quit...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}