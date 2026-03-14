using UnityEngine;

[CreateAssetMenu(fileName = "WorldStateSO", menuName = "Scriptable Objects/WorldStateSO")]
public class WorldStateSO : ScriptableObject
{
    /*
    // Danh sách lưu trữ ID của các sự kiện đã hoàn thành (Mở cửa, giết Boss, nhặt đồ)
    public List<string> completedEvents = new List<string>();

    public void AddEvent(string eventID)
    {
        if (!completedEvents.Contains(eventID))
        {
            completedEvents.Add(eventID);
        }
    }

    public bool CheckEvent(string eventID)
    {
        return completedEvents.Contains(eventID);
    }

    public void ResetWorld()
    {
        completedEvents.Clear();
    }
     */
}
