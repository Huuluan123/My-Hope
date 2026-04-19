using UnityEngine;
using TMPro;
using DG.Tweening; // Yêu cầu đã cài DOTween

public class TutorialSign : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform signContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack; // Hiệu ứng nảy khi hiện
    [SerializeField] private Ease hideEase = Ease.InBack;  // Hiệu ứng hóp khi ẩn

    private bool isPlayerInside = false;

    void Awake()
    {
        // Khởi tạo: Thu nhỏ bảng lại ngay từ đầu
        signContainer.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        // Lắng nghe sự kiện đổi thiết bị từ InputDeviceManager
        if (InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged += UpdateIcon;
    }

    void OnDisable()
    {
        if (InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged -= UpdateIcon;
    }

    private void Start()
    {
        // Cập nhật text ban đầu
        if (InputDeviceManager.Instance != null)
            UpdateIcon(InputDeviceManager.Instance.CurrentDevice);
    }

    private void UpdateIcon(DeviceType device)
    {
        // Sử dụng TMP Sprite Assets để chèn hình vào chữ
        switch (device)
        {
            case DeviceType.Keyboard:
                promptText.text = "Press <sprite name=\"key_C\"> to Jump";
                break;
            case DeviceType.Xbox:
                promptText.text = "Press <sprite name=\"xbox_A\"> to Jump";
                break;
            case DeviceType.PlayStation:
                promptText.text = "Press <sprite name=\"ps_X\"> to Jump";
                break;
            case DeviceType.Nintendo:
                promptText.text = "Press <sprite name=\"nin_B\"> to Jump";
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            AnimateSign(Vector3.one, showEase);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            AnimateSign(Vector3.zero, hideEase);
        }
    }

    private void AnimateSign(Vector3 targetScale, Ease easeType)
    {
        // DOKill() để tránh xung đột nếu người chơi chạy ra vào quá nhanh
        signContainer.DOKill();
        signContainer.DOScale(targetScale, duration).SetEase(easeType);
    }
}