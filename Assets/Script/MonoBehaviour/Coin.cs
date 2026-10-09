using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int moneyAmountToAdd;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.Instance.gameObject.GetComponent<PlayerWallet>().AddMoney(moneyAmountToAdd);
            Destroy(gameObject);
        }
    }
}
