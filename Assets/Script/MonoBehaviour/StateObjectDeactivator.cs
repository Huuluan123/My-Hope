using UnityEngine;

public class StateObjectDeactivator : MonoBehaviour
{
    [Header("State save (Optional)")]
    [Tooltip("Nếu điền Key vào đây, khi bị tắt nó sẽ tự động lưu lại vào WorldStateManager để biến mất vĩnh viễn.")]
    public string saveKeyOnDeactivate;
    public bool targetStateValueOnDeactivate = false;

    public void Deactivate()
    {
        // 1. Tắt vật thể (Hình ảnh, Va chạm)
        gameObject.SetActive(false);

        // 2. Tự động báo cáo cho "Bộ Não" để lần sau load game nó không hiện ra nữa
        if (!string.IsNullOrEmpty(saveKeyOnDeactivate) && WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SetState(saveKeyOnDeactivate, targetStateValueOnDeactivate);
        }
    }

    // BONUS: Thêm một hàm tắt từ từ bằng Coroutine nếu bạn không muốn xài Animation
    public void DeactivateWithDelay(float delayTime)
    {
        StartCoroutine(DeactivateRoutine(delayTime));
    }

    private System.Collections.IEnumerator DeactivateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Deactivate();
    }
}