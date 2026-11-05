using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CustomRuleTile", menuName = "2D/Tiles/Custom Rule Tile")]
public class CustomRuleTile : RuleTile
{
    [Header("Custom Autotile Settings")]
    [Tooltip("Danh sách các Tile khác được coi là cùng loại (nối liền mạch, không sinh viền ngăn cách)")]
    public List<TileBase> connectableTiles = new List<TileBase>();

    [Tooltip("Coi mép phòng (ngoài ranh giới Room Bounds) như là tile cứng để không bị bo viền cạnh?")]
    public bool clampToRoomBoundary = true;

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        // Lấy thông tin Bounds của phòng (nếu có RoomManager / BoxCollider2D ở Root)
        BoundsInt roomBounds = GetRoomBounds(tilemap);

        for (int i = 0; i < rule.m_Neighbors.Count; i++)
        {
            Vector3Int offset = GetOffsetPosition(i);
            Vector3Int neighborPos = position + offset;

            int neighborType = rule.m_Neighbors[i];

            // 1. XỬ LÝ RANH GIỚI PHÒNG (Room Boundary Clamping)
            if (clampToRoomBoundary && roomBounds.size != Vector3Int.zero)
            {
                // Nếu vị trí lân cận rơi ra ngoài khung chữ nhật của phòng
                if (neighborPos.x < roomBounds.xMin || neighborPos.x >= roomBounds.xMax ||
                    neighborPos.y < roomBounds.yMin || neighborPos.y >= roomBounds.yMax)
                {
                    // Ngoài phòng được xem là "Có gạch cứng" (This)
                    if (neighborType == TilingRuleOutput.Neighbor.NotThis)
                        return false;

                    continue; // Thỏa mãn điều kiện Neighbor.This, xét tiếp ô khác
                }
            }

            // 2. XỬ LÝ KHỚP VỚI CÁC RULETILE KHÁC (Connectable Tiles)
            TileBase neighborTile = tilemap.GetTile(neighborPos);

            if (neighborType == TilingRuleOutput.Neighbor.This)
            {
                // Coi là khớp nếu là chính nó, HOẶC nằm trong danh sách connectableTiles
                bool isMatch = (neighborTile == this) || (neighborTile != null && connectableTiles.Contains(neighborTile));
                if (!isMatch)
                    return false;
            }
            else if (neighborType == TilingRuleOutput.Neighbor.NotThis)
            {
                // Không được là chính nó, VÀ không được nằm trong danh sách connectableTiles
                bool isBlocked = (neighborTile == this) || (neighborTile != null && connectableTiles.Contains(neighborTile));
                if (isBlocked)
                    return false;
            }
        }

        return true;
    }

    private BoundsInt GetRoomBounds(ITilemap tilemap)
    {
        // Tìm RoomManager hoặc BoxCollider2D của phòng chứa Tilemap này
        var tilemapComponent = tilemap.GetComponent<Tilemap>();
        if (tilemapComponent == null) return default;

        var col = tilemapComponent.GetComponentInParent<BoxCollider2D>();
        if (col != null)
        {
            Vector3 minWorld = col.bounds.min;
            Vector3 maxWorld = col.bounds.max;

            Vector3Int minCell = tilemapComponent.WorldToCell(minWorld);
            Vector3Int maxCell = tilemapComponent.WorldToCell(maxWorld);

            return new BoundsInt(minCell, maxCell - minCell);
        }

        return default;
    }

    private Vector3Int GetOffsetPosition(int index)
    {
        // Chuyển index của ma trận 3x3 thành Vector3Int offset (-1 đến 1)
        switch (index)
        {
            case 0: return new Vector3Int(-1, 1, 0);
            case 1: return new Vector3Int(0, 1, 0);
            case 2: return new Vector3Int(1, 1, 0);
            case 3: return new Vector3Int(-1, 0, 0);
            case 4: return new Vector3Int(1, 0, 0);
            case 5: return new Vector3Int(-1, -1, 0);
            case 6: return new Vector3Int(0, -1, 0);
            case 7: return new Vector3Int(1, -1, 0);
            default: return Vector3Int.zero;
        }
    }
}