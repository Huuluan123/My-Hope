using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Scene to load after choosing Play")]
    public string gameSceneName = "0";

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