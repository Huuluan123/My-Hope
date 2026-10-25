using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenuManager : MonoBehaviour, IActivatableUI
{
    [Header("UI Toolkit Reference")]
    public UIDocument uiDocument;
    public UILayer GetUILayer() => UILayer.Menu; // Tự động nhận khoảng 200+

    private VisualElement pauseOverlay;
    private Button btnResume;
    private Button btnOptions;
    private Button btnLanguage;
    private Button btnExit;

    private InputAction uiPauseAction;

    private bool isOptionsMenuOpen = false;

    #region IActivatableUI Implementation

    public UIInputConfig GetInputConfig()
    {
        return new UIInputConfig
        {
            inputMode = UIInputMode.UIOnly,
            pauseTime = true,
            allowPauseToggle = true
        };
    }

    public void OnActivate()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.style.display = DisplayStyle.Flex;
            pauseOverlay.SetEnabled(true);
        }
    }

    public void OnPassivate()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.SetEnabled(false);
        }
    }

    public void OnDeactivate()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.style.display = DisplayStyle.None;
        }
        isOptionsMenuOpen = false;
    }

    public void FocusDefaultElement()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.schedule.Execute(() =>
            {
                var target = isOptionsMenuOpen ? btnOptions : btnResume;
                target?.Focus();
            });
        }
    }

    public void ClearFocus()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            var focusController = uiDocument.rootVisualElement.focusController;
            if (focusController != null && focusController.focusedElement != null)
            {
                focusController.focusedElement.Blur();
            }
        }
    }

    public void SetSortOrder(int order)
    {
        if (uiDocument != null)
        {
            uiDocument.sortingOrder = order;
        }
    }

    public bool OnBackAction()
    {
        if (isOptionsMenuOpen)
        {
            CloseOptions();
            return true;
        }
        else
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause)
            {
                GameManager.Instance.TogglePause();
                return true;
            }
            return false;
        }
    }

    #endregion

    // Dùng Start thay vì OnEnable để chắc chắn GameManager đã khởi tạo xong
    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        pauseOverlay = root.Q<VisualElement>("pause-overlay");

        btnResume = root.Q<Button>("btn-resume");
        btnOptions = root.Q<Button>("btn-options");
        btnLanguage = root.Q<Button>("btn-language");
        btnExit = root.Q<Button>("btn-exit");

        // Gắn sự kiện Click cho các nút
        if (btnResume != null) btnResume.clicked += OnResumeClicked;
        if (btnOptions != null) btnOptions.clicked += OpenOptions;
        if (btnLanguage != null) btnLanguage.clicked += OnLanguageButtonClicked;
        if (btnExit != null) btnExit.clicked += SaveAndExitGame;

        // ĐĂNG KÝ NGHE LỆNH TỪ GAMEMANAGER
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // 1. LẮP CẢM BIẾN NHẬN DIỆN DI CHUYỂN CHUỘT
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

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
        if (UIStackManager.Instance != null)
        {
            UIStackManager.Instance.RemoveUI(this);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (btnResume != null) btnResume.clicked -= OnResumeClicked;
        if (btnOptions != null) btnOptions.clicked -= OpenOptions;
        if (btnLanguage != null) btnLanguage.clicked -= OnLanguageButtonClicked;
        if (btnExit != null) btnExit.clicked -= SaveAndExitGame;
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

        ClearFocus();
    }

    private void OnPauseResumePerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton() == false) return;

        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause)
        {
            // Nếu đang mở Menu chính (không phải Options) thì mới Resume
            if (!isOptionsMenuOpen)
            {
                GameManager.Instance.TogglePause();
                Debug.Log("Pause/Resume performed: Resuming game");
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
            if (UIStackManager.Instance != null)
            {
                UIStackManager.Instance.PushUI(this);
            }
            else
            {
                ShowMenu();
            }
        }
        else
        {
            if (UIStackManager.Instance != null)
            {
                UIStackManager.Instance.RemoveUI(this);
            }
            else
            {
                HideMenu();
            }
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
        OnActivate();
        if (GameManager.Instance == null || !GameManager.Instance.IsUsingPointer)
        {
            FocusDefaultElement();
        }
    }

    private void HideMenu()
    {
        OnDeactivate();
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
        if (btnOptions != null && GameManager.Instance != null && !GameManager.Instance.IsUsingPointer)
        {
            btnOptions.Focus();
        }
    }

    // Hàm chuyển ngôn ngữ, gọi khi bấm nút Language
    public void OnLanguageButtonClicked()
    {
        // Đảm bảo hệ thống Localization đã khởi tạo xong để tránh lỗi NullReference
        if (!LocalizationSettings.InitializationOperation.IsDone) return;

        // Lấy danh sách toàn bộ ngôn ngữ bạn đã thêm vào Project (Tiếng Anh, Tiếng Việt...)
        var locales = LocalizationSettings.AvailableLocales.Locales;

        // Nếu không có bảng dịch nào hoặc chỉ có 1 ngôn ngữ thì không làm gì cả
        if (locales == null || locales.Count <= 1) return;

        // Tìm vị trí (Index) của ngôn ngữ đang dùng hiện tại
        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);

        // Toán học xoay vòng: Tăng index lên 1, nếu chạm giới hạn thì tự quay về 0
        int nextIndex = (currentIndex + 1) % locales.Count;

        // Ra lệnh cho Unity đổi ngôn ngữ!
        LocalizationSettings.SelectedLocale = locales[nextIndex];

        Debug.Log($"[Localization] Language switched to: {LocalizationSettings.SelectedLocale.Identifier.Code}");
    }

    private void SaveAndExitGame()
    {
        // 1. Lưu toàn bộ (Cốt truyện + Người chơi)
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SaveGame();
        }

        // 2. Tắt trạng thái Pause (Nếu bạn đang dùng Time.timeScale = 0 thì nhớ trả về 1)
        //Time.timeScale = 1f;

        // 3. Thoát ra Main Menu
        //TransitionManager.Instance.StartCoroutine(LoadLevelRoutine());
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadLevelRoutine()
    {
        // 1. Kéo màn đen xuống
        TransitionManager.Instance.PlayStartTransition();

        // 2. CHỜ màn đen kéo xong (Lúc này màn hình đen thui)
        yield return new WaitForSecondsRealtime(TransitionManager.Instance.transitionTime);

        // THANH TRỪNG PLAYER
        if (PlayerController.Instance != null)
        {
            Destroy(PlayerController.Instance.gameObject);
        }

        if (MusicManager.Instance != null)
        {
            Destroy(MusicManager.Instance.gameObject);
        }

        if (WorldStateManager.Instance != null)
        {
            Destroy(WorldStateManager.Instance.gameObject);
        }

        // 3. Tiến hành Load Scene mới (Người chơi sẽ không thấy sự giật lag vì màn hình đang đen)
        SceneManager.LoadScene("Title Screen");

        // BẮT BUỘC: Chờ 1 frame để Unity khởi tạo xong Scene mới, tránh lỗi NullReference
        yield return null;

        // 4. Kéo màn đen lên để thấy Scene mới
        TransitionManager.Instance.PlayEndTransition();
    }
}
