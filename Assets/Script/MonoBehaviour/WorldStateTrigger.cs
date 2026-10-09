using UnityEngine;

public class WorldStateTrigger : MonoBehaviour
{
    [Header("Configure Event Flag")]
    [Tooltip("Gõ Key bạn muốn thay đổi. VD: NPC1_Met")]
    public string stateKey;

    [Tooltip("Bạn muốn gán Key này thành True hay False?")]
    public bool targetStateValue = true;

    // Hàm KHÔNG CÓ THAM SỐ này mới là thứ UnityEvent rất thích!
    public void TriggerStateChange()
    {
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(stateKey))
        {
            WorldStateManager.Instance.SetState(stateKey, targetStateValue);
            Debug.Log("WorldStateTrigger: State changed - " + stateKey + " set to " + targetStateValue);
        }
    }
}