using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerController controller;

    // Các ID Animation (Hash để tối ưu hiệu năng)
    private static readonly int JumpTrig = Animator.StringToHash("jump");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsWallGrabbing = Animator.StringToHash("isWallGrabbing");
    private static readonly int IsClimbing = Animator.StringToHash("isClimbing");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
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
    }

    // --- CÁC HÀM XỬ LÝ (Sẽ được gọi tự động khi Event kích hoạt) ---

    private void PlayJump()
    {
        animator.SetTrigger(JumpTrig);
        // Có thể thêm code: Tạo bụi nhảy, phát âm thanh nhảy ở đây
        SoundManager.Instance.PlaySound("Jump");
    }

    private void PlayLand()
    {
        // Có thể thêm code: Tạo hiệu ứng nén nhân vật (Squash), bụi tiếp đất
        // SoundManager.Instance.PlaySound("Land");
    }

    private void PlayHand()
    {
        SoundManager.Instance.PlaySound("Shoot Hand");
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
}