using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;

    // Các ID Animation (Hash để tối ưu hiệu năng)
    private static readonly int JumpTrig = Animator.StringToHash("jump");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsWallGrabbing = Animator.StringToHash("isWallGrabbing");
    private static readonly int IsClimbing = Animator.StringToHash("isClimbing");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");

    private Coroutine lowStaminaCoroutine;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (controller != null)
        {
            // Cập nhật giá trị velocity.y vào Animator
            animator.SetFloat(YVelocity, controller.Velocity.y);
        }
    }

    // --- ĐĂNG KÝ LẮNG NGHE (QUAN TRỌNG NHẤT) ---
    private void OnEnable()
    {
        if (controller == null) return;

        // Cú pháp: Tên_Event += Tên_Hàm_Xử_Lý
        controller.OnJumpPerformed += PlayJump;
        controller.OnLanded += PlayLand;
        controller.OnShootHand += PlayHand;
        controller.OnRunChanged += SetRunState;
        controller.OnWallGrabChanged += SetWallGrab;
        controller.OnClimbChanged += SetClimb;
        controller.OnGroundedChanged += SetGrounded;
        controller.OnLowStamina += HandleLowStamina;
    }

    // --- HỦY ĐĂNG KÝ (BẮT BUỘC ĐỂ TRÁNH LỖI MEMORY LEAK) ---
    private void OnDisable()
    {
        if (controller == null) return;

        // Cú pháp: Tên_Event -= Tên_Hàm_Xử_Lý (Ngược lại với OnEnable)
        controller.OnJumpPerformed -= PlayJump;
        controller.OnLanded -= PlayLand;
        controller.OnRunChanged -= SetRunState;
        controller.OnWallGrabChanged -= SetWallGrab;
        controller.OnClimbChanged -= SetClimb;
        controller.OnGroundedChanged -= SetGrounded;
        controller.OnLowStamina -= HandleLowStamina;
    }

    // --- CÁC HÀM XỬ LÝ (Sẽ được gọi tự động khi Event kích hoạt) ---

    private void PlayJump()
    {
        animator.SetTrigger(JumpTrig);
    }

    private void PlayLand()
    {
        // Có thể thêm code: Tạo hiệu ứng nén nhân vật (Squash), bụi tiếp đất
        SoundManager.Instance.PlaySound("Land");
        if (HapticManager.Instance != null)
        {
            // Rung chạm đất nhẹ nhàng: 
            // Motor trầm (Low): 0.1 | Motor gắt (High): 0.3 | Thời gian: 0.1 giây
            HapticManager.Instance.Rumble(0.1f, 0.3f, 0.1f);
        }
    }

    private void PlayHand()
    {
        SoundManager.Instance.PlaySound("Shoot Hand");
        HapticManager.Instance.Rumble(1f, 1f, 0.8f, 0f, 0.05f);
    }

    private void SetGrounded(bool isGrounded)
    {
        animator.SetBool(IsGrounded, isGrounded);
    }

    private void SetRunState(bool isRunning)
    {
        animator.SetBool(IsRunning, isRunning);
    }

    private void SetWallGrab(bool isGrabbing)
    {
        animator.SetBool(IsWallGrabbing, isGrabbing);
        // Khi bám tường thì chắc chắn không ở dưới đất
        if (isGrabbing) animator.SetBool(IsGrounded, false);
    }

    private void SetClimb(bool isClimbing)
    {
        animator.SetBool(IsClimbing, isClimbing);
    }

    // --- XỬ LÝ BÁO ĐỘNG THỂ LỰC ---
    private void HandleLowStamina(bool isLow)
    {
        if (isLow)
        {
            // Bắt đầu nhấp nháy nếu chưa chạy
            if (lowStaminaCoroutine == null)
            {
                lowStaminaCoroutine = StartCoroutine(LowStaminaRoutine());
            }
        }
        else
        {
            // Tắt nhấp nháy ngay lập tức khi an toàn
            if (lowStaminaCoroutine != null)
            {
                StopCoroutine(lowStaminaCoroutine);
                lowStaminaCoroutine = null;
            }
            // Đảm bảo trả lại màu trắng gốc cho nhân vật
            spriteRenderer.color = Color.white;
        }
    }

    // COROUTINE TẠO NHỊP ĐẬP CHỚP ĐỎ VÀ RUNG TAY CẦM
    private IEnumerator LowStaminaRoutine()
    {
        // Chạy vòng lặp vô tận cho đến khi bị StopCoroutine cắt ngang
        while (true)
        {
            // 1. NHỊP CĂNG: Đổi màu Đỏ + Rung gắt
            spriteRenderer.color = Color.red;
            if (HapticManager.Instance != null)
            {
                // Rung motor nhỏ (High frequency) rất gắt trong thời gian cực ngắn (0.1s)
                HapticManager.Instance.Rumble(0f, 0.4f, 0.1f);
            }
            yield return new WaitForSeconds(0.15f); // Thời gian hiển thị màu đỏ

            // 2. NHỊP NGHỈ: Trả về màu Trắng + Dừng rung
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.15f); // Thời gian chờ trước nhịp đập tiếp theo
        }
    }
}