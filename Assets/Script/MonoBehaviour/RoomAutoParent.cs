using UnityEngine;
using System.Collections.Generic;

public class RoomAutoParent : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    public void AutoParentElements()
    {
        bool triggeredChildren = false;

        // 1. ỦY QUYỀN CHO CÁC PHÒNG CON (Recursive Delegation)
        // Quét các Object con trực tiếp. Nếu nó cũng là một Phòng, ra lệnh cho nó tự gom đồ!
        foreach (Transform child in transform)
        {
            RoomAutoParent childRoom = child.GetComponent<RoomAutoParent>();
            if (childRoom != null)
            {
                childRoom.AutoParentElements();
                triggeredChildren = true;
            }
        }

        // 2. LẤY COLLIDER CỦA PHÒNG NÀY
        // Ưu tiên GetComponent (lấy collider của chính nó) thay vì GetComponentInChildren.
        // Vì nếu Object tổng "Rooms" dùng GetComponentInChildren, nó sẽ lấy nhầm Collider của phòng con!
        if (roomBounds == null) roomBounds = GetComponent<BoxCollider2D>();
        if (roomBounds == null) roomBounds = GetComponentInChildren<BoxCollider2D>();

        // Nếu Object này không có Collider (Ví dụ: nó chỉ là Object "Rooms" tổng)
        if (roomBounds == null)
        {
            // Chỉ báo lỗi nếu nó cũng KHÔNG có phòng con nào
            if (!triggeredChildren)
            {
                Debug.LogWarning($"[RoomAutoParent] Warning: Room {gameObject.name} doesn't have a BoxCollider2D to scan!");
            }
            return; // Dừng lại, không chạy logic quét đồ bên dưới nữa
        }

        // 3. LOGIC GOM ĐỒ CỦA TỪNG PHÒNG RIÊNG LẺ
        int count = 0;
        Bounds bounds = roomBounds.bounds;

        // QUÉT THEO TAG ("LevelElement")
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("LevelElement");
        foreach (GameObject obj in taggedObjects)
        {
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

        // QUÉT THEO INTERFACE (IRoomElement)
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

        // In Log thông báo
        if (count > 0)
            Debug.Log($"<color=#00FFFF>[Auto Parent]</color> Automatically grouped {count} objects into <b>{gameObject.name}</b>!");
        else if (!triggeredChildren) // Tắt bớt log rác nếu đây là Object tổng
            Debug.Log($"<color=#FFFF00>[Auto Parent]</color> {gameObject.name} didn't find any new objects.");
    }

    // HÀM MỚI: Thả các vật thể ra ngoài (Unparent)
    public void ReleaseElements()
    {
        // 1. ỦY QUYỀN CHO CÁC PHÒNG CON THẢ ĐỒ
        foreach (Transform child in transform)
        {
            RoomAutoParent childRoom = child.GetComponent<RoomAutoParent>();
            if (childRoom != null)
            {
                childRoom.ReleaseElements();
            }
        }

        // 2. LOGIC THẢ ĐỒ CỦA PHÒNG NÀY
        int count = 0;

        // Quét ngược từ dưới lên để an toàn khi thay đổi danh sách con (Child Count)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            // BẢO VỆ CẤU TRÚC: Đừng thả các "Phòng con" ra khỏi Object tổng "Rooms"!
            if (child.GetComponent<RoomAutoParent>() != null) continue;

            // Chỉ thả những vật có đúng Tag hoặc Interface
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