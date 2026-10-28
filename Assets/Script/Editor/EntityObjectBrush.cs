#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityPrefabBrush", menuName = "2D/Brushes/Entity Prefab Brush")]
[CustomGridBrush(false, true, false, "Entity Prefab Brush")]
public class EntityPrefabBrush : GameObjectBrush
{
    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        // 1. Tìm container "Entities" của phòng nếu brushTarget nằm trong phòng
        GameObject targetParent = brushTarget;

        if (brushTarget != null)
        {
            RoomManager roomManager = brushTarget.GetComponentInParent<RoomManager>();
            if (roomManager != null)
            {
                Transform entitiesContainer = roomManager.transform.Find("Entities");
                if (entitiesContainer != null)
                {
                    targetParent = entitiesContainer.gameObject;
                }
            }
        }

        // 2. Vẽ Prefab và gán vào đúng cha
        base.Paint(gridLayout, targetParent, position);
    }
}
#endif