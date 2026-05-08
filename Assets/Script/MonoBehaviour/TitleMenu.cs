using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Scene to load after choosing Play")]
    public string gameSceneName = "0";

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

        // THỦ THUẬT: Yêu cầu TransitionManager (kẻ sống sót) chạy hộ Coroutine này.
        // Như vậy Coroutine sẽ không bị ngắt giữa chừng khi Scene hiện tại bị hủy!
        TransitionManager.Instance.StartCoroutine(LoadLevelRoutine());
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