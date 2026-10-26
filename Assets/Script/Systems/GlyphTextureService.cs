using System.Collections.Generic;
using UnityEngine;

public static class GlyphTextureService
{
    private static Dictionary<string, Sprite> spriteMap;
    private const string TextureSheetResourcePath = "InputIconsSheet"; // Assumes texture is in a Resources folder

    public static void Initialize(string resourcePath = TextureSheetResourcePath)
    {
        if (spriteMap != null) return;

        spriteMap = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        Sprite[] subSprites = Resources.LoadAll<Sprite>(resourcePath);

        if ((subSprites == null || subSprites.Length == 0) && resourcePath == TextureSheetResourcePath)
        {
            subSprites = Resources.LoadAll<Sprite>("Icon_pack");
        }

        if (subSprites == null || subSprites.Length == 0)
        {
            Debug.LogWarning($"[GlyphTextureService] No sub-sprites found at Resources path: '{resourcePath}'");
            return;
        }

        foreach (Sprite s in subSprites)
        {
            spriteMap[s.name] = s;
        }
    }

    public static Sprite GetSprite(string glyphName)
    {
        if (string.IsNullOrEmpty(glyphName)) return null;

        if (spriteMap == null) Initialize();

        if (spriteMap != null && spriteMap.TryGetValue(glyphName, out Sprite sprite))
        {
            return sprite;
        }

        return null;
    }

    public static void ClearCache()
    {
        spriteMap?.Clear();
        spriteMap = null;
    }
}

