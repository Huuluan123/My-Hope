using UnityEngine;
using UnityEngine.UIElements;

// [UxmlElement] yêu cầu class phải có từ khóa partial trên Unity 6
[UxmlElement]
public partial class ActionGlyph : VisualElement
{
    // [UxmlAttribute] cho phép tùy chỉnh thuộc tính trực tiếp từ Inspector và UI Builder
    [UxmlAttribute("action-id")]
    public string actionId { get; set; } = "Jump";

    [UxmlAttribute("icon-size")]
    public float iconSize { get; set; } = 24f;

    public ActionGlyph()
    {
        style.width = iconSize;
        style.height = iconSize;
        style.flexShrink = 0;

        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        UpdateIcon();
        if (Application.isPlaying && InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnDeviceChanged += OnDeviceChanged;
        }
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnDeviceChanged -= OnDeviceChanged;
        }
    }

    private void OnDeviceChanged(DeviceType device)
    {
        UpdateIcon();
    }

    public void UpdateIcon()
    {
        style.width = iconSize;
        style.height = iconSize;

        if (string.IsNullOrEmpty(actionId)) return;

        InputDatabase db = Resources.Load<InputDatabase>("GlobalInputDatabase");
        if (db == null) return;

        DeviceType device = (Application.isPlaying && InputDeviceManager.Instance != null)
            ? InputDeviceManager.Instance.CurrentDevice
            : DeviceType.Keyboard;

        string glyphName = db.GetGlyphName(actionId, device);
        Sprite iconSprite = GlyphTextureService.GetSprite(glyphName);

        if (iconSprite != null)
        {
            style.backgroundImage = new StyleBackground(iconSprite);
        }
        else
        {
            style.backgroundImage = StyleKeyword.Null;
        }
    }
}