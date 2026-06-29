using TMPro;
using UnityEngine;

public class UI_Grapple : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo thả Text Mesh Pro hiển thị số lần móc vào đây")]
    public TextMeshProUGUI grappleCounterText;

    // Biến lưu trữ con số của frame trước đó, giúp tối ưu hiệu năng
    private int lastRemainingGrapples = -1;

    void Update()
    {
        // Kiểm tra xem PlayerController đã xuất hiện trong Scene chưa
        if (PlayerController.Instance == null) return;

        // Tính toán số lần bắn tay móc còn lại
        int remainingGrapples = PlayerController.Instance.maxGrappleCount - PlayerController.Instance.currentGrappleCount;

        // CHỈ cập nhật nội dung chữ khi con số thực sự thay đổi
        if (remainingGrapples != lastRemainingGrapples)
        {
            UpdateUI(remainingGrapples);
            lastRemainingGrapples = remainingGrapples; // Cập nhật lại trí nhớ
        }
    }

    // Hàm riêng biệt để xử lý hiển thị
    private void UpdateUI(int amount)
    {
        if (grappleCounterText != null)
        {
            // Hiển thị định dạng "X 2", "X 1", "X 0"
            grappleCounterText.text = "X " + amount.ToString();
        }
        else
        {
            Debug.LogWarning("[GrappleUIManager] Bạn quên chưa kéo TextMeshPro vào Script!");
        }
    }
}
