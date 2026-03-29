using UnityEngine;
using System.Collections.Generic;

// Phép thuật này giúp script chạy liên tục ngay cả khi bạn chưa bấm nút Play!
[ExecuteAlways]
public class RoomAutoParent : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    [Tooltip("Toogle auto gathering objects (Auto parent). Turn off if the number of levels is too big and causes lag to editor.")]
    public bool autoScanInEditor = true;
    public float scanInterval = 0.5f; // Quét mỗi 0.5 giây

    private float nextScanTime;

    private void Awake()
    {
        roomBounds = GetComponentInChildren<BoxCollider2D>();
    }

    private void Update()
    {
        // BẢO VỆ 1: Khi game đang chạy (Play Mode), TẮT TÍNH NĂNG NÀY ĐỂ GAME KHÔNG BỊ LAG!
        if (Application.isPlaying) return;

        // BẢO VỆ 2: Công tắc tắt/bật cho Designer
        if (!autoScanInEditor) return;

        // BẢO VỆ 3: Bộ đếm thời gian mỗi 0.5 giây (Dùng realtime để chạy mượt trong Editor)
        if (Time.realtimeSinceStartup >= nextScanTime)
        {
            nextScanTime = Time.realtimeSinceStartup + scanInterval;
            AutoParentElements();
        }
    }

    [ContextMenu("Gom Đồ Vật (Goi Thủ Công)")]
    public void AutoParentElements()
    {
        if (roomBounds == null)
        {
            roomBounds = GetComponentInChildren<BoxCollider2D>();
            if (roomBounds == null) return; // Nếu phòng chưa có Collider thì im lặng bỏ qua, không báo lỗi
        }

        int count = 0;
        Bounds bounds = roomBounds.bounds;

        // 1. QUÉT THEO TAG ("RoomElement")
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("LevelElement");
        foreach (GameObject obj in taggedObjects)
        {
            if (obj == this.gameObject || obj.transform.IsChildOf(this.transform)) continue;

            if (bounds.Contains(obj.transform.position))
            {
                obj.transform.SetParent(this.transform);
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
                    obj.transform.SetParent(this.transform);
                    count++;
                }
            }
        }

        // CHỈ BÁO CÁO KHI CÓ VẬT THỂ MỚI (Tránh spam ngập lụt Console mỗi 0.5 giây)
        if (count > 0)
        {
            Debug.Log($"<color=#00FFFF>[Auto Parent]</color> Đã tự động gom {count} đồ vật vào <b>{gameObject.name}</b>!");
        }
    }
}