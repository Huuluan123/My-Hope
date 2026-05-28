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

    // Hàm toàn cục để các script khác "hỏi xin" tên thẻ Sprite
    public string GetSpriteTag(string actionID, DeviceType device)
    {
        ActionIconMapping mapping = mappings.Find(x => x.actionID.ToLower() == actionID.ToLower());

        if (mapping == null) return $"[{actionID}_MISSING]"; // Cảnh báo nếu gõ sai tên hành động

        string spriteName = "";
        switch (device)
        {
            case DeviceType.Keyboard: spriteName = mapping.keyboardIcon.ToString(); break;
            case DeviceType.Xbox: spriteName = mapping.xboxIcon.ToString(); break;
            case DeviceType.PlayStation: spriteName = mapping.psIcon.ToString(); break;
            case DeviceType.Nintendo: spriteName = mapping.nintendoIcon.ToString(); break;
            default: spriteName = mapping.keyboardIcon.ToString(); break;
        }

        return $"<sprite name=\"{spriteName}\">";
    }
}