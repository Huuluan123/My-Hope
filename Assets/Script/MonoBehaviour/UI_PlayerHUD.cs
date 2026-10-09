using TMPro;
using UnityEngine;

public class UI_PlayerHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text element for remaining grapples")]
    public TextMeshProUGUI grappleCounterText;

    [Tooltip("Text element for current money/coins")]
    public TextMeshProUGUI moneyText;

    // Tracking variables for optimization (Dirty Flag Pattern)
    // -1 ensures the UI updates at least once when the game starts
    private int lastRemainingGrapples = -1;
    private int lastMoneyCount = -1;

    // Cached reference to avoid GetComponent every frame
    private PlayerWallet playerWallet;

    private void Start()
    {
        // Try to find the wallet on startup if PlayerController exists
        if (PlayerController.Instance != null)
        {
            playerWallet = PlayerController.Instance.GetComponent<PlayerWallet>();
        }
    }

    private void Update()
    {
        // Safety check: Do nothing if the player doesn't exist yet
        if (PlayerController.Instance == null) return;

        // --- 1. GRAPPLE TRACKING ---
        int remainingGrapples = PlayerController.Instance.maxGrappleCount - PlayerController.Instance.currentGrappleCount;

        // ONLY update UI if the value has actually changed
        if (remainingGrapples != lastRemainingGrapples)
        {
            UpdateGrappleUI(remainingGrapples);
            lastRemainingGrapples = remainingGrapples; // Update memory
        }

        // --- 2. MONEY TRACKING ---
        // Lazy initialization: In case wallet was added late or player respawned
        if (playerWallet == null)
        {
            playerWallet = PlayerController.Instance.GetComponent<PlayerWallet>();
        }

        if (playerWallet != null)
        {
            int currentMoney = playerWallet.currentCoins;

            // ONLY update UI if the money amount has changed
            if (currentMoney != lastMoneyCount)
            {
                UpdateMoneyUI(currentMoney);
                lastMoneyCount = currentMoney; // Update memory
            }
        }
    }

    // Encapsulated UI update methods
    private void UpdateGrappleUI(int amount)
    {
        if (grappleCounterText != null)
        {
            grappleCounterText.text = "X " + amount.ToString();
        }
        else
        {
            Debug.LogWarning("[UI_PlayerHUD] Grapple Counter Text is missing! Please assign it in the Inspector.");
        }
    }

    private void UpdateMoneyUI(int amount)
    {
        if (moneyText != null)
        {
            // Formatting the string to show a currency symbol (e.g., $ 50)
            moneyText.text = "$ " + amount.ToString();
        }
        else
        {
            Debug.LogWarning("[UI_PlayerHUD] Money Text is missing! Please assign it in the Inspector.");
        }
    }
}