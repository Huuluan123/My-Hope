using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public void OnRoomEntered()
    {
        // 1. Tìm TẤT CẢ các vật thể con (kể cả những đứa đang bị SetActive(false)) có mang hợp đồng IRoomElement
        IRoomElement[] allElements = GetComponentsInChildren<IRoomElement>(true);

        // 2. Ra lệnh cho từng đứa tự reset lại bản thân
        foreach (IRoomElement element in allElements)
        {
            element.ResetState();
        }
    }
}