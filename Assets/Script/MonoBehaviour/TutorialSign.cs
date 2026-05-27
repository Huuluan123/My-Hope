using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Text.RegularExpressions; // Thư viện dùng để dò tìm chữ trong ngoặc nhọn

[ExecuteAlways]
public class TutorialSign : MonoBehaviour
{
    [Header("Global Database")]
    [Tooltip("Kéo file GlobalInputDatabase vào đây")]
    [SerializeField] private InputDatabase inputDatabase;

    [Header("UI References")]
    [SerializeField] private Transform signContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Message Settings")]
    [Tooltip("Ví dụ: Press {Jump} to jump, then press {Dash} to dash in mid-air.")]
    [SerializeField][TextArea] private string messageTemplate = "Press {Jump} to Jump";

    [Header("Editor Preview")]
    [SerializeField] private DeviceType previewDevice = DeviceType.Keyboard;
    [SerializeField] private float duration = 0.3f;

    private bool isPlayerInside = false;

    private void OnValidate()
    {
        if (promptText != null && inputDatabase != null)
        {
            RefreshUI(previewDevice);
        }
    }

    void Awake()
    {
        if (Application.isPlaying && signContainer != null)
        {
            signContainer.localScale = Vector3.zero;
        }
    }

    void OnEnable()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged -= RefreshUI;
    }

    private void Start()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
            RefreshUI(InputDeviceManager.Instance.CurrentDevice);
    }

    private void RefreshUI(DeviceType device)
    {
        if (inputDatabase == null) return;

        string finalMessage = messageTemplate;

        // 🌟 KỸ THUẬT QUÉT TỰ ĐỘNG (Regex):
        // Tìm tất cả các cụm chữ nằm trong dấu ngoặc nhọn { }
        MatchCollection matches = Regex.Matches(finalMessage, @"\{([^}]+)\}");

        foreach (Match match in matches)
        {
            string fullTag = match.Value;      // Kết quả: "{Jump}"
            string actionID = match.Groups[1].Value; // Kết quả: "Jump"

            // Hỏi Bộ Não xem thẻ Sprite của hành động "Jump" là gì?
            string spriteTag = inputDatabase.GetSpriteTag(actionID, device);

            // Thay thế chữ {Jump} bằng thẻ <sprite name="xbox_A">
            finalMessage = finalMessage.Replace(fullTag, spriteTag);
        }

        promptText.text = finalMessage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Application.isPlaying && other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            signContainer.DOKill();
            signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (Application.isPlaying && other.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            signContainer.DOKill();
            signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }
    }

    /// <summary>
    /// Hiển thị bảng Sign bằng Code (Dành cho Cutscene / QTE)
    /// </summary>
    public void ShowCutscenePrompt(string template)
    {
        // Ghi đè chữ hiện tại bằng chuỗi template truyền từ QTEManager hoặc Timeline
        messageTemplate = template;

        // Cập nhật giao diện hiển thị ngay lập tức theo thiết bị đang cắm
        if (InputDeviceManager.Instance != null)
        {
            RefreshUI(InputDeviceManager.Instance.CurrentDevice);
        }

        // Chạy hiệu ứng DoTween để bảng Sign phóng to hiện lên mượt mà
        isPlayerInside = true;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Ẩn bảng Sign bằng Code khi kết thúc QTE hoặc Cutscene
    /// </summary>
    public void HideCutscenePrompt()
    {
        isPlayerInside = false;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }
}