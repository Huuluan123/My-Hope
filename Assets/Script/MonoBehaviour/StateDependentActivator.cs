using UnityEngine;

public class StateDependentActivator : MonoBehaviour
{
    [Header("State Configuration")]
    [Tooltip("Type the state key (e.g., NPC_Guide_01)")]
    public string stateKey;

    [Tooltip("Tick (True): Object ENABLED when Key is True. Untick (False): Object ENABLED when Key is False.")]
    public bool activeWhenStateIsTrue = true;

    private void OnEnable()
    {
        // 1. Kiểm tra xem bộ não trung tâm có tồn tại không
        if (WorldStateManager.Instance == null) return;

        // 2. Đọc trạng thái từ file JSON/RAM
        bool currentState = WorldStateManager.Instance.CheckState(stateKey);

        // 3. Quyết định: Có nên bật không?
        bool shouldBeActive = (currentState == activeWhenStateIsTrue);

        // 4. LỚP PHÒNG THỦ: Nếu RoomManager vừa ép nó bật, nhưng Key bảo nó phải tắt
        // -> Lập tức tự tắt chính mình đi!
        if (!shouldBeActive)
        {
            gameObject.SetActive(false);
        }
    }
}