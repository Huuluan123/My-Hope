using UnityEngine;
using System.Collections;

public class GrappleNode : MonoBehaviour, IRoomElement
{
    [Header("Grapple Settings")]
    public float cooldownTime = 3f;
    public bool isActive = true;
    public float boostForce = 18.4f; // Ngang JumpForce của player

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    // Hàm này sẽ do RobotHand hoặc Player gọi khi tay cắm trúng vật thể này
    public void OnGrappleSuccess()
    {
        if (!isActive) return;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        // 1. Vô hiệu hóa
        isActive = false;
        UpdateVisuals();

        // 2. Chờ 3 giây
        yield return new WaitForSeconds(cooldownTime);

        // 3. Kích hoạt lại
        isActive = true;
        UpdateVisuals();
    }

    public void ResetState()
    {
        StopAllCoroutines(); // Hủy bộ đếm giờ nếu đang chạy dở
        isActive = true;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? activeColor : inactiveColor;
        }
    }
}
