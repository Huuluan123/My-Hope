using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

[ExecuteAlways] // Cho phép script chạy âm thầm trong lúc thiết kế (Edit Mode)
[RequireComponent(typeof(TMP_Text))]
public class TMP_InputPreprocessor : MonoBehaviour, ITextPreprocessor
{
    [Header("Global Database")]
    [Tooltip("Kéo file GlobalInputDatabase vào đây")]
    public InputDatabase inputDatabase;

    [Header("Editor Preview")]
    [Tooltip("Đổi thiết bị ở đây để xem trước Icon ngay lập tức trên màn hình Scene")]
    public DeviceType previewDevice = DeviceType.Keyboard;

    private TMP_Text tmpText;

    private void OnEnable()
    {
        tmpText = GetComponent<TMP_Text>();

        // Đăng ký script này làm "Bộ lọc" cho TextMeshPro
        if (tmpText != null) tmpText.textPreprocessor = this;

        // Chỉ đăng ký nghe sự kiện tay cầm nếu game THỰC SỰ đang chạy
        if (Application.isPlaying && InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnDeviceChanged += OnDeviceChanged;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnDeviceChanged -= OnDeviceChanged;
        }

        // Trả lại sự trong sạch cho TMP khi tắt script
        if (tmpText != null) tmpText.textPreprocessor = null;
    }

    // ========================================================================
    // 🪄 ÉP TMP CẬP NHẬT KHI BẠN THAY ĐỔI DROPDOWN TRONG INSPECTOR
    // ========================================================================
    private void OnValidate()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();

        if (tmpText != null)
        {
            tmpText.textPreprocessor = this;

            // Hàm này "đá" TextMeshPro một cái, ép nó phải tính toán và vẽ lại chữ ngay lập tức
            tmpText.SetAllDirty();
        }
    }

    private void OnDeviceChanged(DeviceType newDevice)
    {
        // Khi game đang chạy và người chơi đổi tay cầm, ép vẽ lại chữ
        if (tmpText != null) tmpText.SetAllDirty();
    }

    // ========================================================================
    // BỘ LỌC CHỮ CỐT LÕI
    // ========================================================================
    public string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text) || inputDatabase == null)
            return text;

        // 🌟 LỰA CHỌN THIẾT BỊ THÔNG MINH
        // Mặc định lấy theo Dropdown để bạn Preview trong Editor
        DeviceType currentDevice = previewDevice;

        // Nếu game bắt đầu bấm Play, tự động phớt lờ Dropdown và lấy thiết bị thật của người chơi
        if (Application.isPlaying && InputDeviceManager.Instance != null)
        {
            currentDevice = InputDeviceManager.Instance.CurrentDevice;
        }

        string processedText = text;
        MatchCollection matches = Regex.Matches(processedText, @"\{([^}]+)\}");

        foreach (Match match in matches)
        {
            string fullTag = match.Value;            // VD: "{Dash}"
            string actionID = match.Groups[1].Value; // VD: "Dash"

            string spriteTag = inputDatabase.GetSpriteTag(actionID, currentDevice);
            processedText = processedText.Replace(fullTag, spriteTag);
        }

        return processedText;
    }
}