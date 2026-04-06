using UnityEngine;

// TUYỆT ĐỐI KHÔNG DÙNG [ExecuteInEditMode] Ở ĐÂY NỮA
public class ParallaxCamera : MonoBehaviour
{
    public delegate void ParallaxCameraDelegate(Vector3 deltaMovement);
    public ParallaxCameraDelegate onCameraTranslate;

    private Vector3 oldPosition;

    void Start()
    {
        oldPosition = transform.position;
    }

    // SỬA UPDATE THÀNH LATEUPDATE ĐỂ ĐỒNG BỘ VỚI CINEMACHINE
    void LateUpdate()
    {
        if (transform.position != oldPosition)
        {
            if (onCameraTranslate != null)
            {
                Vector3 delta = oldPosition - transform.position;
                onCameraTranslate(delta);
            }
            oldPosition = transform.position;
        }
    }

    // HÀM NÀY DÙNG ĐỂ CHỐNG LỖI VĂNG HÌNH KHI CHUYỂN PHÒNG (TELEPORT)
    public void ResetOldPosition()
    {
        oldPosition = transform.position;
    }
}