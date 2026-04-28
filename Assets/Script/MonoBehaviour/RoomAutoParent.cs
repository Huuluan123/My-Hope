using UnityEngine;
using System.Collections.Generic;

public class RoomAutoParent : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    public void AutoParentElements()
    {
        if (roomBounds == null) roomBounds = GetComponentInChildren<BoxCollider2D>();

        if (roomBounds == null)
        {
            Debug.LogWarning($"[RoomAutoParent] Warning: Room {gameObject.name} doesn't have a BoxCollider2D to scan!");
            return;
        }

        int count = 0;
        Bounds bounds = roomBounds.bounds;

        // 1. QUÉT THEO TAG ("LevelElement")
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("LevelElement");
        foreach (GameObject obj in taggedObjects)
        {
            if (obj == this.gameObject || obj.transform.IsChildOf(this.transform)) continue;

            if (bounds.Contains(obj.transform.position))
            {
                // Ghi nhận Undo (Ctrl+Z) cho Editor
#if UNITY_EDITOR
                UnityEditor.Undo.SetTransformParent(obj.transform, this.transform, "Auto Parent");
#else
                obj.transform.SetParent(this.transform);
#endif
                count++;
            }
        }

        // 2. QUÉT THEO INTERFACE (IRoomElement)
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (MonoBehaviour script in allScripts)
        {
            if (script is IRoomElement)
            {
                GameObject obj = script.gameObject;

                if (obj == this.gameObject || obj.transform.IsChildOf(this.transform)) continue;

                if (bounds.Contains(obj.transform.position))
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.SetTransformParent(obj.transform, this.transform, "Auto Parent");
#else
                    obj.transform.SetParent(this.transform);
#endif
                    count++;
                }
            }
        }

        if (count > 0)
            Debug.Log($"<color=#00FFFF>[Auto Parent]</color> Automatically grouped {count} objects into <b>{gameObject.name}</b>!");
        else
            Debug.Log($"<color=#FFFF00>[Auto Parent]</color> {gameObject.name} didn't find any new objects.");
    }

    // HÀM MỚI: Thả các vật thể ra ngoài (Unparent)
    public void ReleaseElements()
    {
        int count = 0;

        // Quét ngược từ dưới lên để an toàn khi thay đổi danh sách con (Child Count)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            // Chỉ thả những vật có đúng Tag hoặc Interface (Tránh thả nhầm Collider/Camera của phòng)
            bool isLevelElement = child.CompareTag("LevelElement") || child.GetComponent<IRoomElement>() != null;

            if (isLevelElement)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.SetTransformParent(child, null, "Release Elements");
#else
                child.SetParent(null);
#endif
                count++;
            }
        }

        if (count > 0)
            Debug.Log($"<color=#FF9900>[Auto Parent]</color> Released <b>{count}</b> objects from room <b>{gameObject.name}</b>!");
    }
}