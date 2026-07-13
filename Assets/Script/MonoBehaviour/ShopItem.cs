using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [Header("Item Config")]
    public int price = 50;
    public string itemName = "Item";

    // Called when the player interacts with this item
    public void OnPlayerInteract(GameObject player)
    {
        // 1. Try to find the PlayerWallet component on the interacting player
        PlayerWallet wallet = player.GetComponent<PlayerWallet>();

        if (wallet != null)
        {
            // 2. Request to spend money
            if (wallet.TrySpendMoney(price))
            {
                // TRANSACTION SUCCESSFUL: Give the item to the player
                Debug.Log($"[ShopItem] Successfully sold {itemName} to the player!");
                // TODO: Add logic to give the item (e.g., InventoryManager.AddItem)
            }
            else
            {
                // TRANSACTION FAILED: Player doesn't have enough money
                Debug.Log($"[ShopItem] Player tried to buy {itemName} but didn't have enough coins!");
            }
        }
        else
        {
            Debug.LogWarning("[ShopItem] Interacting object does not have a PlayerWallet component!");
        }
    }
}