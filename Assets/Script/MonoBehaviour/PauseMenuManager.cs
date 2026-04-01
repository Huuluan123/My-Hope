using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Toolkit Reference")]
    public UIDocument uiDocument;

    private VisualElement pauseOverlay;
    private Button btnResume;
    private Button btnOptions;
    private Button btnExit;

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

        // Mặc định ép ẩn UI khi mới vào game
        HideMenu();
    }

    // 🔴 BẮT BUỘC PHẢI CÓ: Hủy đăng ký khi chuyển Scene hoặc xóa UI để tránh tràn RAM
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (btnResume != null) btnResume.clicked -= OnResumeClicked;
        if (btnOptions != null) btnOptions.clicked -= OpenOptions;
        if (btnExit != null) btnExit.clicked -= SaveAndExitGame;
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
            if (btnResume != null) btnResume.Focus(); // Highlight nút đầu tiên cho tay cầm Console
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