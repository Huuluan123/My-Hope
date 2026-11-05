#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RuleTileGenerator : EditorWindow
{
    private Texture2D tilesetTexture;
    private int columns = 7;
    private int rows = 10;

    [MenuItem("Tools/Generate RuleTile")]
    public static void ShowWindow()
    {
        GetWindow<RuleTileGenerator>("RuleTile Gen");
    }

    private void OnGUI()
    {
        tilesetTexture = (Texture2D)EditorGUILayout.ObjectField("Tileset Texture", tilesetTexture, typeof(Texture2D), false);
        columns = EditorGUILayout.IntField("Columns", columns);
        rows = EditorGUILayout.IntField("Rows", rows);

        if (GUILayout.Button("Auto Generate") && tilesetTexture != null)
        {
            Generate();
        }
    }

    private void Generate()
    {
        string path = AssetDatabase.GetAssetPath(tilesetTexture);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

        List<Sprite> sprites = new List<Sprite>();
        foreach (var obj in subAssets)
        {
            if (obj is Sprite s) sprites.Add(s);
        }

        // Tạo asset RuleTile mới
        RuleTile ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        ruleTile.m_TilingRules = new List<RuleTile.TilingRule>();

        // Ví dụ: Tự động gán rule Ruột Trung Tâm (Center Fill) cho sprite ở tọa độ (3, 4)
        RuleTile.TilingRule centerRule = new RuleTile.TilingRule();
        centerRule.m_Neighbors = new List<int>
        {
            RuleTile.TilingRuleOutput.Neighbor.This, RuleTile.TilingRuleOutput.Neighbor.This, RuleTile.TilingRuleOutput.Neighbor.This,
            RuleTile.TilingRuleOutput.Neighbor.This,                                          RuleTile.TilingRuleOutput.Neighbor.This,
            RuleTile.TilingRuleOutput.Neighbor.This, RuleTile.TilingRuleOutput.Neighbor.This, RuleTile.TilingRuleOutput.Neighbor.This
        };
        centerRule.m_Sprites[0] = sprites[3 + 4 * columns]; // Tọa độ tương đương tiles="3,4" của Celeste
        ruleTile.m_TilingRules.Add(centerRule);

        string savePath = $"Assets/{tilesetTexture.name}_RuleTile.asset";
        AssetDatabase.CreateAsset(ruleTile, savePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Đã sinh xong RuleTile tại: {savePath}");
    }
}
#endif