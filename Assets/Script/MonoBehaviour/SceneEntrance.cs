using UnityEngine;

public class SceneEntrance : MonoBehaviour
{
    [Header("Entrance Settings")]
    [Tooltip("Mã cửa, phải khớp với Exit của Scene trước")]
    public string entranceID; 
    
    [Tooltip("Lực đẩy nhân vật khi vừa xuất hiện (VD: X=8 là sang phải, Y=15 là nhảy lên)")]
    public Vector2 pushVelocity = new Vector2(8f, 0f);
    
    [Tooltip("Thời gian nhân vật bị ép di chuyển (Ví dụ: 0.2s)")]
    public float autoWalkDuration = 0.2f;
}