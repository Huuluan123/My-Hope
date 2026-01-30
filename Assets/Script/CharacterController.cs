using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System;

public enum PlayerState
{
    Idle,
    Running,
    OnAir,
    Sliding,
    Climbing,
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

    [Header("Gameplay attributes")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private float lastJumpInputTime;
    [SerializeField] private const float skinWidth = 0.015f;
    [SerializeField] private int verticalRayCount = 8;
    [SerializeField] private int horizontalRayCount = 8;
    float verticalRaySpacing;
    float horizontalRaySpacing;

    [Header("Wall mechanics")]
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isWallClimbing;
    private int wallDirX; // Hướng tường (1: bên phải, -1: bên trái)
    [SerializeField] private float wallSlideSpeed = 2f; // Tốc độ trượt xuống chậm
    [SerializeField] private float wallClimbSpeed = 4f; // Tốc độ leo lên/xuống
    [SerializeField] private Vector2 wallJumpForce = new Vector2(15f, 25f); // Lực nhảy tường (X, Y)
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float climbStaminaDrain = 20f; // Tốn thể lực khi leo
    [SerializeField] private float holdStaminaDrain = 5f;   // Tốn ít hơn khi chỉ bám yên
    [SerializeField] private float currentStamina;

    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private PlayerState currentState;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    private bool hitLeftWall;
    private bool hitRightWall;

    [Space]
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float acceleration;
    [SerializeField] private float jumpTime;

    [Header("Components")]
    private Collider2D col; // collider của nhân vật
    [SerializeField] private LayerMask collisionMask;
    private RayCastOrigin rayCastOrigins;
    private RaycastHit2D[] hits = new RaycastHit2D[4]; // Mảng để lưu trữ kết quả quét va chạm

    private DialogueTrigger currentDialogueTrigger; // Biến tham chiếu đến NPC hiện tại
    private Rigidbody2D rb;

    private CharacterInput input;

    /* ---------- Events -------------- */
    public event Action OnJumpPerformed;
    public event Action OnLanded;
    public event Action<bool> OnRunChanged;
    public event Action<bool> OnWallGrabChanged;
    public event Action<bool> OnClimbChanged;
    public event Action<bool> OnGroundedChanged;

    #region Unity Callbacks
    void Awake()
    {
        if (Instance == null) Instance = this;

        // Lấy hoặc thêm component CharacterInput
        input = GetComponent<CharacterInput>();
        if (input == null) input = gameObject.AddComponent<CharacterInput>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        CalculateRaySpacing();
    }

    void Update()
    {
        if (GameManager.state == GameState.Pause) return;

        if (!CanMove)
        {
            moveInput = Vector2.zero;
            velocity.x = 0;
            return;
        }

        // Read move value from CharacterInput
        moveInput = (input != null) ? input.MoveInput : Vector2.zero;

        velocity.x = moveInput.x * moveSpeed;
    }

    void FixedUpdate()
    {
        // Process jump buffer & coyote here (FixedUpdate) so ground checks are up-to-date
        ProcessJumpBuffer();

        MoveCharacter();
        ResolveOverlap();
        UpdatePhysicsState();
        HandleAnimationEvents();
    }
    #endregion

    #region Movement Handling

    private void MoveCharacter()
    {
        if (!CanMove) return;

        hitLeftWall = false;
        hitRightWall = false;
        isWallSliding = false;

        if (currentState == PlayerState.Climbing)
        {
            HandleWallClimbingPhysics();
        }
        else
        {
            if (velocity.y > maxFallSpeed) velocity.y += gravity * Time.fixedDeltaTime;
        }

        UpdateRayCastOrigin();
        if (velocity.x != 0) HorizonalCollisionCheck();

        if (isWallSliding && !isWallClimbing)
        {
            velocity.y = -wallSlideSpeed;
        }

        UpdateRayCastOriginWithOffset(velocity.x * Time.fixedDeltaTime); // Cập nhật vị trí tia với offset di chuyển ngang
        if (velocity.y != 0) VerticalCollisionCheck();

        //Calculate new position
        Vector3 moveAmount = velocity * Time.fixedDeltaTime;
        Vector3 targetPosition = (Vector3)rb.position + moveAmount;
        rb.MovePosition(targetPosition);

        // Flip character
        if (moveInput.x != 0)
        {

            if (moveInput.x > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            if (moveInput.x < 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
    }

    // Phương thức xử lý nhảy input: chỉ lưu thời điểm ấn để dùng jump buffer
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!CanMove && currentState != PlayerState.Talking) return;

        if (context.performed)
        {
            // Check if can talk to NPC
            if (currentState == PlayerState.Idle && currentDialogueTrigger != null)
            {
                currentDialogueTrigger.TriggerDialogue();

                // Dừng lại ở đây, KHÔNG thực hiện code nhảy bên dưới
                return;
            }

            lastJumpInputTime = Time.time;
        }
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
        // Wall Jump
        if (isWallSliding || isWallClimbing)
        {
            // Nhảy bật ngược hướng tường
            // wallDirX là 1 (phải) -> nhảy sang trái (-1)
            // wallDirX là -1 (trái) -> nhảy sang phải (1)
            velocity.x = -wallDirX * wallJumpForce.x;
            velocity.y = wallJumpForce.y;

            // Giảm Stamina một chút khi Wall Jump (tùy chọn)
            currentStamina -= 10f;
        }
        else
        // ---------------------------------
        {
            // Nhảy thường (Normal Jump)
            velocity.y = jumpForce;
        }
        currentState = PlayerState.OnAir;
        velocity.y = jumpForce;
        SoundManager.Instance.PlaySound2D("Jump");

        lastGroundedTime = -999f;
        isGrounded = false;
        isWallClimbing = false; // Ngắt trạng thái leo ngay khi nhảy

        OnJumpPerformed?.Invoke();
    }

    private void HandleWallClimbingPhysics() // Gọi trong MoveCharacter()
    {
        // 1. Áp dụng vận tốc leo dựa trên input Y
        velocity.y = moveInput.y * wallClimbSpeed;

        // 2. Trừ thể lực
        float drain = (moveInput.y != 0) ? climbStaminaDrain : holdStaminaDrain;
        currentStamina -= drain * Time.fixedDeltaTime;

        // 3. Nếu hết thể lực, tự động buông tay
        if (currentStamina <= 0)
        {
            currentStamina = 0;
            currentState = PlayerState.OnAir; // Chuyển về trạng thái rơi
            isWallClimbing = false;
        }
    }

    private void CheckWallGrab() // Gọi trong FixedUpdate() hoặc UpdatePhysicsState()
    {
        // Điều kiện: Đang giữ phím Grab + Chạm tường + Không ở mặt đất + Còn thể lực
        bool isTouchingWall = hitLeftWall || hitRightWall;

        if (input.IsGrabbing && isTouchingWall && !isGrounded && currentStamina > 0)
        {
            currentState = PlayerState.Climbing;
            isWallClimbing = true;
            isWallSliding = false; // Ưu tiên leo hơn trượt
        }
        else
        {
            isWallClimbing = false;
            // Nếu không leo, trạng thái sẽ do UpdatePhysicsState quyết định (OnAir/Sliding...)
        }
    }

    #endregion

    #region State Machine
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
                   currentState == PlayerState.OnAir ||
                   currentState == PlayerState.Sliding ||
                   currentState == PlayerState.Climbing;
        }
    }

