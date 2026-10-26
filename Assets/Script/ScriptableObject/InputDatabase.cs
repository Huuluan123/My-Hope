using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActionIconMapping
{
    [Tooltip("Tên hành động gõ trong ngoặc nhọn. VD: Jump, Dash, Attack")]
    public string actionID;

    public KeyboardIconName keyboardIcon;
    public XboxIconName xboxIcon;
    public PSIconName psIcon;
    public NintendoIconName nintendoIcon;
}

[CreateAssetMenu(fileName = "GlobalInputDatabase", menuName = "System/Input Database")]
public class InputDatabase : ScriptableObject
{
    public List<ActionIconMapping> mappings = new List<ActionIconMapping>();

    public string GetGlyphName(string actionID, DeviceType device)
    {
        ActionIconMapping mapping = mappings.Find(x => x.actionID.Equals(actionID, System.StringComparison.OrdinalIgnoreCase));
        if (mapping == null) return string.Empty;

        return device switch
        {
            DeviceType.Xbox => mapping.xboxIcon.ToString(),
            DeviceType.PlayStation => mapping.psIcon.ToString(),
            DeviceType.Nintendo => mapping.nintendoIcon.ToString(),
            _ => mapping.keyboardIcon.ToString()
        };
    }

    // Hàm toàn cục để các script khác "hỏi xin" tên thẻ Sprite
    public string GetSpriteTag(string actionID, DeviceType device)
    {
        string glyphName = GetGlyphName(actionID, device);
        if (string.IsNullOrEmpty(glyphName)) return $"[{actionID}_MISSING]";

        return $"<sprite name=\"{glyphName}\">";
    }
}