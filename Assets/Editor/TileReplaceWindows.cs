using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class TileReplacerWindow : EditorWindow
{
    private Tilemap targetTilemap;
    private TileBase oldTile;
    private TileBase newTile;

    [MenuItem("Tools/Tile Replacer")]
    public static void ShowWindow()
    {
        GetWindow<TileReplacerWindow>("Tile Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Thay thế nhanh Tile / RuleTile", EditorStyles.boldLabel);

        targetTilemap = (Tilemap)EditorGUILayout.ObjectField("Target Tilemap", targetTilemap, typeof(Tilemap), true);
        oldTile = (TileBase)EditorGUILayout.ObjectField("RuleTile Cũ", oldTile, typeof(TileBase), false);
        newTile = (TileBase)EditorGUILayout.ObjectField("RuleTile Mới", newTile, typeof(TileBase), false);

        if (GUILayout.Button("Thay thế"))
        {
            if (targetTilemap == null || oldTile == null || newTile == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng kéo đủ Tilemap, RuleTile cũ và RuleTile mới!", "OK");
                return;
            }

            // Ghi nhận Undo để có thể Ctrl+Z nếu cần
            Undo.RecordObject(targetTilemap, "Swap Tiles");

            // Thực hiện thay thế
            targetTilemap.SwapTile(oldTile, newTile);
            targetTilemap.RefreshAllTiles();

            // Đánh dấu scene đã thay đổi để lưu được
            EditorUtility.SetDirty(targetTilemap);
            Debug.Log($"Đã thay thế thành công {oldTile.name} bằng {newTile.name} trên {targetTilemap.name}.");
        }
    }
}