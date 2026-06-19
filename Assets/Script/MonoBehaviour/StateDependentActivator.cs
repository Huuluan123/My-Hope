using UnityEngine;

public class StateDependentActivator : MonoBehaviour
{
    [Header("Cấu hình Trạng thái")]
    [Tooltip("Gõ chính xác Key trong WorldStateManager. VD: NPC_Guide_01")]
    public string stateKey;

    [Tooltip("Tích (True): Object BẬT khi Key True. Bỏ tích (False): Object BẬT khi Key False.")]
    public bool activeWhenStateIsTrue = true;

    private void Start()
    {
        // 1. Kiểm tra an toàn xem có tồn tại bộ não trung tâm không
        if (WorldStateManager.Instance == null) return;

        // 2. Đọc trạng thái từ file WorldStateManager của bạn
        bool currentState = WorldStateManager.Instance.CheckState(stateKey);
        //Debug.Log(gameObject.name + " đọc được trạng thái: " + stateKey + " = " + currentState);
        //Debug.Log("Global State count: " + WorldStateManager.Instance.GetStateCount());

        // 3. Quyết định số phận của Object
        // Nếu Trạng thái hiện tại khớp với kỳ vọng -> Bật. Nếu nghịch nhau -> Tắt.
        bool shouldBeActive = (currentState == activeWhenStateIsTrue);

        gameObject.SetActive(shouldBeActive);
    }
}