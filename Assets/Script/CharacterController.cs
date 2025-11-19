using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public enum PlayerState
{
    Idle,
    Running,
    OnAir,
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

    private DialogueTrigger currentDialogueTrigger; // Biến tham chiếu đến NPC hiện tại
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

    public bool CanMove
    {
        get
        {
            return currentState == PlayerState.Idle ||
                   currentState == PlayerState.Running ||
                   currentState == PlayerState.OnAir;
        }
    }

    public bool CanJump
    {
        get
        {
            return currentState == PlayerState.Idle ||
                   currentState == PlayerState.Running;
        }
    }

    void Update()
    {
        if (GameManager.state == GameState.Pause) return;

        if (!CanMove)
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
        UpdatePhysicsState();
    }

    private void MoveCharacter()
    {
        if (!CanMove) return;
        //Gravity
        if (currentState == PlayerState.OnAir) velocity.y -= gravity * Time.fixedDeltaTime;
        else velocity.y = 0;

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
        if (!CanMove && currentState != PlayerState.Talking) return;

        if (context.performed)
        {
            // --- LOGIC MỚI: KIỂM TRA HỘI THOẠI ---

            // Nếu đang đứng yên (Idle) VÀ đang có NPC bên cạnh (currentDialogueTrigger != null)
            // Bạn có thể thêm điều kiện && isGrounded nếu muốn chắc chắn phải chạm đất mới nói được
            if (currentState == PlayerState.Idle && currentDialogueTrigger != null)
            {
                currentDialogueTrigger.TriggerDialogue();

                // Dừng lại ở đây, KHÔNG thực hiện code nhảy bên dưới
                return;
            }
            // -------------------------------------

            // Nếu không thỏa mãn điều kiện trên thì thực hiện nhảy bình thường
            lastJumpInputTime = Time.time;
        }
        // Do NOT call Jump() directly here — we process buffer in FixedUpdate after ground check.
    }

    private void ProcessJumpBuffer()
    {
        // If jump was pressed recently and player is within coyote time, perform jump
        if ((Time.time - lastGroundedTime <= coyoteTime) &&
            (Time.time - lastJumpInputTime <= jumpBufferTime) &&
            CanJump)
        {
            Jump();
            lastJumpInputTime = -999f;
        }
    }

    private void Jump()
    {
        currentState = PlayerState.OnAir;
        velocity.y = jumpForce;
        animator.SetTrigger("jump");
        SoundManager.Instance.PlaySound2D("Jump");
        
        lastGroundedTime = -999f;
        isGrounded = false;

    }

    /*private void Dash(InputAction.CallbackContext context)
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
    }*/

    private void Die()
    {
        gameObject.layer = 3;
        animator.SetBool("isDead", true);
    }

    private void UpdatePhysicsState()
    {
        // Nếu đang bận việc riêng (Talking/Dead) thì không can thiệp
        if (currentState == PlayerState.Talking || currentState == PlayerState.Dead) return;

        if (isGrounded && velocity.y <= 0.1f)
        {
            // Nếu chạm đất: quyết định xem là Running hay Idle
            if (moveInput.x != 0)
            {
                currentState = PlayerState.Running;
            }
            else
            {
                currentState = PlayerState.Idle;
            }
        }
        else
        {
            // Nếu không chạm đất -> OnAir
            currentState = PlayerState.OnAir;
        }
    }

    void VerticalCollisionCheck()
    {
        Vector2 boxSize = col.bounds.size;
        boxSize.x *= 0.9f; // Giảm kích thước hộp theo chiều ngang để tránh va chạm không cần thiết
        // Use OverlapBox for boolean ground check
        Collider2D ground = Physics2D.OverlapBox(col.bounds.center + Vector3.down * castDistance, boxSize, 0f, LayerMask.GetMask("Ground"));

        isGrounded = (ground != null);

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

    // Hàm để DialogueTrigger gọi khi người chơi đi vào vùng
    public void SetCurrentTrigger(DialogueTrigger trigger)
    {
        currentDialogueTrigger = trigger;
    }

    // Hàm để DialogueTrigger gọi khi người chơi đi ra vùng
    public void ClearCurrentTrigger(DialogueTrigger trigger)
    {
        // Chỉ xóa nếu cái đang lưu chính là cái vừa đi ra (tránh lỗi khi các vùng chồng nhau)
        if (currentDialogueTrigger == trigger)
        {
            currentDialogueTrigger = null;
        }
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

        //if (dashAction != null) dashAction.performed += Dash;
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (jumpAction != null) { jumpAction.performed -= OnJump; jumpAction.Disable(); }
        //if (dashAction != null) { dashAction.performed -= Dash; dashAction.Disable(); }
        if (attackAction != null) attackAction.Disable();
    }
}