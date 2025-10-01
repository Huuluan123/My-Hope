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
    Jumping,
    Falling,
    Attacking,
    Dashing,
    Hurt,
    Dead
}

public class PlayerController : MonoBehaviour
{
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
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        //Read move value
        moveInput = moveAction.ReadValue<Vector2>();
        if ((hitLeftWall && moveInput.x < 0) || (hitRightWall && moveInput.x > 0)) velocity.x = 0;
        else velocity.x = moveInput.x * moveSpeed;
    }

    void FixedUpdate()
    {
        VerticalCollisionCheck();
        HorizonalCollisionCheck();
        MoveCharacter();
    }

    private void MoveCharacter()
    {
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

    // Phương thức xử lý nhảy
    private void OnJump(InputAction.CallbackContext context)
    {
        lastJumpInputTime = Time.time;
        Jump();
        Invoke("EndJump", jumpTime);
    }

    private void Jump()
    {
        if ((Time.time - lastGroundedTime <= coyoteTime) &&
        (Time.time - lastJumpInputTime <= jumpBufferTime) &&
        isGrounded)
        {
            isJumping = true;
            velocity.y = jumpForce;
            animator.SetTrigger("jump");
            SoundManager.Instance.PlaySound2D("Jump");
            lastJumpInputTime = -999f;

        }
    }

    private void EndJump()
    {
        isJumping = false;
    }

    private void Dash(InputAction.CallbackContext context)
    {
        if (!isDashing)
        {
            isDashing = true;
            animator.SetTrigger("dash");
            float dashDirection =  transform.localScale.x;
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
        isGrounded = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, castDistance,
                   LayerMask.GetMask("Ground"));

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            animator.SetBool("isGrounded", true);

            // Reset velocity.y nếu đang đứng trên mặt đất
            //if (rb.linearVelocityY < 0f) rb.linearVelocityY = 0f;
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
        hitRightWall = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.right, castDistance,
                            LayerMask.GetMask("Ground"));
        hitLeftWall = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.left, castDistance,
                           LayerMask.GetMask("Ground"));
        if (hitRightWall)
        {
            //animator.SetBool("isGrabbingWall", true);
        }

        else
        {
            //animator.SetBool("isGrabbingWall", false);
        }

        if (hitRightWall)
        {
            //animator.SetBool("isGrabbingWall", true);
        }

        else
        {
            //animator.SetBool("isGrabbingWall", false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Vector2 boxSize = col.bounds.size;
        boxSize.x *= 0.9f;
        Gizmos.DrawWireCube(col.bounds.center + Vector3.down * castDistance, boxSize);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        attackAction.Enable();

        jumpAction.performed += OnJump;
        dashAction.performed += Dash;
    }   

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
        attackAction.Disable();

        jumpAction.performed -= OnJump;
        dashAction.performed -= Dash;
    }
}