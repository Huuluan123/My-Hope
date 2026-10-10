using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int moneyAmountToAdd;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerWallet>(out PlayerWallet playerWallet))
        {
            playerWallet.AddMoney(moneyAmountToAdd);
            Destroy(gameObject); // Destroy the coin after collection
        }
    }
}
