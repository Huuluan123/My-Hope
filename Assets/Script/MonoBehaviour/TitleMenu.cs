using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class TitleMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Scene to load after choosing Play")]
    public string gameSceneName = "0";
    public GameObject firstSelectedButton;

    public Button btnPlay;

    // BIẾN LƯU TRẠNG THÁI CŨ ĐỂ SO SÁNH
    private bool wasUsingPointer = true;

    private void Start()
    {
        // Khi vừa mở game lên, kiểm tra xem người chơi đang dùng gì
        if (GameManager.Instance != null)
        {
            wasUsingPointer = GameManager.Instance.IsUsingPointer;

            // Nếu mở game lên bằng tay cầm/bàn phím -> Focus ngay nút Play
            if (!wasUsingPointer && btnPlay != null)
            {
                btnPlay.Select();
                Debug.Log("Focus on Play button at start because player is using controller/keyboard");
            }
        }

        // Xóa trí nhớ cũ của Event System
        EventSystem.current.SetSelectedGameObject(null);

        // Chỉ tự động chọn nút nếu người chơi đang KHÔNG dùng chuột
        if (GameManager.Instance != null && !GameManager.Instance.IsUsingPointer)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            Debug.Log("[MainMenu] Đã tự động Focus vào nút Play!");
        }
    }

    private void Update()
    {
        // Nếu không có GameManager thì bỏ qua
        if (GameManager.Instance == null) return;

        bool isCurrentlyUsingPointer = GameManager.Instance.IsUsingPointer;

        // NẾU VỪA CHUYỂN TỪ CHUỘT SANG TAY CẦM / BÀN PHÍM
        if (wasUsingPointer == true && isCurrentlyUsingPointer == false)
        {
            if (EventSystem.current != null && btnPlay != null)
            {
                // Kiểm tra xem hiện tại có đang chọn nút nào khác không
                // (Tránh trường hợp người chơi đang trỏ chuột ở nút Exit rồi cầm tay cầm bấm chọn thì bị giật về Play)
                if (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
                {
                    btnPlay.Select(); // Focus vào nút Play
                    Debug.Log("Focus on playbutton because player switched from pointer to controller/keyboard");
                }
            }
        }

        // Cập nhật lại trạng thái để dành cho frame tiếp theo
        wasUsingPointer = isCurrentlyUsingPointer;
    }

    // Hàm gọi khi bấm nút Play
    public void PlayGame()
    {
        Time.timeScale = 1f;

        // Tắt EventSystem ngay khi click để CHỐNG SPAM CLICK tuyệt đối
        if (EventSystem.current != null)
        {
            EventSystem.current.enabled = false;
            Debug.Log("[UI Protection] EventSystem disabled. All UI inputs are now locked.");
        }

        // THỦ THUẬT: Yêu cầu TransitionManager (kẻ sống sót) chạy hộ Coroutine này.
        // Như vậy Coroutine sẽ không bị ngắt giữa chừng khi Scene hiện tại bị hủy!
        TransitionManager.Instance.StartCoroutine(LoadLevelRoutine());
    }

    // Hàm chuyển ngôn ngữ, gọi khi bấm nút Language
    public void ToggleLanguage()
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

    // Hàm gọi khi bấm nút Exit
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
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

        // 3. Tiến hành Load Scene mới (Người chơi sẽ không thấy sự giật lag vì màn hình đang đen)
        SceneManager.LoadScene(gameSceneName);

        // BẮT BUỘC: Chờ 1 frame để Unity khởi tạo xong Scene mới, tránh lỗi NullReference
        yield return null;

        // 4. Kéo màn đen lên để thấy Scene mới
        TransitionManager.Instance.PlayEndTransition();
    }
}