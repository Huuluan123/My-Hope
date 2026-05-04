using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

// CHIA NHỎ ENUM THEO TỪNG HỆ MÁY
public enum KeyboardIconName
{
    key_A, key_B, key_C, key_D, key_E, key_F, key_Q, key_R, key_S, key_V, key_W, key_X, key_Z,
    key_Space, key_Enter, key_Shift, key_Ctrl, key_Alt, key_Esc, key_Tab,
    key_Up, key_Down, key_Left, key_Right,
    mouse_LeftClick, mouse_RightClick, mouse_MiddleClick
}

public enum XboxIconName
{
    stick_Left, stick_Right, stick_Up, stick_Down, stick_LeftRight,
    xbox_A, xbox_B, xbox_X, xbox_Y,
    xbox_LB, xbox_RB, xbox_LT, xbox_RT,
    xbox_DpadUp, xbox_DpadDown, xbox_DpadLeft, xbox_DpadRight,
    xbox_Menu, xbox_View, xbox_LS, xbox_RS
}

public enum PSIconName
{
    stick_Left, stick_Right, stick_Up, stick_Down, stick_LeftRight,
    ps_Cross, ps_Circle, ps_Square, ps_Triangle,
    ps_L1, ps_R1, ps_L2, ps_R2,
    ps_DpadUp, ps_DpadDown, ps_DpadLeft, ps_DpadRight,
    ps_Options, ps_Share, ps_Touchpad,
    ps_L3, ps_R3
}

public enum NintendoIconName
{
    stick_Left, stick_Right, stick_Up, stick_Down, stick_LeftRight,
    nin_A, nin_B, nin_X, nin_Y,
    nin_L, nin_R, nin_ZL, nin_ZR,
    nin_DpadUp, nin_DpadDown, nin_DpadLeft, nin_DpadRight,
    nin_Plus, nin_Minus, nin_LS, nin_RS
}

// 🌟 1. TẠO CLASS QUẢN LÝ TỪNG NHÓM PHÍM (Được phép hiển thị trong Editor)
[System.Serializable]
public class PromptAction
{
    [Tooltip("Từ khóa để thay trong chữ. Ví dụ: {icon1}, {icon2}, {move}...")]
    public string placeholder = "{icon1}";

    public KeyboardIconName keyboardIcon = KeyboardIconName.key_C;
    public XboxIconName xboxIcon = XboxIconName.xbox_A;
    public PSIconName psIcon = PSIconName.ps_Cross;
    public NintendoIconName nintendoIcon = NintendoIconName.nin_B;
}

[ExecuteAlways]
public class TutorialSign : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform signContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Message Settings")]
    [Tooltip("Ví dụ: Hold {icon1} and press {icon2} to Dash")]
    [SerializeField] private string messageTemplate = "Press {icon1} to Jump";

    // 🌟 2. CHUYỂN THÀNH DANH SÁCH (LIST) ĐỂ THÊM BAO NHIÊU NÚT TÙY Ý
    [Header("Setup Icons")]
    [SerializeField] private List<PromptAction> iconList = new List<PromptAction>();

    [Header("Editor Preview")]
    [SerializeField] private DeviceType previewDevice = DeviceType.Keyboard;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;

    private bool isPlayerInside = false;

    private void OnValidate()
    {
        if (promptText != null)
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
        // Copy chuỗi gốc ra để bắt đầu thay thế
        string finalMessage = messageTemplate;

        // 🌟 3. QUÉT QUA TOÀN BỘ DANH SÁCH VÀ THAY THẾ TỪNG TỪ KHÓA
        foreach (var action in iconList)
        {
            string spriteName = "";

            switch (device)
            {
                case DeviceType.Keyboard: spriteName = action.keyboardIcon.ToString(); break;
                case DeviceType.Xbox: spriteName = action.xboxIcon.ToString(); break;
                case DeviceType.PlayStation: spriteName = action.psIcon.ToString(); break;
                case DeviceType.Nintendo: spriteName = action.nintendoIcon.ToString(); break;
                default: spriteName = action.keyboardIcon.ToString(); break;
            }

            string iconTag = $"<sprite name=\"{spriteName}\">";

            // Thay thế từ khóa (VD: {icon1}) bằng thẻ ảnh tương ứng
            finalMessage = finalMessage.Replace(action.placeholder, iconTag);
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
    /// </summary
    public void ShowCutscenePrompt(List<PromptAction> actions, string template)
    {
        // Ghi đè danh sách nút và chữ
        iconList = actions;
        //messageTemplate = template;

        // Cập nhật giao diện ngay lập tức
        if (InputDeviceManager.Instance != null)
        {
            RefreshUI(InputDeviceManager.Instance.CurrentDevice);
        }

        // Chạy hiệu ứng DoTween hiện lên
        isPlayerInside = true;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Ẩn bảng Sign bằng Code
    /// </summary>
    public void HideCutscenePrompt()
    {
        isPlayerInside = false;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }
}