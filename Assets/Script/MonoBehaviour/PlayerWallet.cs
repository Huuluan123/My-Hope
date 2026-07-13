using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Header("Economy Settings")]
    public int currentCoins = 0;

    private void Start()
    {
        // ON GAME START: Load money from the Master Save file
        if (WorldStateManager.Instance != null)
        {
            currentCoins = WorldStateManager.Instance.currentPlayerSave.savedCoins;
        }

        // Notify the UI to update the initial coin amount
        EventBus.Publish("OnMoneyChanged", currentCoins);
    }

    // 1. Function to receive money
    public void AddMoney(int amount)
    {
        currentCoins += amount;
        Debug.Log($"[PlayerWallet] Received {amount} coins. Total: {currentCoins}");

        // Broadcast the event to update UI
        EventBus.Publish("OnMoneyChanged", currentCoins);
    }

    // 2. Function to spend money (Returns true if successful, false if not enough money)
    public bool TrySpendMoney(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            Debug.Log($"[PlayerWallet] Successfully spent {amount} coins. Remaining: {currentCoins}");

            // Broadcast the event to update UI
            EventBus.Publish("OnMoneyChanged", currentCoins);
            return true; // Transaction successful
        }
        else
        {
            Debug.Log($"[PlayerWallet] Transaction failed. Required: {amount} coins, but only have {currentCoins} coins.");

            // Optional: Broadcast an event to shake the UI or play an error sound
            EventBus.Publish("OnNotEnoughMoney", null);
            return false; // Transaction failed
        }
    }
}