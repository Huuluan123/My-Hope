using UnityEngine;
using UnityEngine.EventSystems; // Bắt buộc phải có thư viện này

public class UIHoverSelect : MonoBehaviour, IPointerEnterHandler
{
    // Hàm này tự động chạy khi con trỏ chuột ĐI VÀO phạm vi của nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Ép EventSystem chọn nút này ngay lập tức
        // Điều này sẽ tự động tước quyền Selected của nút cũ (như nút Play)
        EventSystem.current.SetSelectedGameObject(gameObject);
        Debug.Log("OnPointerEnter: " + gameObject.name + " is now selected!");
    }
}