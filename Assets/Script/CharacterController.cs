using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{
    public float rayDistance;
    // Các biến có thể điều chỉnh trong Unity Editor
    [Header("Stats")]
    [SerializeField] private float attackDamage = 40f;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed = 0.7f; // Attack speed = thời gian thực hiện animation attack
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;

    [Header("Gameplay attributes")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float gravity = 40f;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private float lastJumpInputTime;
    [SerializeField] private float lastChangeVelocityYTime;
    [SerializeField] private float lastVelocityX; //v0
    [SerializeField] private float lastVelocityY; //v0

    [Header("Components")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform topCircle;
    [SerializeField] private Transform bottomCircle;
    [SerializeField] private Transform frontCircle;
    [SerializeField] private Transform backCircle;
    [SerializeField] private Collider2D col; // collider của nhân vật
    [SerializeField] private Collider2D enemyHit;
    [SerializeField] private LayerMask enemyLayers;

    private Rigidbody2D rb;
    private Animator animator;

    [Header("Input system")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction dashAction;
    public InputAction attackAction;

    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isDashing;
    [SerializeField] private bool isHittingFrontWall;
    [SerializeField] private bool isHittingBackWall;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    [SerializeField] private bool isJumping;

    [Space]
    [SerializeField] private float velocityX;
    [SerializeField] private float velocityY;
    [SerializeField] private float acceleration;
    [SerializeField] private float jumpTime;
    public Vector2 moveVector;

    //Script
    [Header("Script")]
    [SerializeField] private RoomTranstionScript roomTranstionScript;

    // Lệnh này được gọi khi script được khởi tạo
    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        col = GetComponent<Collider2D>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;
    }

    // Lệnh này được gọi mỗi khung hình
    void FixedUpdate()
    {
        CheckGrounded();
        CheckWallHit();
        MoveCharacter();
    }

    private void MoveCharacter()
    {
        float lastXInput = moveInput.x;
        moveInput = moveAction.ReadValue<Vector2>();
        velocityX = moveInput.x * moveSpeed;

        if (isGrounded && !isJumping)
        {
            velocityY = 0;
        }
        else if (!isGrounded && !isJumping) velocityY = -gravity; //lastVelocityY + gravity * (Time.time - lastChangeVelocityYTime); // v = v0 + g*t

        if (isHittingFrontWall && velocityX != 0) velocityX = 0;
        //if(isHittingBackWall && velocityX != 0) velocityX = 0;

        if (moveInput.x != 0)
        {
            animator.SetBool("isRunning", true);

            if (moveInput.x > 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }

            if (moveInput.x < 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        if (moveInput.x == 0)
        {
            animator.SetBool("isRunning", false);
        }

        //if (!isDashing && !isAttacking) rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        moveVector = new Vector2(velocityX, velocityY);
        if (!isDashing && !isAttacking) rb.MovePosition(rb.position + moveVector * Time.fixedDeltaTime);
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
            velocityY = jumpForce;
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

    private void Attack(InputAction.CallbackContext context)
    {
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if(!isDashing) animator.SetTrigger("attack");
        if (isDashing) animator.SetTrigger("dashAttack");

        SoundManager.Instance.PlaySound2D("Attack");

        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in enemiesHit)
        {
            enemyHit = enemy;

            if (enemyHit != null)
            {
                Debug.Log("I hit: " + enemyHit.gameObject.name);
                enemyHit.GetComponent<Enemy>().TakeDamage(attackDamage);
            }

            if (enemyHit == null) break;
        }

        yield return new WaitForSeconds(attackSpeed);
        isAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log("Current Health: " + currentHealth);
        animator.SetTrigger("hurt");
        if (currentHealth < 0) Die();
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


    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

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

    void CheckGrounded()
    {
        bool hit = Physics2D.OverlapCircle(bottomCircle.position, 0.1f, LayerMask.GetMask("Ground"));
        if (hit)
        {
            isGrounded = true;
            lastGroundedTime = Time.time;

            // Reset velocity.y nếu đang đứng trên mặt đất
            //if (rb.linearVelocityY < 0f) rb.linearVelocityY = 0f;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded)
        {
            animator.SetBool("isGrounded", true);
        }

        else
        {
            animator.SetBool("isGrounded", false);
        }
    }

    void CheckWallHit()
    {
        //bool hitWall = isFacingRight ? Physics2D.Raycast(transform.position, Vector2.right, rayDistance, LayerMask.GetMask("Ground"))
        //    : Physics2D.Raycast(transform.position, Vector2.left, rayDistance, LayerMask.GetMask("Ground"));

        bool hitFrontWall = Physics2D.OverlapCircle(frontCircle.position, 0.1f, LayerMask.GetMask("Ground"));
        bool hitBackWall = Physics2D.OverlapCircle(backCircle.position, 0.1f, LayerMask.GetMask("Ground"));
        if (hitFrontWall)
        {
            animator.SetBool("isGrabbingWall", true);
            isHittingFrontWall = true;
        }

        else
        {
            animator.SetBool("isGrabbingWall", false);
            isHittingFrontWall = false;
        }

        if (hitBackWall)
        {
            animator.SetBool("isGrabbingWall", true);
            isHittingBackWall = true;
        }

        else
        {
            animator.SetBool("isGrabbingWall", false);
            isHittingBackWall = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.DrawWireSphere(topCircle.position, 0.1f);
        Gizmos.DrawWireSphere(bottomCircle.position, 0.1f);
        Gizmos.DrawWireSphere(backCircle.position, 0.1f);
        Gizmos.DrawWireSphere(frontCircle.position, 0.1f);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        attackAction.Enable();

        jumpAction.performed += OnJump;
        dashAction.performed += Dash;
        attackAction.performed += Attack;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
        attackAction.Disable();

        jumpAction.performed -= OnJump;
        dashAction.performed -= Dash;
        attackAction.performed -= Attack;
    }
}