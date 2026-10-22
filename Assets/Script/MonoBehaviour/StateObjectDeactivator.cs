using UnityEngine;

public class StateObjectDeactivator : MonoBehaviour
{
    [Header("State condition")]
    public string watchStateKey;
    public bool targetStateValueOnDeactivate = false;

    [Header("State save (Optional)")]
    [Tooltip("Nếu điền Key vào đây, khi bị tắt nó sẽ tự động lưu lại vào WorldStateManager.")]
    public string saveKeyOnDeactivate;

    private void OnEnable()
    {
        EventBus.Subscribe("OnWorldStateChanged", CheckForDeactivate);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe("OnWorldStateChanged", CheckForDeactivate);
    }

    public void CheckForDeactivate(object obj)
    {
        if (obj is not WorldStateChangedEvent stateEvent)
        {
            return;
        }

        if (stateEvent.Key != watchStateKey)
        {
            return;
        }

        if (stateEvent.Value == targetStateValueOnDeactivate)
        {
            Deactivate();
        }
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(saveKeyOnDeactivate)
            && WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SetState(
                saveKeyOnDeactivate,
                targetStateValueOnDeactivate
            );
        }
    }

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