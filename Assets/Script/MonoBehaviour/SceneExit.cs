using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneExit : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Tên Scene muốn đi tới (Ví dụ: Scene_2)")]
    public string targetSceneName;

    [Tooltip("Mã cửa bên kia (Ví dụ: Door_A)")]
    public string targetEntranceID;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.gameObject == PlayerController.Instance.gameObject)
        {
            // THAY ĐỔI 1: Yêu cầu TransitionManager (kẻ sống sót) chạy hộ Coroutine này.
            // Nhờ vậy, khi Scene 1 bị hủy, Coroutine vẫn sẽ tiếp tục chạy tiếp trên TransitionManager!
            TransitionManager.Instance.StartCoroutine(TransitionToSceneRoutine());
        }
    }

    private IEnumerator TransitionToSceneRoutine()
    {
        // THAY ĐỔI 2: CHỐNG MẤT DỮ LIỆU KHI SCRIPT BỊ HỦY (QUAN TRỌNG)
        // Lưu thông tin vào biến cục bộ ngay lập tức. Khi Scene cũ bị giải phóng, 
        // các biến thuộc tính Class (this.targetSceneName) sẽ bị lỗi Null.
        // Biến cục bộ sẽ sống sót an toàn trong bộ nhớ máy trạng thái của Coroutine.
        string sceneToLoad = targetSceneName;
        string entranceToFind = targetEntranceID;

        // 1. Kéo màn đen xuống ngay lập tức
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.PlayStartTransition();
            yield return new WaitForSeconds(TransitionManager.Instance.transitionTime); // Chờ màn hình đen kịt
        }

        // 2. Khóa Input của nhân vật để đề phòng trong lúc load
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetState(PlayerState.ScriptedMove);
        }

        // 3. Tiến hành Load Scene mới (Dùng biến cục bộ sceneToLoad)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // BÂY GIỜ BREAKPOINT CỦA BẠN SẼ DỪNG ĐƯỢC Ở ĐÂY CHẮC CHẮN 100%!
        Debug.Log("Done Loading! Script cũ đã chết nhưng Coroutine vẫn sống nhờ TransitionManager.");

        // 4. Lùng sục Cánh cửa ở Scene mới (Dùng biến cục bộ entranceToFind)
        SceneEntrance[] allEntrances = FindObjectsByType<SceneEntrance>(FindObjectsSortMode.None);
        SceneEntrance targetDoor = null;

        foreach (SceneEntrance entrance in allEntrances)
        {
            if (entrance.entranceID == entranceToFind)
            {
                targetDoor = entrance;
                break;
            }
        }

        if (targetDoor != null)
        {
            // Dịch chuyển TỨC THỜI nhân vật đến điểm neo
            PlayerController.Instance.transform.position = targetDoor.transform.position;

            // Kéo màn đen lên
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.PlayEndTransition();

            // Kích hoạt hiệu ứng đi bộ thoát khỏi cửa!
            //PlayerController.Instance.ForceWalkIntoScene(targetDoor.pushVelocity, targetDoor.autoWalkDuration);
            PlayerController.Instance.SetState(PlayerState.Idle); // Cho phép người chơi tự do điều khiển sau khi dịch chuyển xong
        }
        else
        {
            Debug.LogError($"[Scene Transition] Không tìm thấy Entrance nào mang mã: {entranceToFind} ở Scene {sceneToLoad}!");
            // Failsafe phòng hờ: Nếu lỗi không thấy cửa, nhả Player ra để người chơi không bị kẹt cứng trong màn hình đen
            PlayerController.Instance.SetState(PlayerState.Idle);
        }
    }
}