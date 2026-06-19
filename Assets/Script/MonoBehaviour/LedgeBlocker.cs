using UnityEngine;

public class LedgeBlocker : MonoBehaviour
{
    // Hàm này sẽ tự quyết định xem có chặn Player hay không
    public bool HopBlockCheck(PlayerController player)
    {
        return true; // Mặc định là chặn
    }
}