    public bool CanJump
    {
        get
        {
            return currentState == PlayerState.Idle ||
                   currentState == PlayerState.Sliding ||
                   currentState == PlayerState.Climbing ||
                   currentState == PlayerState.Running;
        }
    }

    private void UpdatePhysicsState()
    {
        // Nếu đang bận việc riêng (Talking/Dead) thì không can thiệp
        if (currentState == PlayerState.Talking || currentState == PlayerState.Dead) return;

        if (isGrounded)
        {
            currentStamina = maxStamina;
        }

        CheckWallGrab(); // Kiểm tra có đang leo tưởng hay không
        if (isWallClimbing) return; // Nếu đang leo thì không chạy logic bên dưới

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
            // Cập nhật trạng thái trượt tường cho Animation
            if (isWallSliding)
            {
                currentState = PlayerState.Sliding;
            }
            else
            {
                currentState = PlayerState.OnAir;
            }
        }
    }

    private void HandleAnimationEvents()
    {
        // 1. Xử lý Event Bám tường (Sliding hoặc Climbing đều là đang bám)
        bool isGrabbing = (currentState == PlayerState.Sliding || currentState == PlayerState.Climbing);
        OnWallGrabChanged?.Invoke(isGrabbing);

        // 2. Xử lý Event Leo trèo (Chỉ khi đang Climbing VÀ có di chuyển tay)
        // Lưu ý: Dùng Mathf.Abs > 0.1f để tránh lỗi rung tay cầm (stick drift) nếu có
        bool isClimbing = (currentState == PlayerState.Climbing && Mathf.Abs(moveInput.y) > 0.1f);
        OnClimbChanged?.Invoke(isClimbing);

        // 3. Xử lý Event Chạy và Cập nhật trạng thái chạm đất cho Animation
        bool isRunning = (currentState == PlayerState.Running);
        OnRunChanged?.Invoke(isRunning);
        OnGroundedChanged?.Invoke(isGrounded);
    }
    #endregion

    #region Collision Handling
    void VerticalCollisionCheck()
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + skinWidth;

        if (directionY == -1) isGrounded = false;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;

            rayOrigin += Vector2.right * (verticalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                // Cho phép nhảy xuyên qua (One Way Platform) nếu cần - Logic đơn giản
                // if (hit.collider.CompareTag("OneWay") && directionY == 1) continue;

                velocity.y = (hit.distance - skinWidth) * directionY / Time.fixedDeltaTime; // Tính lại vận tốc thực dựa trên quãng đường còn lại
                rayLength = hit.distance; // Thu ngắn tia lại để không check xuyên qua vật cản gần nhất

                if (directionY == -1) // Đang rơi xuống
                {
                    if (!isGrounded) OnLanded?.Invoke();
                    isGrounded = true;
                    lastGroundedTime = Time.time;
                }
                else // Đang nhảy lên đụng trần
                {
                    velocity.y = 0; // Tùy chọn: Đụng trần thì dừng hẳn lực nảy
                }
            }
        }
    }

    void HorizonalCollisionCheck()
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x * Time.fixedDeltaTime) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight;

            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                // Nếu khoảng cách quá gần (bằng 0), nghĩa là đang kẹt trong tường -> bỏ qua
                if (hit.distance == 0) continue;

                //float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                velocity.x = (hit.distance - skinWidth) * directionX / Time.fixedDeltaTime;
                rayLength = hit.distance;

                //Phát hiện tường
                wallDirX = (directionX == 1) ? 1 : -1;
                // Trạng thái trượt tường: Đang chạm tường, đang rơi và người chơi đang ấn phím hướng vào tường
                bool isPressingAgainstWall = (moveInput.x > 0 && wallDirX == 1) || (moveInput.x < 0 && wallDirX == -1);

                if (!isGrounded && velocity.y < 0 && isPressingAgainstWall)
                {
                    isWallSliding = true;
                    // Thêm dòng này để đảm bảo vận tốc trượt được áp dụng ngay lập tức trong frame này nếu cần
                    // (Tùy chọn, nhưng giúp mượt hơn)
                    //if (velocity.y < -wallSlideSpeed)
                    //{
                    //    velocity.y = -wallSlideSpeed;
                    //}
                }

                // Cập nhật biến va chạm
                if (directionX == -1) hitLeftWall = true;
                if (directionX == 1) hitRightWall = true;
            }
        }
    }

    // Hàm để giải quyết việc nhân vật bị lồng vào vật thể
    void ResolveOverlap()
    {
        Collider2D[] results = new Collider2D[5];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(collisionMask);
        filter.useTriggers = false;

        int count = col.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            ColliderDistance2D diff = col.Distance(results[i]);
            if (diff.isOverlapped)
            {
                float overlapThreshold = skinWidth + 0.01f;

                if (Mathf.Abs(diff.distance) > overlapThreshold)
                {
                    // Chỉ đẩy phần dư ra ngoài skinWidth
                    float errorCorrection = Mathf.Abs(diff.distance) - skinWidth;
                    Vector2 separationVector = diff.normal * -errorCorrection;

                    // Nếu là tường đứng (Normal chủ yếu theo phương ngang), khóa trục Y lại
                    if (Mathf.Abs(diff.normal.y) < 0.5f)
                    {
                        separationVector.y = 0;
                    }

                    transform.position += (Vector3)separationVector;
                    rb.position = (Vector2)transform.position;
                }
            }
        }
    }
    #endregion

    #region Raycast Origin Setup
    void UpdateRayCastOrigin()
    {
        Bounds bounds = col.bounds;
        // Quan trọng: Thu nhỏ bounds lại để tia bắn từ bên trong cơ thể ra ngoài
        bounds.Expand(skinWidth * -2);

        rayCastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        rayCastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        rayCastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        rayCastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
    }

    void UpdateRayCastOriginWithOffset(float offsetX)
    {
        UpdateRayCastOrigin(); // Cập nhật lại vị trí hiện tại trước
        rayCastOrigins.bottomLeft += Vector2.right * offsetX;
        rayCastOrigins.bottomRight += Vector2.right * offsetX;
        rayCastOrigins.topLeft += Vector2.right * offsetX;
        rayCastOrigins.topRight += Vector2.right * offsetX;
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        // Clamp đúng biến cho từng loại
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue); // Đã sửa từ horizontalRayCount thành verticalRayCount

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
    #endregion

    #region Dialouge Handling
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
    #endregion

    #region Callback Fuctions
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
    }

    private void OnEnable()
    {
        // Đăng ký với CharacterInput
        if (input != null) input.JumpPerformed += OnJump;
    }

    private void OnDisable()
    {
        if (input != null) input.JumpPerformed -= OnJump;
    }
    #endregion

    #region structs
    struct RayCastOrigin
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
    #endregion
}