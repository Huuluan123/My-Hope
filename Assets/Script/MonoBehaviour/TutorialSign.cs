using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Text.RegularExpressions;
using UnityEngine.Localization;

[ExecuteAlways]
public class TutorialSign : MonoBehaviour
{
    [Header("Global Database")]
    [Tooltip("Kéo file ScriptableObject GlobalInputDatabase vào đây")]
    [SerializeField] private InputDatabase inputDatabase;

    [Header("Localization Settings")]
    [SerializeField] private LocalizedString localizedMessage;

    [Header("UI References")]
    [SerializeField] private Transform signContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Editor Preview")]
    [SerializeField] private DeviceType previewDevice = DeviceType.Keyboard;
    [SerializeField] private float duration = 0.3f;

    private bool isPlayerInside = false;
    private string qteTemplateOverride = "";

    // Biến lưu trữ chữ đã dịch sau khi hệ thống tải xong
    private string currentTranslatedText = "";

    private void OnValidate()
    {
        if (promptText != null && inputDatabase != null) RefreshUI(previewDevice);
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
        if (Application.isPlaying)
        {
            if (InputDeviceManager.Instance != null)
                InputDeviceManager.Instance.OnDeviceChanged += OnDeviceChangedHandler;

            // 🌟 CHÌA KHÓA VÀNG Ở ĐÂY:
            // Đăng ký nhận chữ. Nó sẽ TỰ ĐỘNG chờ hệ thống Load xong rồi mới gọi hàm OnStringChanged.
            // Nó cũng TỰ ĐỘNG gọi lại hàm này mỗi khi người chơi đổi ngôn ngữ trong Settings!
            if (localizedMessage != null && !localizedMessage.IsEmpty)
            {
                localizedMessage.StringChanged += OnStringChanged;
            }
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            if (InputDeviceManager.Instance != null)
                InputDeviceManager.Instance.OnDeviceChanged -= OnDeviceChangedHandler;

            if (localizedMessage != null && !localizedMessage.IsEmpty)
            {
                localizedMessage.StringChanged -= OnStringChanged;
            }
        }
    }

    private void Start()
    {
        // ❌ KHÔNG GỌI RefreshUI Ở ĐÂY NỮA VÌ DỮ LIỆU CHƯA TẢI XONG
    }

    // Sự kiện nổ ra khi biến LocalizedString lấy chữ thành công
    private void OnStringChanged(string translatedText)
    {
        currentTranslatedText = translatedText; // Lưu lại câu thoại (VD: "Giữ {Jump} để nhảy")

        DeviceType currentDevice = InputDeviceManager.Instance != null ? InputDeviceManager.Instance.CurrentDevice : DeviceType.Keyboard;
        RefreshUI(currentDevice); // Tiến hành chèn Icon
    }

    private void OnDeviceChangedHandler(DeviceType device)
    {
        RefreshUI(device);
    }

    private void RefreshUI(DeviceType device)
    {
        if (inputDatabase == null || promptText == null) return;

        string finalMessage = "";

        if (Application.isPlaying && isPlayerInside && !string.IsNullOrEmpty(qteTemplateOverride))
        {
            finalMessage = qteTemplateOverride;
        }
        else
        {
            if (Application.isPlaying)
            {
                // Lúc chơi game: Dùng chuỗi đã được hệ thống StringChanged lấy về an toàn
                finalMessage = currentTranslatedText;
            }
            else
            {
                // Lúc ở màn hình Editor (Chưa bấm Play): Dùng lệnh ép lấy chữ để Preview
                if (localizedMessage != null && !localizedMessage.IsEmpty)
                {
                    try { finalMessage = localizedMessage.GetLocalizedString(); }
                    catch { finalMessage = "Preview not available in Editor"; }
                }
            }
        }

        if (string.IsNullOrEmpty(finalMessage))
        {
            promptText.text = "Pick a key in Localization Table";
            return;
        }

        // BỘ QUÉT REGEX THẦN THÁNH
        MatchCollection matches = Regex.Matches(finalMessage, @"\{([^}]+)\}");

        foreach (Match match in matches)
        {
            string fullTag = match.Value;
            string actionID = match.Groups[1].Value;

            string spriteTag = inputDatabase.GetSpriteTag(actionID, device);
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

            if (InputDeviceManager.Instance != null) RefreshUI(InputDeviceManager.Instance.CurrentDevice);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (Application.isPlaying && other.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            qteTemplateOverride = "";
            signContainer.DOKill();
            signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }
    }

    public void ShowCutscenePrompt(string template)
    {
        qteTemplateOverride = template;

        if (InputDeviceManager.Instance != null)
        {
            RefreshUI(InputDeviceManager.Instance.CurrentDevice);
        }

        isPlayerInside = true;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    public void HideCutscenePrompt()
    {
        isPlayerInside = false;
        qteTemplateOverride = "";
        signContainer.DOKill();
        signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }
}