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

    /* Nhóm khoá input */
    GrappleFreeze,
    Grappling,

    Talking,
    Dead,
    ScriptedMove
}

public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Components")]
    public static PlayerController Instance; // Biến Singleton

    // Các biến có thể điều chỉnh trong Unity Editor
    [Header("Movement stats")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField][Range(0f, 1f)] private float variableJumpHeightMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private float lastJumpInputTime;
    [SerializeField] private float acceleration = 50f; // Gia tốc tăng (Càng lớn càng nhanh đạt tốc độ tối đa)
    [SerializeField] private float deceleration = 50f; // Gia tốc giảm (Càng lớn càng phanh gấp)
    [SerializeField] private float airDeceleration = 40f; // Ma sát trên không (Thấp hơn để bay lượn mượt hơn)

    [Header("Jump Settings")]
    [SerializeField] private const float skinWidth = 0.015f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private int verticalRayCount = 8;
    [SerializeField] private int horizontalRayCount = 8;
    float verticalRaySpacing;
    float horizontalRaySpacing;

    [Header("Wall mechanics")]
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isWallClimbing;
    [SerializeField] private float wallJumpInputLockTime = 0.15f; // Thời gian cấm người chơi cản trở quán tính nhảy tường
    private int wallDirX; // Hướng tường (1: bên phải, -1: bên trái)
    private float wallJumpInputTimer; // Timer đếm ngược để khóa input sau khi nhảy tường

    [SerializeField] private float wallCoyoteTime = 0.1f;
    private float lastWallTouchTime = -999f; // Ghi nhớ thời điểm cuối cùng chạm tường
    private int lastWallDirX; // Ghi nhớ hướng của bức tường cuối cùng

    [Space]

    [SerializeField] private float wallSlideSpeed = 2f; // Tốc độ trượt xuống chậm
    [SerializeField] private float wallClimbSpeed = 4f; // Tốc độ leo lên/xuống
    [SerializeField] private float wallClimbDownSpeed = 8f; // Tốc độ trượt tường nhanh khi bám
    [SerializeField] private float ledgeSlipSpeed = 8f; // Tốc độ tụt xuống khi đỉnh đầu không chạm tường
    [SerializeField] private Vector2 wallJumpForce = new Vector2(15f, 25f); // Lực nhảy tường (X, Y)
    [SerializeField] private float wallClimbJumpForceY = 20f; // Nhảy leo thẳng lên (Chỉ cần lực Y)
    private float climbJumpCooldownTimer;

    [Space]
    // Neutreal Wall Jump
    [SerializeField] private Vector2 neutralWallJumpForce = new Vector2(5f, 22f); // Lực đẩy ngang rất nhỏ, lực dọc lớn
    [SerializeField] private float neutralJumpStaminaCost = 10f; // Hình phạt thể lực
    [SerializeField] private float neutralJumpInputLockTime = 0.1f; // Khóa input ngắn hơn nhảy tường thường

    [Space]

    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float climbStaminaDrain = 20f; // Tốn thể lực khi leo
    [SerializeField] private float holdStaminaDrain = 5f;   // Tốn ít hơn khi chỉ bám yên
    [SerializeField] private float currentStamina;
    [SerializeField] private float lowStaminaThreshold = 25f; // Dưới mức này sẽ báo động
    private bool isLowStaminaState; // Biến nhớ để không spam event liên tục

    [Header("Grappling Hand")]
    [SerializeField] private RobotHand robotHand;
    [SerializeField] private float pullSpeed = 30f;
    [SerializeField] private float grappleInputWindow = 0.05f; // Cửa sổ chờ Input (0.05s)
    private Vector2 grappleTarget;

    [Header("Climb hop Settings")]
    [SerializeField] private Vector2 climbHopForce = new Vector2(10f, 15f); // Lực đẩy lên sàn (X nhỏ, Y lớn)
    [SerializeField] private float climbHopNoInputTime = 0.2f; // Thời gian khóa Input ngang để nhân vật lọt vào sàn
    private float climbHopInputTimer; // Timer đếm ngược
    private bool isWaitingForHopX; // Cờ đánh dấu đang chờ bung lực ngang
    private int hopDirX; // Hướng đẩy vào sàn

    [Header("Corner Correction")]
    [SerializeField] private float cornerCorrectionSpeed = 0.1f; // Khoảng cách đẩy lên mỗi frame (Pixel perfect)
    [SerializeField] private float ceilingTolerance = 0.15f; // Độ rộng tối đa của góc có thể lách qua (Tùy theo chiều rộng nhân vật)
    [SerializeField] private float ceilingNudgeAmount = 0.2f;

    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private float diagonalThreshold = 0.35f;
    [SerializeField] private PlayerState currentState;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    private bool wasGrounded;
    [SerializeField] private bool hitLeftWall;
    [SerializeField] private bool hitRightWall;
    private bool canClimbCurrentWall;

    [Space]
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float jumpTime;

    public Vector2 Velocity
    {
        get { return velocity; }
    }

    [Header("Ability Unlocks & Counters")]
    [Tooltip("Maximum number of times to shoot grappling hand while on air")]
    public int maxGrappleCount = 2;
    private int currentGrappleCount = 0;

    [Tooltip("Number of air jumps (0 = Normal jump, 1 = Double Jump, 2 = Triple Jump)")]
    public int maxAirJumps = 0;
    private int currentAirJumps = 0;

    [Header("Moving Platform Handle")]
    private Transform currentMovingPlatform;
    private Vector3 lastPlatformPosition;

    [Header("Respawn Settings")]
    [SerializeField] private Vector2 currentRespawnPoint;
    //[SerializeField] private float respawnDelay = 1f;

    [Header("Cutscene Settings")]
    public float autoWalkSpeed = 4f;

    [Header("Components")]
    private Collider2D col; // collider của nhân vật
    [SerializeField] private LayerMask collisionMask;
    private RayCastOrigin rayCastOrigins;
    private RaycastHit2D[] hits = new RaycastHit2D[4]; // Mảng để lưu trữ kết quả quét va chạm

    private DialogueTrigger currentDialogueTrigger; // Biến tham chiếu đến NPC hiện tại
    private Rigidbody2D rb;

    private CharacterInput input;

    [Header("Failsafe Mechanics")]
    [SerializeField] private float stuckTimeout = 3f;
    [SerializeField] private float mapBottom = -50f;
    private float currentStuckTimer = 0f;
    private Vector3 lastSafePosition;

    /* ---------- Events -------------- */
    public event Action OnJumpPerformed;
    public event Action OnLanded;
    public event Action OnShootHand;
    public event Action<bool> OnRunChanged;
    public event Action<bool> OnWallGrabChanged;
    public event Action<bool> OnClimbChanged;
    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnLowStamina; // Truyền true khi báo động, false khi an toàn
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(Instance);
            return;
        }

        // Lấy hoặc thêm component CharacterInput
        input = GetComponent<CharacterInput>();
        if (input == null) input = gameObject.AddComponent<CharacterInput>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        // Not using Unity's physics for movement, we'll handle it manually
        rb.bodyType = RigidbodyType2D.Kinematic; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        CalculateRaySpacing();
    }

    void Update()
    {
        if (GameManager.Instance.State == GameState.Pause) return;

        // Chống rơi ra ngoài bản đồ (Failsafe)
        if (transform.position.y < mapBottom)
        {
            //transform.position = lastSafePosition;
            //rb.position = transform.position;
            Debug.LogWarning("Player has fallen out of the map! Activating failsafe reset.");
            EmergencyReset();
            return;
        }

        bool isVulnerableState = (currentState == PlayerState.Grappling ||
                                  currentState == PlayerState.GrappleFreeze);

        if (isVulnerableState)
        {
            currentStuckTimer += Time.deltaTime;
            if (currentStuckTimer >= stuckTimeout)
            {
                Debug.LogWarning($"[Watchdog] Stucking at {currentState} for more than {stuckTimeout} seconds. Activating emergency reset!");
                EmergencyReset();
            }
        }
        else
        {
            currentStuckTimer = 0f; // Các trạng thái khác (Idle, Talking, Cutscene...) đều an toàn -> Reset
        }

        if (climbHopInputTimer > 0)
        {
            climbHopInputTimer -= Time.deltaTime;
        }

        // Giảm timer cấm bám tường (nếu lớn hơn 0)
        if (climbJumpCooldownTimer > 0)
        {
            climbJumpCooldownTimer -= Time.deltaTime;
        }

        if (wallJumpInputTimer > 0)
        {
            wallJumpInputTimer -= Time.deltaTime;
        }

        if (IsInputDisabled)
        {
            moveInput = Vector2.zero;
        }

        else
        {
            // Nếu được phép điều khiển:
            // Chuẩn hoá Input để chỉ còn -1, 0, hoặc 1 ở mỗi trục (D-pad hoặc Analog nhẹ cũng chỉ tính là 1)
            Vector2 rawInput = (input != null) ? input.MoveInput : Vector2.zero;

            moveInput = Apply8WaySnap(rawInput);

            if (CanMove)
                CalculateHorizontalMovement();
            else
                velocity.x = 0;
        }
    }

    void FixedUpdate()
    {
        wasGrounded = isGrounded;

        // Process jump buffer & coyote here (FixedUpdate) so ground checks are up-to-date
        ProcessJumpBuffer();

        MoveCharacter();
        ResolveOverlap();
        UpdatePhysicsState();
        HandleAnimationEvents();
    }
    #endregion

    #region Movement Handling

    private Vector2 Apply8WaySnap(Vector2 rawInput)
    {
        float snapX = 0;
        float snapY = 0;

        // Trục X: Chỉ ghi nhận nếu người chơi đẩy MẠNH hơn ngưỡng chéo
        if (Mathf.Abs(rawInput.x) > diagonalThreshold)
        {
            snapX = Mathf.Sign(rawInput.x);
        }

        // Trục Y: Bỏ qua các rung động nhỏ của ngón tay (ví dụ y = 0.15 sẽ bị bỏ qua)
        if (Mathf.Abs(rawInput.y) > diagonalThreshold)
        {
            snapY = Mathf.Sign(rawInput.y);
        }

        // Rơi vào 1 trong 8 hướng chuẩn mực: (1,0), (1,1), (0,1)...
        return new Vector2(snapX, snapY);
    }

    private void MoveCharacter()
    {
        // Xử lí đóng băng chờ Input khi phóng tay móc (Grapple)
        if (currentState == PlayerState.GrappleFreeze)
        {
            velocity = Vector3.zero;
            return; // Đứng yên tuyệt đối trên không
        }

        // Xử lí kéo tới mục tiêu khi đang bị móc (Grappling)
        if (currentState == PlayerState.Grappling)
        {
            velocity = Vector3.zero; // Không dùng vận tốc rớt/di chuyển thông thường

            if (grappleTarget != Vector2.zero)
            {
                // Dịch chuyển nhân vật lao vút tới mục tiêu
                transform.position = Vector2.MoveTowards(transform.position, grappleTarget, pullSpeed * Time.fixedDeltaTime);
                rb.position = transform.position;

                // Nếu chạm tay
                if (Vector2.Distance(transform.position, grappleTarget) < 0.6f)
                {
                    // Đẩy người chơi về phía tường để bám
                    transform.position = new Vector3(transform.position.x + (wallDirX * 0.3f), transform.position.y, transform.position.z);
                    rb.position = transform.position;
                    // Chuyển ngay sang bám tường
                    currentState = PlayerState.OnAir;
                    //isWallClimbing = true;
                    //isWallSliding = false;
                    isGrounded = false;

                    grappleTarget = Vector2.zero; // Xóa mục tiêu
                    if (robotHand != null) robotHand.FinishGrapple(); // Gọi tay thu hồi lại
                }
            }
            return; // Dừng hàm, KHÔNG chạy các code trọng lực và Raycast bên dưới
        }

        if (!CanMove) return;

        UpdateRayCastOrigin();

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

        HorizonalCollisionCheck();

        //if (!CorrectCorner(velocity))
        //{
        //    HorizonalCollisionCheck();
        //}

        //else Debug.Log("Corner corrected!");

        if (isWallSliding && !isWallClimbing)
        {
            velocity.y = -wallSlideSpeed;
        }

        UpdateRayCastOriginWithOffset(velocity.x * Time.fixedDeltaTime); // Cập nhật vị trí tia với offset di chuyển ngang
        if (velocity.y > 0)
        {
            if (CorrectCeiling(velocity))
            {
                // Cập nhật lại vị trí raycast nếu có sửa góc trần
                UpdateRayCastOriginWithOffset(velocity.x * Time.fixedDeltaTime);
            }
        }
        if (velocity.y != 0) VerticalCollisionCheck();

        //Calculate new position
        Vector3 moveAmount = velocity * Time.fixedDeltaTime;

        //Handle moving platform
        if (currentMovingPlatform != null)
        {
            Vector3 platformDelta = currentMovingPlatform.position - lastPlatformPosition;
            moveAmount += platformDelta;

            // Cập nhật trí nhớ cho frame sau
            lastPlatformPosition = currentMovingPlatform.position;
        }

        Vector3 targetPosition = (Vector3)rb.position + moveAmount;
        rb.MovePosition(targetPosition);

        // Flip character
        HandleSpriteFlipping();
    }

    private void HandleSpriteFlipping()
    {
        // ƯU TIÊN 1: Khi bám tường hoặc trượt tường -> BẮT BUỘC quay mặt vào tường
        if (isWallSliding || isWallClimbing)
        {
            if (wallDirX != 0) // Kiểm tra an toàn
            {
                // Ép Scale X mang dấu của wallDirX (1 là phải, -1 là trái)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * wallDirX, transform.localScale.y, transform.localScale.z);
            }
        }
        // ƯU TIÊN 2: Đi bộ/Nhảy bình thường -> Quay mặt theo phím bấm
        // (Chỉ áp dụng khi không bị khóa hướng do vừa nhảy bật tường ra)
        else if (moveInput.x != 0 && wallJumpInputTimer <= 0 && CanMove)
        {
            float inputDirection = Mathf.Sign(moveInput.x);
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * inputDirection, transform.localScale.y, transform.localScale.z);
        }
    }

    // Tính toán vận tốc ngang
    private void CalculateHorizontalMovement()
    {
        // Khoá di chuyển ngang khi đang leo tường.
        if (isWallClimbing)
        {
            velocity.x = 0;
            return; // Thoát hàm ngay, không chạy logic bên dưới
        }

        if (isWaitingForHopX)
        {
            // Người chơi cố tình bấm phím di chuyển ngược hướng với vách tường
            bool isSteeringAway = (moveInput.x != 0 && Mathf.Sign(moveInput.x) != hopDirX);

            // Hủy nếu cố tình bẻ lái ra ngoài, HOẶC đã hết đà bay lên (velocity.y <= 0)
            if (isSteeringAway || velocity.y <= 0)
            {
                isWaitingForHopX = false;
                climbHopInputTimer = 0f; // Mở khóa Input ngay lập tức để người chơi lấy lại quyền kiểm soát!
            }

            else
            {
                // Bắn một tia raycast ngắn ở dưới chân về hướng tường để xem toàn thân đã lọt qua tường chưa
                Vector2 originBottom = (hopDirX == 1) ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;
                RaycastHit2D hitWall = Physics2D.Raycast(originBottom, Vector2.right * hopDirX, skinWidth * 2, collisionMask);

                if (!hitWall)
                {
                    // Toàn thân đã lọt lên trên vách tường -> BUNG LỰC NGANG ĐẨY VÀO TRONG!
                    velocity.x = hopDirX * climbHopForce.x;
                    isWaitingForHopX = false;
                }
                else if (velocity.y <= 0)
                {
                    // Nếu đang rơi xuống (Hết đà) mà vẫn chưa lọt -> Hủy lệnh đẩy ngang
                    isWaitingForHopX = false;
                }
            }
        }

        if (climbHopInputTimer > 0 || wallJumpInputTimer > 0)
        {
            // Ép nhân vật di chuyển theo hướng Hop (để chắc chắn leo lọt vào sàn)
            // Hoặc đơn giản là giữ nguyên velocity.x hiện tại do quán tính
            // Khoá input khi nhảy đường để đảm bảo quán tính
            //Debug.Log("Input locked due to recent wall jump or climb hop. Maintaining current velocity.x: " + velocity.x);
            return;
        }

        // 1. Tính vận tốc mục tiêu (Target Velocity)
        float targetSpeed = moveInput.x * moveSpeed;

        // 2. Chọn gia tốc phù hợp (Gia tốc tăng hay Gia tốc giảm?)
        float currentAccel;

        // Nếu người chơi thả tay ra (moveInput == 0) -> Cần Giảm tốc
        if (Mathf.Abs(targetSpeed) < 0.01f)
        {
            // Kiểm tra xem đang ở dưới đất hay trên trời để chọn ma sát
            currentAccel = isGrounded ? deceleration : airDeceleration;
        }
        else
        {
            // Nếu có Input, kiểm tra xem đang Tăng tốc hay Đang quay đầu
            // Nếu Input cùng hướng với vận tốc hiện tại -> Tăng tốc
            // Nếu Input ngược hướng (đang chạy trái mà bấm phải) -> Dùng Deceleration để phanh gấp rồi mới chạy tiếp
            if (Mathf.Sign(targetSpeed) == Mathf.Sign(velocity.x) && Mathf.Abs(targetSpeed) > Mathf.Abs(velocity.x))
            {
                currentAccel = acceleration;
            }
            else
            {
                // Đang quay đầu hoặc đang chạy nhanh hơn tốc độ tối đa (do ngoại lực)
                currentAccel = deceleration;
            }
        }

        // 3. Áp dụng công thức MoveTowards
        // Nó sẽ thay đổi velocity.x tiến về targetSpeed một lượng bằng (currentAccel * Time.deltaTime)
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, currentAccel * Time.deltaTime);
    }

    // Hàm gọi Player tự động đi đến vị trí và quay mặt
    public void MoveToAndFace(Transform targetPos, Transform lookAtTarget, Action onComplete)
    {
        StartCoroutine(AutoWalkRoutine(targetPos, lookAtTarget, onComplete));
    }

    private IEnumerator AutoWalkRoutine(Transform targetPos, Transform lookAtTarget, Action onComplete)
    {
        //KHÓA INPUT (Ngăn người chơi bấm nút lúc đang đi tới chỗ NPC)
        currentState = PlayerState.ScriptedMove;

        // Chạy vòng lặp cho đến khi khoảng cách X < 0.05
        while (Mathf.Abs(transform.position.x - targetPos.position.x) > 0.05f)
        {
            // Hướng đi sang trái hay phải
            float dirX = Mathf.Sign(targetPos.position.x - transform.position.x);

            // Di chuyển
            Vector3 nextPos = new Vector3(targetPos.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, nextPos, autoWalkSpeed * Time.deltaTime);

            // Xoay mặt theo hướng đi
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * dirX;
            transform.localScale = scale;

            OnRunChanged?.Invoke(true); // Báo hiệu đang chạy cho Animation

            yield return null; // Đợi frame tiếp theo
        }

        OnRunChanged?.Invoke(false); // Báo hiệu dừng chạy cho Animation

        // 3. ĐẾN NƠI -> QUAY MẶT VÀO NPC
        float faceDir = Mathf.Sign(lookAtTarget.position.x - transform.position.x);
        if (faceDir != 0)
        {
            Vector3 finalScale = transform.localScale;
            finalScale.x = Mathf.Abs(finalScale.x) * faceDir;
            transform.localScale = finalScale;
        }

        // 4. BÁO HIỆU XONG VIỆC BẰNG CALLBACK
        onComplete?.Invoke();
    }

    // Hàm Tách Biệt: Chỉ dùng cho chuyển phòng (Dựa trên thời gian và Vận tốc)
    public void ForceWalkIntoRoom(Vector2 direction, float duration)
    {
        if (currentState == PlayerState.Dead || currentState == PlayerState.ScriptedMove) return;
        StartCoroutine(RoomTransitionWalkRoutine(direction, duration));
    }

    private IEnumerator RoomTransitionWalkRoutine(Vector2 dir, float time)
    {
        SetState(PlayerState.ScriptedMove);
        OnRunChanged?.Invoke(true); // Báo hiệu Animation chạy

        float elapsedTime = 0f;

        while (elapsedTime < time)
        {
            // Dùng Vận tốc (Velocity) để KHÔNG làm hỏng Trọng lực khi đang nhảy/rơi
            velocity.x = dir.x * moveSpeed;

            // Xoay mặt
            if (dir.x != 0)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dir.x);
                transform.localScale = scale;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Trả lại trạng thái bình thường
        velocity.x = 0f;
        OnRunChanged?.Invoke(false); // Tắt Animation chạy
        SetState(PlayerState.Idle);
    }

    #region Jump Handling
    // Phương thức xử lý nhảy input: chỉ lưu thời điểm ấn để dùng jump buffer
    private void OnJump(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.State == GameState.Pause) return;

        if (!CanMove && currentState != PlayerState.Talking) return;

        if (context.performed)
        {
            lastJumpInputTime = Time.time;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.State == GameState.Pause) return;

        // Chỉ thực hiện khi đang bay lên (velocity.y > 0)
        // Không áp dụng khi đang rơi hoặc đang đứng yên
        if (velocity.y > 0)
        {
            velocity.y = velocity.y * variableJumpHeightMultiplier;
        }
    }

    private void ProcessJumpBuffer()
    {
        // Nếu người chơi vừa bấm nhảy trong khoảng thời gian cho phép
        if (Time.time - lastJumpInputTime <= jumpBufferTime && CanJump)
        {
            // ƯU TIÊN 1: NHẢY THƯỜNG (Từ mặt đất)
            // Kể cả khi đang sát tường nhưng chân vẫn chạm đất thì tính là nhảy thường
            if (Time.time - lastGroundedTime <= coyoteTime)
            {
                Jump(false, 0, false);
                lastJumpInputTime = -999f; // Hủy buffer
            }

            // ƯU TIÊN 2: NHẢY TƯỜNG (Sử dụng Wall Coyote Time thay vì check va chạm trực tiếp)
            // Cho phép nhảy nếu vừa rời tường chưa quá 0.1 giây
            else if (Time.time - lastWallTouchTime <= wallCoyoteTime)
            {
                // Kiểm tra Input người chơi để quyết định loại nhảy tường (Leap hay Climb)
                // Phải dùng lastWallDirX vì lúc này hitWall có thể đã bằng false
                bool isHoldingTowardsWall = (moveInput.x != 0 && Mathf.Sign(moveInput.x) == lastWallDirX);
                //bool isHoldingAwayFromWall = (moveInput.x != 0 && Mathf.Sign(moveInput.x) != lastWallDirX);
                bool isHoldingUp = moveInput.y > 0.1f;
                bool isNotHolding = moveInput.magnitude < 0.1f;
                bool isNotHoldingHorizontal = Mathf.Abs(moveInput.x) < 0.1f; // NGƯỜI CHƠI KHÔNG BẤM TRÁI/PHẢI

                // NẾU: Đang giữ nút Bám VÀ (cố tình ép vào tường HOẶC hướng lên HOẶC không giữ hướng di chuyển) VÀ còn thể lực
                if (input.IsGrabbing && (isHoldingTowardsWall || isHoldingUp || isNotHolding) && currentStamina > 0)
                {
                    // Trèo nhảy lên (Wall Climb Jump)
                    Jump(true, lastWallDirX, true);
                }

                // B. Nhảy trung lập (Neutral Wall Jump) - 🌟 TÍNH NĂNG MỚI: Không bấm hướng ngang
                else if (isNotHoldingHorizontal)
                {
                    Jump(true, lastWallDirX, false, true); // Gọi Jump với tham số isNeutral = true
                }
                else
                {
                    // CÁC TRƯỜNG HỢP KHÁC: Không bấm, bấm nhầm, bấm ra xa... Đều vọt ra ngoài!
                    // (Wall Leap)
                    Jump(true, lastWallDirX, false);
                }

                // Tiêu thụ lệnh nhảy để không nhảy đúp
                lastJumpInputTime = -999f;
                // QUAN TRỌNG: Xóa luôn trí nhớ tường để tránh bị spam nhảy tường trên không!
                lastWallTouchTime = -999f;
            }

            // ƯU TIÊN 3: NHẢY TRÊN KHÔNG (Double Jump, Triple Jump...)
            else if (currentAirJumps < maxAirJumps)
            {
                currentAirJumps++; // Trừ đi 1 lượt nhảy trên không

                // Gọi lại hàm Jump thường. Nó sẽ tự động đè velocity.y = jumpForce, 
                // tạo ra cảm giác nảy lên cực kỳ chuẩn xác dù nhân vật đang rơi nhanh cỡ nào!
                Jump(false, 0, false);

                lastJumpInputTime = -999f; // Hủy buffer để không nhảy đúp

                // (Tùy chọn) TODO: Bạn có thể gọi một âm thanh/Particle System khói nổ riêng cho Double Jump ở đây
                Debug.Log($"Air Jump performed! Performed {currentAirJumps}/{maxAirJumps} attempts.");
            }
        }
    }

    private void Jump(bool isWallJump, int wallDir, bool isClimbJump, bool isNeutral = false)
    {
        // Wall Jump
        if (isWallJump)
        {
            if (isClimbJump)
            {
                // Wall Climb Jump
                velocity.x = 0;
                velocity.y = wallClimbJumpForceY;

                currentStamina -= 15f; // Celeste trừ nhiều thể lực hơn cho cú nhảy này
                climbJumpCooldownTimer = 0.2f;
                //Debug.Log("Wall Climb jump!");
            }
            else if (isNeutral)
            {
                // NEUTRAL WALL JUMP
                // Đẩy ngược hướng tường với lực RẤT NHỎ
                velocity.x = -wallDir * neutralWallJumpForce.x;
                velocity.y = neutralWallJumpForce.y;

                // Áp dụng hình phạt thể lực
                currentStamina -= neutralJumpStaminaCost;

                // Khóa input ngắn để người chơi vẫn giữ được sự linh hoạt
                wallJumpInputTimer = neutralJumpInputLockTime;

                // Quay mặt ra ngoài tường một chút
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * (-wallDir), transform.localScale.y, transform.localScale.z);
            }
            else
            {
                // Wall Leap Jump
                velocity.x = -wallDir * wallJumpForce.x;
                velocity.y = wallJumpForce.y;
                wallJumpInputTimer = wallJumpInputLockTime;
                //Debug.Log("Wall Leap jump!");

                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (-wallDir);
                transform.localScale = scale;
                //currentStamina -= 10f;

            }
        }
        else
        // ---------------------------------
        {
            // Normal Jump
            velocity.y = jumpForce;
            //Debug.Log("Normal jump!");
        }

        // Cập nhật trạng thái
        currentState = PlayerState.OnAir;
        isGrounded = false;
        isWallClimbing = false; // Ngắt trạng thái leo ngay khi nhảy
        isWallSliding = false;

        lastGroundedTime = -999f; // Reset ground time

        OnJumpPerformed?.Invoke();
        SoundManager.Instance.PlaySound("Jump");
    }
    #endregion

    #region Wall Climbing Handling
    private void HandleWallClimbingPhysics() // Gọi trong MoveCharacter()
    {
        // --- BƯỚC 1: KIỂM TRA LEDGE SLIP (TRƯỢT MÉP) ---
        Vector2 originTop = (wallDirX == 1) ? rayCastOrigins.topRight : rayCastOrigins.topLeft;
        Vector2 originBottom = (wallDirX == 1) ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;

        // Bắn tia kiểm tra
        bool hitTop = Physics2D.Raycast(originTop, Vector2.right * wallDirX, skinWidth * 2, collisionMask);
        bool hitBottom = Physics2D.Raycast(originBottom, Vector2.right * wallDirX, skinWidth * 2, collisionMask);

        // Điều kiện trượt: Chân/thân đụng tường NHƯNG đỉnh đầu hụt mất
        bool isSlipping = hitBottom && !hitTop;

        if (isSlipping)
        {
            // Ép nhân vật trượt xuống, KHÔNG cho phép leo lên hay đứng im
            velocity.y = -ledgeSlipSpeed;
        }
        else
        {
            // --- BƯỚC 2: XỬ LÝ LEO TRÈO BÌNH THƯỜNG (Khi đã bám chắc) ---
            if (moveInput.y > 0)
            {
                velocity.y = moveInput.y * wallClimbSpeed;
                CheckForLedge(); // Hàm này có thể dùng để gọi ClimbHop() khi đã leo hẳn lên mép
            }
            else if (moveInput.y < 0)
            {
                velocity.y = moveInput.y * wallClimbDownSpeed;
            }
            else
            {
                velocity.y = 0; // Đứng yên
            }

            // --- BƯỚC 3: TRỪ THỂ LỰC ---
            // (Chỉ trừ thể lực khi không bị trượt mép)
            float drain = (moveInput.y != 0) ? climbStaminaDrain : holdStaminaDrain;
            currentStamina -= drain * Time.fixedDeltaTime;

            // --- CẢM BIẾN BÁO ĐỘNG THỂ LỰC ---
            bool isCurrentlyLow = (currentStamina > 0 && currentStamina <= lowStaminaThreshold);

            // Chỉ phát Event khi có sự thay đổi trạng thái (Từ an toàn sang nguy hiểm, hoặc ngược lại)
            if (isCurrentlyLow != isLowStaminaState)
            {
                isLowStaminaState = isCurrentlyLow;
                OnLowStamina?.Invoke(isLowStaminaState);
            }
        }

        // --- BƯỚC 4: RỚT KHI HẾT THỂ LỰC ---
        if (currentStamina <= 0)
        {
            currentStamina = 0;
            currentState = PlayerState.OnAir;
            isWallClimbing = false;
        }
    }

    private void CheckWallGrab() // Gọi trong FixedUpdate() hoặc UpdatePhysicsState()
    {
        // Nếu đang trong thời gian Cooldown sau khi nhảy dọc tường -> Cấm bám
        if (climbJumpCooldownTimer > 0 || climbHopInputTimer > 0)
        {
            isWallClimbing = false;
            return;
        }

        // Điều kiện: Đang giữ phím Grab + Chạm tường + Không ở mặt đất + Còn thể lực
        bool isTouchingWall = hitLeftWall || hitRightWall;

        if (input.IsGrabbing && isTouchingWall && currentStamina > 0 && canClimbCurrentWall)
        {
            currentState = PlayerState.Climbing;
            isWallClimbing = true;
            isWallSliding = false; // Ưu tiên leo hơn trượt
            velocity.x = 0; // Dừng di chuyển khi bám tường
            isGrounded = false; // Đảm bảo không bị chạm đất khi bám tường
        }
        else
        {
            isWallClimbing = false;
            // Nếu không leo, trạng thái sẽ do UpdatePhysicsState quyết định (OnAir/Sliding...)
        }
    }

    private void CheckForLedge()
    {
        // Chỉ kiểm tra khi đang bám tường hoặc trượt tường
        if (!isWallClimbing && !isWallSliding) return;

        // Logic Raycast kiểm tra mép (như bạn đã làm với Gizmos)
        float rayLength = skinWidth * 2;
        Vector2 originTop = (wallDirX == 1) ? rayCastOrigins.topRight : rayCastOrigins.topLeft;

        // Bắn tia ngay trên đỉnh đầu
        Vector2 rayPos = originTop + Vector2.up * 0.1f; // Nhích lên 1 xíu
        RaycastHit2D hitLedge = Physics2D.Raycast(rayPos, Vector2.right * wallDirX, rayLength, collisionMask);

        // NẾU: Đang bám tường (hitBody trúng) NHƯNG Trên đầu lại thoáng (hitLedge trượt)
        // VÀ: Người chơi đang bấm LÊN (Input.y > 0) hoặc đang leo lên
        if (!hitLedge && velocity.y >= 0) // Celeste còn check Input.MoveY != 1 (xuống)
        {
            PerformClimbHop();
        }
    }

    private void PerformClimbHop()
    {
        hopDirX = wallDirX;

        // CHỈ cấp lực bật lên (Trục Y), KHÔNG cấp trục X ngay lập tức
        velocity.y = climbHopForce.y;
        velocity.x = 0;

        // Khóa Input để người chơi không bẻ lái lung tung
        climbHopInputTimer = climbHopNoInputTime;

        // Bật cờ chờ đẩy ngang
        isWaitingForHopX = true;

        currentState = PlayerState.OnAir;
        isWallClimbing = false;
        isWallSliding = false;
        isGrounded = false;
        lastGroundedTime = -999f;

        OnJumpPerformed?.Invoke();
    }
    #endregion

    #region Grabbling Handling
    private void OnGrappleInput(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.State == GameState.Pause) return;

        // Chặn nếu đang bận, đang dừng hình, hoặc đang bị kéo
        if (!CanMove || currentState == PlayerState.GrappleFreeze || currentState == PlayerState.Grappling) return;

        if (context.performed)
        {
            // Check if can talk to NPC
            if (currentState == PlayerState.Idle && currentDialogueTrigger != null)
            {
                currentDialogueTrigger.TriggerDialogue();

                // Dừng lại ở đây, KHÔNG thực hiện code nhảy bên dưới
                return;
            }

            if (!isGrounded && currentGrappleCount >= maxGrappleCount)
            {
                Debug.Log("All attempts to shoot grappling hand on air have been used up!");
                return;
            }

            if (!isGrounded) currentGrappleCount++;

            StartCoroutine(PerformGrappleWithLeniency());
        }
    }

    private IEnumerator PerformGrappleWithLeniency()
    {
        OnShootHand?.Invoke();
        // 1. ĐÓNG BĂNG VẬT LÝ VÀ CHUYỂN TRẠNG THÁI
        currentState = PlayerState.GrappleFreeze;
        velocity = Vector3.zero; // Dừng toàn bộ vận tốc (trọng lực, quán tính)

        // 2. CHỜ 0.05s ĐỂ NGƯỜI CHƠI NHẬP HƯỚNG
        yield return new WaitForSeconds(grappleInputWindow);

        // 3. ĐỌC HƯỚNG BẮN
        Vector2 shootDir = Vector2.zero;

        // Nếu có bấm phím điều hướng (Analog hoặc D-pad)
        if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
        {
            // Ép hướng về 8 hướng cơ bản (Lên, Xuống, Trái, Phải và 4 góc chéo)
            shootDir = new Vector2(Mathf.Sign(moveInput.x) * Mathf.Round(Mathf.Abs(moveInput.x)),
                                   Mathf.Sign(moveInput.y) * Mathf.Round(Mathf.Abs(moveInput.y))).normalized;
        }
        else
        {
            // Mặc định bắn ngang theo hướng mặt nhân vật đang nhìn
            shootDir = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }

        // Ép nhân vật quay mặt theo hướng bắn
        if (shootDir.x != 0)
        {
            Vector3 playerScale = transform.localScale;
            // Ép Scale X của nhân vật mang dấu của hướng bắn (Trái = âm, Phải = dương)
            playerScale.x = Mathf.Abs(playerScale.x) * Mathf.Sign(shootDir.x);
            transform.localScale = playerScale;
        }

        // 4. PHÓNG TAY VÀ KHÓA CHỜ
        currentState = PlayerState.Grappling;
        if (robotHand != null)
        {
            robotHand.Shoot(shootDir, this);
        }
    }

    // RobotHand gọi hàm này khi nó cắm trúng vách tường
    public void StartPullingToHand(Vector2 targetPos, Transform wallHit)
    {
        grappleTarget = targetPos;
        // Tự động nhận diện vách tường nằm bên nào để nhân vật úp mặt vào tường chuẩn xác
        wallDirX = (grappleTarget.x > transform.position.x) ? 1 : -1;
    }

    // RobotHand gọi hàm này khi cắm trượt (bay hết cỡ không trúng gì)
    public void CancelGrapple()
    {
        // Hủy trạng thái Grapple để nhân vật rơi xuống lại
        if (currentState == PlayerState.Grappling || currentState == PlayerState.GrappleFreeze)
        {
            currentState = PlayerState.OnAir;
            grappleTarget = Vector2.zero;
        }
    }
    #endregion
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
                   currentState == PlayerState.Running ||
                   currentState == PlayerState.OnAir;
        }
    }

    // Property kiểm tra xem trạng thái hiện tại có thuộc nhóm "Cấm Input" hay không
    public bool IsInputDisabled
    {
        get
        {
            return currentState == PlayerState.Talking ||
                   currentState == PlayerState.Dead ||
                   currentState == PlayerState.ScriptedMove;
        }
    }

    private void UpdatePhysicsState()
    {
        // Nếu đang bận việc riêng (Talking/Dead) thì không can thiệp
        if (currentState == PlayerState.Talking || currentState == PlayerState.Dead
            || currentState == PlayerState.Grappling || currentState == PlayerState.GrappleFreeze) return;

        if (isGrounded)
        {
            currentStamina = maxStamina;

            if (isLowStaminaState)
            {
                isLowStaminaState = false;
                OnLowStamina?.Invoke(false);
            }

            RechargeAbilities(); // Sạc lại kỹ năng mỗi lần chạm đất
        }

        CheckWallGrab(); // Kiểm tra có đang leo tưởng hay không
        if (isWallClimbing) return; // Nếu đang leo thì không chạy logic bên dưới

        // Chỉ lưu điểm an toàn khi: Đang chạm đất + Đứng yên + Không ở trạng thái nguy hiểm
        if (isGrounded && velocity.magnitude < 0.1f && CanMove)
        {
            lastSafePosition = transform.position;
        }

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
        bool isClimbing = (currentState == PlayerState.Climbing && moveInput.y > 0.1f);
        OnClimbChanged?.Invoke(isClimbing);

        // 3. Xử lý Event Chạy và Cập nhật trạng thái chạm đất cho Animation
        bool isRunning = (currentState == PlayerState.Running);
        OnRunChanged?.Invoke(isRunning);
        OnGroundedChanged?.Invoke(isGrounded);
    }

    // Gọi hàm này khi bị rớt map, kẹt góc, hoặc văng quá xa
    private void EmergencyReset()
    {
        // 1. Dừng mọi luồng Coroutine đang chạy ngầm (AutoWalk, v.v.)
        StopAllCoroutines();

        // 2. Thu hồi Grappling Hook (nếu tay xích đang kẹt trong tường)
        if (robotHand != null) robotHand.FinishGrapple();
        grappleTarget = Vector2.zero;

        velocity = Vector3.zero;

        // 4. DỊCH CHUYỂN VỀ ĐIỂM AN TOÀN TRƯỚC ĐÓ (Teleport)
        if (lastSafePosition != Vector3.zero)
        {
            transform.position = lastSafePosition;
            if (rb != null) rb.position = lastSafePosition;
        }

        // 5. Trả về trạng thái an toàn
        SetState(PlayerState.Idle);
        isGrounded = false; // Reset để hàm VerticalCollisionCheck tự động tính lại vào frame sau

        // 6. Reset lại bộ đếm Watchdog
        currentStuckTimer = 0f;
    }

    // Xử lý Unity Timeline
    public void OnTimelineStart()
    {
        // Đưa vào nhóm State bị cấm Input và cấm Vật lý
        SetState(PlayerState.ScriptedMove);
        velocity = Vector3.zero;
    }

    public void OnTimelineEnd()
    {
        // Đánh thức hệ thống vật lý
        SetState(PlayerState.Idle);

        // Reset các trạng thái chạm đất để raycast tự cập nhật vào frame sau
        wasGrounded = false;
        isGrounded = false;
    }
    #endregion

    #region Collision Handling
    void VerticalCollisionCheck()
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + skinWidth;

        // Kéo dài tia raycast xuống thêm một chút khi đang đứng trên Moving Platform đang đi xuống
        if (currentMovingPlatform != null && directionY == -1)
        {
            rayLength += 0.2f;
        }

        if (directionY == -1 || velocity.y > 0)
        {
            isGrounded = false;
        }

        Transform hitPlatform = null; // Biến tạm để lưu bệ đỡ nếu dẫm trúng trong frame này

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;

            rayOrigin += Vector2.right * (verticalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                // Bỏ qua va chạm nếu tia ray phát hiện va chạm ở khoảng cách 0 (bị kẹt bên trong vật thể),
                if (hit.distance == 0) continue;

                // Xử lí One-way platform
                if (hit.collider.CompareTag("OneWay")){
                    // 1. Đang bay lên -> Bỏ qua va chạm (xuyên qua sàn)
                    if (directionY > 0) continue;

                    // 2. Nếu đang rớt xuống, nhưng tia ray bắt đầu từ BÊN TRONG vật thể
                    // (hit.distance == 0 nghĩa là chân nhân vật đã lọt vào sàn lúc đang ở đỉnh cú nhảy -> Cho rớt xuyên nốt để không bị kẹt)
                    if (hit.distance == 0) continue;
                }

                velocity.y = (hit.distance - skinWidth) * directionY / Time.fixedDeltaTime; // Tính lại vận tốc thực dựa trên quãng đường còn lại
                rayLength = hit.distance; // Thu ngắn tia lại để không check xuyên qua vật cản gần nhất

                if (directionY == -1) // Đang rơi xuống
                {
                    if (!wasGrounded)
                    {
                        // Chỉ phát âm thanh chạm đất nếu trước đó ĐANG RƠI hoặc ĐANG TRƯỢT TƯỜNG
                        if (currentState == PlayerState.OnAir || currentState == PlayerState.Sliding)
                        {
                            OnLanded?.Invoke();
                        }

                        // Nếu lúc chạm đất mà người chơi không bấm phím di chuyển -> Khóa vận tốc ngang về 0 ngay lập tức
                        if (Mathf.Abs(moveInput.x) < 0.01f)
                        {
                            velocity.x = 0f;
                        }
                    }

                    isGrounded = true;
                    lastGroundedTime = Time.time;

                    // KIỂM TRA MOVING PLATFORM: Nếu dẫm trúng thì lưu lại
                    if (hit.collider.CompareTag("MovingPlatform"))
                    {
                        hitPlatform = hit.transform;
                    }
                }
                else // Đang nhảy lên đụng trần
                {
                    velocity.y = 0; // Tùy chọn: Đụng trần thì dừng hẳn lực nảy
                }
            }
        }

        if (hitPlatform != null)
        {
            // Nếu đây là bệ đỡ mới (vừa nhảy lên) -> Cập nhật tọa độ tham chiếu
            if (currentMovingPlatform != hitPlatform)
            {
                currentMovingPlatform = hitPlatform;
                lastPlatformPosition = currentMovingPlatform.position;
            }
        }
        else
        {
            // Nếu không dẫm trúng (nhảy ra khỏi bệ, hoặc rớt xuống) -> Hủy trí nhớ
            currentMovingPlatform = null;
        }
    }

    void HorizonalCollisionCheck()
    {
        float directionX;

        if (isWallClimbing)
        {
            directionX = wallDirX;
        }
        else if (velocity.x != 0)
        {
            directionX = Mathf.Sign(velocity.x);
        }
        else
        {
            // Khi đứng yên (velocity = 0), ta lấy hướng theo Input
            if (moveInput.x != 0)
            {
                directionX = Mathf.Sign(moveInput.x);
            }
            else
            {
                // Nếu không bấm gì cả, lấy hướng mặt hiện tại của nhân vật
                directionX = Mathf.Sign(transform.localScale.x);
            }
        }

        float rayLength = Mathf.Abs(velocity.x * Time.fixedDeltaTime) + skinWidth;
        // Nếu đứng yên thì vẫn bắn tia.
        if (Mathf.Abs(velocity.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        canClimbCurrentWall = false; // Mặc định là không thể leo

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                // Bỏ qua HOÀN TOÀN va chạm ngang với nền One-way để không bị vướng đầu/vai
                if (hit.collider.CompareTag("OneWay")) continue;

                if (!hit.collider.CompareTag("Unclimbable")) canClimbCurrentWall = true;

                // Nếu khoảng cách quá gần (bằng 0), nghĩa là đang kẹt trong tường -> bỏ qua
                if (hit.distance == 0) continue;

                // Chỉ cập nhật vận tốc nếu thực sự đang di chuyển
                if (velocity.x != 0)
                {
                    //float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                    velocity.x = (hit.distance - skinWidth) * directionX / Time.fixedDeltaTime;
                    rayLength = hit.distance;
                }

                // Cập nhật trạng thái va chạm tường
                if (directionX == -1) hitLeftWall = true;
                if (directionX == 1) hitRightWall = true;

                if (hitLeftWall || hitRightWall)
                {
                    // KIỂM TRA ĐIỀU KIỆN TRƯỚC KHI CHO PHÉP NHẢY
                    if (canClimbCurrentWall)
                    {
                        // Nếu tường bám được -> Lưu lại trí nhớ để cho phép Wall Jump
                        lastWallTouchTime = Time.time;
                        lastWallDirX = hitLeftWall ? -1 : 1;
                    }
                    else
                    {
                        // XÓA TRÍ NHỚ NẾU CHẠM TƯỜNG TRƠN (Teflon Wall)
                        // Bất kể người chơi thao tác nhanh cỡ nào, hệ thống cũng không ghi nhận đây là tường để nhảy
                        lastWallTouchTime = -999f; 
                    }
                }

                //Phát hiện tường
                wallDirX = (directionX == 1) ? 1 : -1;
                // Trạng thái trượt tường: Đang chạm tường, đang rơi và người chơi đang ấn phím hướng vào tường
                bool isPressingAgainstWall = (moveInput.x > 0 && wallDirX == 1) || (moveInput.x < 0 && wallDirX == -1);

                if (!isGrounded && velocity.y < 0 && isPressingAgainstWall && canClimbCurrentWall)
                {
                    isWallSliding = true;
                }
            }
        }
    }

    // Hàm trả về True nếu đã thực hiện sửa góc (đẩy nhân vật lên)
    bool CorrectCorner(Vector2 moveVelocity)
    {
        // Chỉ sửa góc khi đang di chuyển vào tường hoặc nhảy lên
        if (Mathf.Abs(moveVelocity.x) < 0.01f && moveVelocity.y <= 0) return false;

        // Xác định hướng đang lao tới
        float directionX = (moveVelocity.x != 0) ? Mathf.Sign(moveVelocity.x) : wallDirX;

        // Vị trí bắn tia: Đỉnh đầu
        Vector2 origin = (directionX == 1) ? rayCastOrigins.topRight : rayCastOrigins.topLeft;

        // 1. TIA ĐẦU (Head Ray): Kiểm tra xem đầu có thoáng không?
        // Bắn dài hơn skinWidth một chút để dự đoán va chạm
        float rayLength = Mathf.Abs(moveVelocity.x * Time.fixedDeltaTime) + skinWidth * 2;
        RaycastHit2D hitHead = Physics2D.Raycast(origin, Vector2.right * directionX, rayLength, collisionMask);

        if (hitHead && hitHead.collider.CompareTag("OneWay")) hitHead = default;

        // Nếu đầu va phải tường -> Không phải là góc -> Không sửa được
        if (hitHead) return false;

        // 2. TIA THÂN (Body Ray): Kiểm tra xem vai/ngực có bị vướng không?
        // Hạ thấp xuống một đoạn nhỏ (bạn có thể tạo biến cornerDetectOffset)
        Vector2 bodyOrigin = origin + Vector2.down * 0.1f;
        RaycastHit2D hitBody = Physics2D.Raycast(bodyOrigin, Vector2.right * directionX, rayLength, collisionMask);

        if (hitBody && hitBody.collider.CompareTag("OneWay")) hitBody = default;

        // --- LOGIC SỬA GÓC ---
        // Nếu ĐẦU THOÁNG (Miss) mà THÂN VƯỚNG (Hit) => Đang ở mép/góc
        if (hitBody)
        {
            // Đẩy nhân vật lên trên một chút để "trượt" qua mép
            // Thay vì dùng velocity, ta dịch chuyển position trực tiếp để chính xác tuyệt đối

            transform.position += Vector3.up * cornerCorrectionSpeed;

            // Cập nhật lại Physics Position
            rb.position = transform.position;

            // Trả về True để báo cho MoveCharacter biết: "Tao đã xử lý va chạm này rồi, đừng chặn velocity.x lại!"
            return true;
        }

        return false;
    }

    // TODO: Nâng cấp thành OverlapBox nếu phát hiện lỗi xuyên tường sau này
    bool CorrectCeiling(Vector2 moveVelocity)
    {
        // Chỉ xử lý khi đang bay lên
        if (moveVelocity.y <= 0) return false;

        // Độ dài tia: Bắn dài hơn vận tốc một chút để dự đoán trước va chạm
        float rayLength = Mathf.Abs(moveVelocity.y * Time.fixedDeltaTime) + skinWidth * 2;

        // Kiểm tra góc trái
        Vector2 outerLeft = rayCastOrigins.topLeft;
        // Inner: Thụt vào trong một đoạn tolerance
        Vector2 innerLeft = rayCastOrigins.topLeft + Vector2.right * ceilingTolerance;

        RaycastHit2D hitOuterLeft = Physics2D.Raycast(outerLeft, Vector2.up, rayLength, collisionMask);
        RaycastHit2D hitInnerLeft = Physics2D.Raycast(innerLeft, Vector2.up, rayLength, collisionMask);

        // Xoá bỏ kết quả nếu bắn tia trúng One-way platform
        if (hitOuterLeft && hitOuterLeft.collider.CompareTag("OneWay")) hitOuterLeft = default;
        if (hitInnerLeft && hitInnerLeft.collider.CompareTag("OneWay")) hitInnerLeft = default;

        // LOGIC: Vai trúng (Outer Hit) nhưng Đầu lọt (Inner Miss) => Đẩy sang PHẢI
        if (hitOuterLeft && !hitInnerLeft)
        {
            // Dịch chuyển tức thời
            transform.position += Vector3.right * ceilingNudgeAmount;
            rb.position = transform.position; // Đồng bộ Rigidbody
            return true;
        }

        // Kiểm tra góc phải
        Vector2 outerRight = rayCastOrigins.topRight;
        Vector2 innerRight = rayCastOrigins.topRight + Vector2.left * ceilingTolerance;

        RaycastHit2D hitOuterRight = Physics2D.Raycast(outerRight, Vector2.up, rayLength, collisionMask);
        RaycastHit2D hitInnerRight = Physics2D.Raycast(innerRight, Vector2.up, rayLength, collisionMask);

        // Xoá bỏ kết quả nếu bắn tia trúng One-way platform
        if (hitOuterRight && hitOuterRight.collider.CompareTag("OneWay")) hitOuterRight = default;
        if (hitInnerRight && hitInnerRight.collider.CompareTag("OneWay")) hitInnerRight = default;

        // LOGIC: Vai trúng (Outer Hit) nhưng Đầu lọt (Inner Miss) => Đẩy sang TRÁI
        if (hitOuterRight && !hitInnerRight)
        {
            // Dịch chuyển tức thời
            transform.position += Vector3.left * ceilingNudgeAmount;
            rb.position = transform.position;
            return true;
        }

        return false;
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
            // Không đẩy nhân vật ra nếu đang xuyên qua nền One-way
            if (results[i].CompareTag("OneWay")) continue;

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
                    //Debug.Log("Resolved Overlap");
                    //if (isGrounded) Debug.Log("Is grounded during overlap resolution");
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

    #region Respawn Handling
    public void SetRespawnPoint(Vector2 newPoint)
    {
        currentRespawnPoint = newPoint;
        lastSafePosition = currentRespawnPoint;
    }

    public void Die()
    {
        // Tránh trường hợp gọi hàm Die nhiều lần (ví dụ chạm 2 cái gai cùng lúc)
        if (currentState == PlayerState.Dead) return;

        // 1. Chuyển trạng thái để khóa toàn bộ Input và Vật lý
        SetState(PlayerState.Dead);

        // 2. Ép dừng mọi chuyển động ngay lập tức
        velocity = Vector3.zero;

        // 3. Tắt hình ảnh nhân vật (Tạo cảm giác nhân vật vỡ vụn biến mất)
        // Lưu ý: Đừng Destroy(gameObject), vì ta còn phải hồi sinh nó!
        GetComponentInChildren<SpriteRenderer>().enabled = false;

        // TODO: Gọi FMOD phát tiếng vỡ vụn ở đây
        // TODO: Gọi Particle System nổ ra các mảnh vỡ ở đây

        // 4. Bắt đầu đếm ngược hồi sinh
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Kích hoạt ANIMATION START
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.PlayStartTransition();
        }

        // --- BẮT ĐẦU HỒI SINH ---

        // Đợi cho Animation Start chạy xong
        yield return new WaitForSeconds(TransitionManager.Instance.transitionTime + 0.5f);

        // ANIMATION START đã xong, giờ mới cho nhân vật hiện lại và hồi sinh thực sự
        

        // 1. Dịch chuyển nhân vật về điểm Checkpoint
        transform.position = currentRespawnPoint;
        if (rb != null) rb.position = currentRespawnPoint; // Cập nhật cả Rigidbody cho chắc chắn

        // 2. Reset lại các chỉ số quan trọng
        velocity = Vector3.zero;
        currentStamina = maxStamina; // Hồi đầy thể lực
        isGrounded = false; // Bắt buộc false để tính toán lại chạm đất an toàn

        // Nếu tay móc đang bắn ra, thu hồi lại
        if (robotHand != null) robotHand.FinishGrapple();

        // 3. Bật lại hình ảnh nhân vật
        GetComponentInChildren<SpriteRenderer>().enabled = true;

        // Delay một chút để đủ tổng thời gian 1 giây
        yield return new WaitForSeconds(0.4f);

        // KÍCH HOẠT ANIMATION END
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.PlayEndTransition();
        }

        // 4. Trả lại quyền điều khiển
        SetState(PlayerState.Idle);
    }
    #endregion

    #region Player Progress
    private void RechargeAbilities()
    {
        currentGrappleCount = 0;
        currentAirJumps = 0;
    }

    #endregion

    #region Callback Fuctions
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
    }

    private void OnDrawGizmos()
    {
        // 1. Vẽ BoxCollider
        if (col == null) col = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Màu đỏ mờ
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);

        // 2. Tính toán lại Bounds và Origin giống hệt logic Physics
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        // Xác định hướng vẽ (Dựa vào hướng mặt nhân vật vì khi không chạy game wallDirX = 0)
        // Nếu đang chạy game thì ưu tiên wallDirX
        float direction = (wallDirX != 0) ? wallDirX : Mathf.Sign(transform.localScale.x);

        Vector2 originTop = (direction == 1) ? new Vector2(bounds.max.x, bounds.max.y) : new Vector2(bounds.min.x, bounds.max.y);

        DrawClimbHopRays(originTop, direction);

        Vector2 topLeft = new Vector2(bounds.min.x, bounds.max.y);
        Vector2 topRight = new Vector2(bounds.max.x, bounds.max.y);
        DrawCeilingCorrectionRays(topLeft, topRight);

    }

    private void OnEnable()
    {
        // Đăng ký với CharacterInput
        if (input != null)
        {
            input.JumpPerformed += OnJump;
            input.JumpCanceled += OnJumpCanceled;
            input.GrapplePerformed += OnGrappleInput;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.JumpPerformed -= OnJump;
            input.JumpCanceled -= OnJumpCanceled;
            input.GrapplePerformed -= OnGrappleInput;
        }
    }

    #region Custom Draw
    void DrawClimbHopRays(Vector2 originTop, float direction)
    {
        // 1. Cấu hình giống hệt logic CheckForLedge
        // Nếu bạn đã đưa 0.1f ra biến riêng thì thay vào đây nhé
        float verticalOffset = 0.1f;
        float rayLength = skinWidth * 2; // Độ dài tia check mép

        // Vị trí bắn tia: Đỉnh đầu + nhích lên một chút
        Vector2 rayOrigin = originTop + (Vector2.up * verticalOffset);
        Vector2 rayDirection = Vector2.right * direction;
        Vector2 rayEnd = rayOrigin + (rayDirection * rayLength);

        // 2. Kiểm tra thử va chạm (Simulation) để chọn màu Gizmos
        // Lưu ý: Gizmos chạy trong Editor mode nên ta dùng Physics2D.Raycast để mô phỏng
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionMask);

        if (hit)
        {
            // Trúng tường => Chưa đến mép => Màu ĐỎ
            Gizmos.color = Color.red;
        }
        else
        {
            // Không trúng tường => Đã đến mép (Thoáng) => Màu XANH (Sẵn sàng Hop)
            Gizmos.color = Color.green;
        }

        // 3. Vẽ tia
        Gizmos.DrawLine(rayOrigin, rayEnd);

        // 4. Vẽ điểm gốc để dễ căn chỉnh Offset
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rayOrigin, 0.015f);
    }

    void DrawCeilingCorrectionRays(Vector2 topLeft, Vector2 topRight)
    {
        // Độ dài tia hiển thị (chỉ dùng cho Gizmos để dễ nhìn)
        float ceilRayLength = skinWidth * 5;

        // 1. GÓC TRÁI
        Vector2 outerLeft = topLeft;
        Vector2 innerLeft = topLeft + Vector2.right * ceilingTolerance;

        // Vẽ tia Outer Left (Xanh lá - Tia mép ngoài, mong muốn TRÚNG)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(outerLeft, outerLeft + Vector2.up * ceilRayLength);
        Gizmos.DrawSphere(outerLeft, 0.015f);

        // Vẽ tia Inner Left (Đỏ - Tia lùi vào trong, mong muốn TRƯỢT)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(innerLeft, innerLeft + Vector2.up * ceilRayLength);
        Gizmos.DrawSphere(innerLeft, 0.015f);


        // 2. GÓC PHẢI
        Vector2 outerRight = topRight;
        Vector2 innerRight = topRight + Vector2.left * ceilingTolerance;

        // Vẽ tia Outer Right (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(outerRight, outerRight + Vector2.up * ceilRayLength);
        Gizmos.DrawSphere(outerRight, 0.015f);

        // Vẽ tia Inner Right (Đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(innerRight, innerRight + Vector2.up * ceilRayLength);
        Gizmos.DrawSphere(innerRight, 0.015f);
    }
    #endregion
    #endregion

    #region structs
    struct RayCastOrigin
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
    #endregion
}