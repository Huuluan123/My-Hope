using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public enum PlayerState
{
    Moving,
    Talking,
    Dead
}

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public static PlayerController Instance; // Biến Singleton

    // Các biến có thể điều chỉnh trong Unity Editor
    [Header("Stats")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float dashDistance = 10f;

    [Header("Gameplay attributes")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float gravity = 40f;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private float lastJumpInputTime;
    [SerializeField] private float castDistance = 0.01f;

    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private PlayerState currentState;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isDashing;
    [SerializeField] private bool hitLeftWall;
    [SerializeField] private bool hitRightWall;
    [SerializeField] private bool hitCelling;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    [SerializeField] private bool isJumping;

    [Space]
    [SerializeField] private Vector2 velocity;
    [SerializeField] private float acceleration;
    [SerializeField] private float jumpTime;

    [Header("Components")]
    [SerializeField] private Collider2D col; // collider của nhân vật
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private RaycastHit2D[] hits = new RaycastHit2D[4]; // Mảng để lưu trữ kết quả quét va chạm

    private Rigidbody2D rb;
    private Animator animator;

    [Header("Input system")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction dashAction;
    public InputAction attackAction;

    // Lệnh này được gọi khi script được khởi tạo
    void Awake()
    {
        if (Instance == null) Instance = this;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    //Set trạng thái của người chơi
    public void SetState(PlayerState newState)
    {
        currentState = newState;
    }

    void Update()
    {
        if (GameManager.state == GameState.Pause) return;

        if (currentState != PlayerState.Moving)
        {
            moveInput = Vector2.zero; // Xóa input di chuyển
            velocity.x = 0;           // Dừng di chuyển ngang
            animator.SetBool("isRunning", false); // Tắt animation chạy
            return; // Bỏ qua phần còn lại của Update
        }

        //Read move value
        if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();
        if ((hitLeftWall && moveInput.x < 0) || (hitRightWall && moveInput.x > 0)) velocity.x = 0;
        else velocity.x = moveInput.x * moveSpeed;
    }

    void FixedUpdate()
    {
        VerticalCollisionCheck();
        HorizonalCollisionCheck();

        // Process jump buffer & coyote here (FixedUpdate) so ground checks are up-to-date
        ProcessJumpBuffer();

        MoveCharacter();
    }

    private void MoveCharacter()
    {
        if (currentState != PlayerState.Moving) return;
        //Gravity
        if (!isGrounded) velocity.y -= gravity * Time.fixedDeltaTime;
        else if (!isJumping) velocity.y = 0;

        //Calculate new position
        Vector2 moveDelta = velocity * Time.fixedDeltaTime;
        Vector2 targetPosition = rb.position + moveDelta;

        // Flip character
        if (moveInput.x != 0)
        {
            animator.SetBool("isRunning", true);

            if (moveInput.x > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            if (moveInput.x < 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        if (moveInput.x == 0)
        {
            animator.SetBool("isRunning", false);
        }

        if (!isDashing) rb.MovePosition(targetPosition);
    }

    // Phương thức xử lý nhảy input: chỉ lưu thời điểm ấn để dùng jump buffer
    private void OnJump(InputAction.CallbackContext context)
    {
        if (currentState != PlayerState.Moving) return;
        lastJumpInputTime = Time.time;
        // Do NOT call Jump() directly here — we process buffer in FixedUpdate after ground check.
    }

    private void ProcessJumpBuffer()
    {
        // If jump was pressed recently and player is within coyote time, perform jump
        if ((Time.time - lastGroundedTime <= coyoteTime) &&
            (Time.time - lastJumpInputTime <= jumpBufferTime) &&
            isGrounded && !isJumping)
        {
            Jump();
            lastJumpInputTime = -999f;
        }
    }

    private void Jump()
    {
        isJumping = true;
        velocity.y = jumpForce;
        animator.SetTrigger("jump");
        SoundManager.Instance.PlaySound2D("Jump");
        // schedule end of jump state
        CancelInvoke(nameof(EndJump));
        Invoke(nameof(EndJump), jumpTime);
    }

    private void EndJump()
    {
        isJumping = false;
    }

    private void Dash(InputAction.CallbackContext context)
    {
        if (currentState != PlayerState.Moving) return;
        if (!isDashing)
        {
            isDashing = true;
            animator.SetTrigger("dash");
            float dashDirection = transform.localScale.x;
            gameObject.layer = 3; // Invincible Layer
            rb.AddForce(new Vector2(dashDistance * dashDirection, 0), ForceMode2D.Impulse);
            Invoke("EndDash", 0.3f);
        }
    }

    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        gameObject.layer = 8; // Player Layer
    }

    private void Die()
    {
        gameObject.layer = 3;
        animator.SetBool("isDead", true);
    }

    // Lệnh này để kiểm tra va chạm với mặt đất
    /*void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Touch ground!");
            isGrounded = true;
            animator.SetBool("isGrounded", true);
        }
    }*/

    /*void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            animator.SetBool("isGrounded", false);
        }
    }*/

    void IsGroundedCheck() // Dùng raycast để kiểm tra xem nhân vật có chạm đất không
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        if (isGrounded)
        {
            animator.SetBool("isGrounded", true);
        }

        else
        {
            animator.SetBool("isGrounded", false);
        }
    }

    /*private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            animator.SetBool("isGrounded", false);
        }
    }*/

    void VerticalCollisionCheck()
    {
        Vector2 boxSize = col.bounds.size;
        boxSize.x *= 0.9f; // Giảm kích thước hộp theo chiều ngang để tránh va chạm không cần thiết
        // Use OverlapBox for boolean ground check
        Collider2D ground = Physics2D.OverlapBox(col.bounds.center + Vector3.down * castDistance, boxSize, 0f, LayerMask.GetMask("Ground"));
        isGrounded = ground != null;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            animator.SetBool("isGrounded", true);
        }
        else
        {
            animator.SetBool("isGrounded", false);
        }
    }

    void HorizonalCollisionCheck()
    {
        Vector2 boxSize = col.bounds.size;
        boxSize.y *= 0.9f; // Giảm kích thước hộp theo chiều ngang để tránh va chạm không cần thiết
        bool right = Physics2D.OverlapBox(col.bounds.center + Vector3.right * castDistance, boxSize, 0f, LayerMask.GetMask("Ground"));
        bool left = Physics2D.OverlapBox(col.bounds.center + Vector3.left * castDistance, boxSize, 0f, LayerMask.GetMask("Ground"));

        hitRightWall = right;
        hitLeftWall = left;

        animator.SetBool("hitWall", hitLeftWall || hitRightWall);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        if (col == null) return;
        Vector2 boxSize = col.bounds.size;
        boxSize.x *= 0.9f;
        Gizmos.DrawWireCube(col.bounds.center + Vector3.down * castDistance, boxSize);
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (jumpAction != null) { jumpAction.Enable(); jumpAction.performed += OnJump; }
        if (dashAction != null) dashAction.Enable();
        if (attackAction != null) attackAction.Enable();

        if (dashAction != null) dashAction.performed += Dash;
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (jumpAction != null) { jumpAction.performed -= OnJump; jumpAction.Disable(); }
        if (dashAction != null) { dashAction.performed -= Dash; dashAction.Disable(); }
        if (attackAction != null) attackAction.Disable();
    }
}