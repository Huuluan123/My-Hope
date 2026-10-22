using UnityEngine;

public class Trigger_WorldStateChange : MonoBehaviour
{
    [Header("Configure Event Flag")]
    [Tooltip("Gõ Key bạn muốn thay đổi. VD: NPC1_Met")]
    public string stateKey;

    [Tooltip("Bạn muốn gán Key này thành True hay False?")]
    public bool targetStateValue = true;

    public void TriggerStateChange()
    {
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(stateKey))
        {
            WorldStateManager.Instance.SetState(stateKey, targetStateValue);

            EventBus.Publish(
                "OnWorldStateChanged",
                new WorldStateChangedEvent(stateKey, targetStateValue)
            );

            Debug.Log(
                "Trigger_WorldStateChange: State changed - "
                + stateKey
                + " set to "
                + targetStateValue
            );
        }

        else Debug.Log("Trigger_WorldStateChange: WorldStateManager instance is null or stateKey is empty.");
    }
}

public class WorldStateChangedEvent
{
    public string Key { get; }
    public bool Value { get; }

    public WorldStateChangedEvent(string key, bool value)
    {
        Key = key;
        Value = value;
    }
}