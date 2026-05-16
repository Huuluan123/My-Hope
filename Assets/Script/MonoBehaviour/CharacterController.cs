using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public enum PlayerState
{
    Idle,
    Running,
    OnAir,
    Sliding,
    Climbing,
    Pushing,

    /* Nhóm khoá input */
    GrappleFreeze,
    Grappling,

    Talking,
    WaitingForCutscene,
    Dead,
    ScriptedMove
}

public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Components")]
    public static PlayerController Instance; // Biến Singleton
    private Vector2 baseScale;

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
    [SerializeField] private float jumpHorizontalBoost = 5f; // Lực đẩy ngang khi nhảy (Nên set bằng khoảng 40-50% moveSpeed của bạn)
    [SerializeField] private const float skinWidth = 0.015f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private float apexHangThreshold = 6f; // Ngưỡng vận tốc y để bắt đầu giảm trọng lực (Khi nhân vật đang ở gần đỉnh nhảy, tức là vận tốc y nhỏ hơn 2 đơn vị)
    [SerializeField] private float apexHangMultiplier = 0.5f; // Hệ số nhân trọng lực khi ở trạng thái Apex Hang (0.5f nghĩa là giảm 50% lực hút, giúp nhân vật lơ lửng lâu hơn ở đỉnh nhảy)
    [SerializeField] private int verticalRayCount = 8;
    [SerializeField] private int horizontalRayCount = 8;
    float verticalRaySpacing;
    float horizontalRaySpacing;

    #region Gameplay Mechanics
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

    [Header("Push/Pull Mechanics")]
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // Giảm 50% tốc độ khi đẩy hộp
    [SerializeField] private float grabReachDistance = 0.02f;   // Tầm với tay ra lấy hộp
    private IPushable currentPushable;

    [Header("Grappling Hand")]
    [SerializeField] private RobotHand robotHand;
    [SerializeField] private float pullSpeed = 30f;
    [SerializeField] private float grappleInputWindow = 0.05f; // Cửa sổ chờ Input (0.05s)
    private Vector2 grappleTarget;
    private GrappleNode targetGrappleNode;

    [Header("Climb hop Settings")]
    [SerializeField] private Vector2 climbHopForce = new Vector2(10f, 15f); // Lực đẩy lên sàn (X nhỏ, Y lớn)
    [SerializeField] private float climbHopNoInputTime = 0.2f; // Thời gian khóa Input ngang để nhân vật lọt vào sàn
    private float climbHopInputTimer; // Timer đếm ngược
    private bool isWaitingForHopX; // Cờ đánh dấu đang chờ bung lực ngang
    private int hopDirX; // Hướng đẩy vào sàn

    [Header("Corner & Step Correction")]
    //[SerializeField] private float cornerCorrectionSpeed = 0.1f; // Khoảng cách đẩy lên mỗi frame (Pixel perfect)
    [SerializeField] private float ceilingTolerance = 0.15f; // Độ rộng tối đa của góc có thể lách qua (Tùy theo chiều rộng nhân vật)
    [SerializeField] private float ceilingNudgeAmount = 0.2f;
    [Space]
    //[Tooltip("Khoảng cách đẩy lên mỗi frame khi leo bậc (VD: 0.05f)")]
    //[SerializeField] private float stepCorrectionSpeed = 0.05f;
    [Tooltip("Độ cao tối đa của gờ đất có thể tự động bước qua")]
    [SerializeField] private float stepHeight = 0.25f;
    [Tooltip("Nâng tia quét dưới cùng lên một chút để không quét trúng mặt phẳng ngang")]
    [SerializeField] private float bottomRayOffset = 0.05f;

    [Header("Fast Fall & Visuals")]
    [SerializeField] private float fastMaxFallSpeed = -35f; // (Nên để tầm -35 hoặc -30)
    [SerializeField] private float fastMaxAccel = 40f; // Gia tốc mở rộng giới hạn rơi
    private float currentMaxFall; // Biến lưu trữ giới hạn rơi hiện tại

    [Header("Wall Speed Retention")]
    [SerializeField] private float wallSpeedRetentionTime = 0.06f; // Nhớ vận tốc trong 0.06 giây (khoảng 3-4 frame)
    private float wallSpeedRetentionTimer;
    private float wallSpeedRetained;

    [Header("Jump Thru Assist")]
    [SerializeField] private float jumpThruAssistSpeed = 4f; // Tốc độ đẩy trượt lên bục

    [Header("Holding System")]
    public Transform holdSlot; // Object con của Robot làm vị trí nắm tay
    private IHoldable currentlyHeldObject;
    #endregion


    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private float diagonalThreshold = 0.35f;
    [SerializeField] private PlayerState currentState;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    [HideInInspector] public bool IsGrounded => isGrounded;
    [SerializeField] private bool isStandingOnOneWay;
    private bool wasGrounded;
    [SerializeField] private bool hitLeftWall;
    [SerializeField] private bool hitRightWall;
    private bool hitCeiling;
    private bool hitFloor;
    private bool canClimbCurrentWall;

    [Space]
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float jumpTime;

    public Vector2 Velocity
    {
        get { return velocity; }
    }

    [Header("Input Forgiveness")]
    [Tooltip("Input forgiveness time for down input when jumping (0.05s)")]
    [SerializeField] private float downInputBufferTime = 0.05f;
    private float lastDownInputTime = -999f;

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

    [Header("One-Way Platform Settings")]
    [SerializeField] private float oneWayDropGhostTime = 0.2f; // Thời gian đi xuyên sàn
    private float ignoreOneWayTimer; // Bộ đếm lùi

    [Header("Respawn Settings")]
    [SerializeField] private Vector2 currentRespawnPoint;
    [HideInInspector] public float lastSpawnTime;
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
    [HideInInspector] public RoomManager currentRoom;
    private CutsceneSequence pendingSequence;

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
        lastSpawnTime = Time.unscaledTime;

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
        baseScale = new Vector2(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
    }

    void Start()
    {
        InitializeClosestCheckpoint();
        CalculateRaySpacing();
        currentMaxFall = maxFallSpeed;

        // Khởi động hệ thống Fail-safe ngay khi nhân vật xuất hiện
        StartCoroutine(RoomFailSafeRoutine());
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

            if (moveInput.x == 0 && moveInput.y == -1)
            {
                lastDownInputTime = Time.time;
            }

            if (CanMove)
                CalculateHorizontalMovement();
            else
                velocity.x = 0;
        }

        // --- 1. POLLING: NHẢY (JUMP) ---
        if (input.JumpWasPressedThisFrame)
        {
            if (CanMove || currentState == PlayerState.Talking)
            {
                lastJumpInputTime = Time.time;
            }
        }

        // NHẢ NÚT NHẢY (Biến thiên độ cao)
        if (input.JumpWasReleasedThisFrame)
        {
            if (velocity.y > 0)
            {
                velocity.y = velocity.y * variableJumpHeightMultiplier;
            }
        }

        // --- 2. POLLING: BẮN TAY MÓC (GRAPPLE) ---
        if (input.GrappleWasPressedThisFrame)
        {
            if (CanMove && currentState != PlayerState.GrappleFreeze && currentState != PlayerState.Grappling)
            {
                if (currentState == PlayerState.Idle && currentDialogueTrigger != null)
                {
                    currentDialogueTrigger.TriggerDialogue();
                }
                else
                {
                    if (!isGrounded && currentGrappleCount >= maxGrappleCount)
                    {
                        Debug.Log("All attempts to shoot grappling hand on air have been used up!");
                    }
                    else
                    {
                        if (!isGrounded) currentGrappleCount++;
                        StartCoroutine(PerformGrappleWithLeniency());
                    }
                }
            }
        }

        // --- KIỂM TRA INPUT NHẶT/THẢ ---
        if (!IsInputDisabled && input.GrabWasPressedThisFrame)
        {
            // TRƯỜNG HỢP 1: Đang cầm đồ -> Thả ra
            if (currentlyHeldObject != null)
            {
                DropObject();
            }
            // TRƯỜNG HỢP 2: Tay trống -> Bắn tia tìm đồ
            else
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                Vector2 origin = col.bounds.center;
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * facingDir, col.bounds.extents.x + grabReachDistance, collisionMask);

                if (hit && hit.collider.TryGetComponent(out IHoldable holdable))
                {
                    CatchObject(hit.collider.gameObject);
                }
            }
        }
    }

    void FixedUpdate()
    {
        wasGrounded = isGrounded;

        if (ignoreOneWayTimer > 0)
        {
            ignoreOneWayTimer -= Time.deltaTime;
        }

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

        if (!CanMove && currentState != PlayerState.Grappling) return;

        UpdateRayCastOrigin();

        hitLeftWall = false;
        hitRightWall = false;
        isWallSliding = false;
        hitCeiling = false;
        hitFloor = false;


        // Xử lí kéo tới mục tiêu khi đang bị móc (Grappling)
        if (currentState == PlayerState.Grappling)
        {
            velocity = Vector3.zero; // Không dùng vận tốc rớt/di chuyển thông thường

            if (grappleTarget != Vector2.zero)
            {
                // Dịch chuyển nhân vật lao vút tới mục tiêu
                Vector2 dir = (grappleTarget - (Vector2)transform.position).normalized;
                velocity = dir * pullSpeed; // Cấp vận tốc cực mạnh về phía mục tiêu
            }
        }

        else if (currentState == PlayerState.Climbing)
        {
            HandleWallClimbingPhysics();
        }
        else
        {
            float currentGravity = gravity;

            // 1. APEX HANG (Lơ lửng đỉnh)
            if (Mathf.Abs(velocity.y) < apexHangThreshold && input.IsJumping)
            {
                currentGravity *= apexHangMultiplier;
            }

            // 2. FAST FALL
            float mf = maxFallSpeed;
            float fmf = fastMaxFallSpeed;

            // NẾU: Đang đè phím Xuống VÀ Vận tốc rơi đã chạm ngưỡng Max Fall bình thường
            if (moveInput.x == 0 && moveInput.y == -1 && velocity.y <= mf)
            {
                // Từ từ nới rộng giới hạn rơi bằng FastMaxAccel
                currentMaxFall = Mathf.MoveTowards(currentMaxFall, fmf, fastMaxAccel * Time.fixedDeltaTime);
            }
            else
            {
                // Trả giới hạn rơi về bình thường
                currentMaxFall = Mathf.MoveTowards(currentMaxFall, mf, fastMaxAccel * Time.fixedDeltaTime);
            }

            // Áp dụng trọng lực: Ép vận tốc tiến về giới hạn currentMaxFall
            if (velocity.y > currentMaxFall)
            {
                velocity.y += currentGravity * Time.fixedDeltaTime;

                // Chặn không cho rớt lố giới hạn (phòng trường hợp trọng lực quá lớn)
                if (velocity.y < currentMaxFall) velocity.y = currentMaxFall;
            }
        }

        // 3. TRÍ NHỚ QUÁN TÍNH TƯỜNG (Wall Speed Retention)
        // Chạy ngay trước HorizonalCollisionCheck
        if (wallSpeedRetentionTimer > 0)
        {
            // Nếu bẻ lái ngược lại hoặc đang đang bay tới theo bàn tay -> Hủy trí nhớ
            if (moveInput.x != 0 && Mathf.Sign(moveInput.x) == -Mathf.Sign(wallSpeedRetained))
            {
                wallSpeedRetentionTimer = 0;
            }
            else 
            {
                // Kiểm tra xem phía trước mặt đã hết chướng ngại vật chưa
                float checkDir = Mathf.Sign(wallSpeedRetained);
                Vector2 originTop = (checkDir == 1) ? rayCastOrigins.topRight : rayCastOrigins.topLeft;
                Vector2 originBottom = (checkDir == 1) ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;
                
                bool hitWall = Physics2D.Raycast(originTop, Vector2.right * checkDir, skinWidth * 2, collisionMask) ||
                               Physics2D.Raycast(originBottom, Vector2.right * checkDir, skinWidth * 2, collisionMask);
                               
                if (!hitWall)
                {
                    velocity.x = wallSpeedRetained; // Trả lại vận tốc
                    wallSpeedRetentionTimer = 0;
                }
                else
                {
                    wallSpeedRetentionTimer -= Time.fixedDeltaTime; // Tiếp tục chờ
                }
            }
        }

        // KIỂM TRA LEO BẬC THỀM TRƯỚC
        if (CorrectStep(velocity))
        {
            // Nếu có leo bậc (bị đẩy lên cao), phải cập nhật lại gốc tia raycast ngay lập tức
            UpdateRayCastOrigin();
        }

        HorizonalCollisionCheck();

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

        // 4. JUMP THRU ASSIST (Trợ lực nhảy xuyên one-way platform)
        // Nếu đang bay lên hoặc ở đỉnh, và nằm CÙNG LÚC bên trong nền One-way
        if (velocity.y >= 0 && !isGrounded)
        {
            Collider2D jumpThruHit = Physics2D.OverlapBox(col.bounds.center, col.bounds.size, 0f, collisionMask);
            if (jumpThruHit != null && jumpThruHit.CompareTag("OneWay"))
            {
                // Đẩy người chơi trượt nhẹ lên trên để lọt qua nhanh hơn, chống kẹt
                transform.position += Vector3.up * jumpThruAssistSpeed * Time.fixedDeltaTime;
                rb.position = transform.position;
            }
        }

        bool isPullingBox = false;
        float pullAmount = 0f;

        // CODE ÉP HỘP CHẠY THEO PLAYER
        if (currentState == PlayerState.Pushing && currentPushable != null && velocity.x != 0)
        {
            float pushAmount = velocity.x * Time.fixedDeltaTime;

            Transform boxTransform = ((Component)currentPushable).transform;
            float dirToBox = Mathf.Sign(boxTransform.position.x - transform.position.x);
            bool isPushingBox = Mathf.Sign(velocity.x) == dirToBox;

            if (isPushingBox)
            {
                // --- TRƯỜNG HỢP 1: ĐANG ĐẨY (HỘP ĐI TRƯỚC) ---
                float actualBoxMove = currentPushable.TryMove(pushAmount);

                // Ép vận tốc người chơi BẰNG CHÍNH XÁC quãng đường hộp đã đi!
                velocity.x = actualBoxMove / Time.fixedDeltaTime;

                // Failsafe: Nếu hộp đụng tường (không đi được), tự động bật cờ leo
                if (Mathf.Abs(actualBoxMove) < 0.001f)
                {
                    if (velocity.x > 0 || moveInput.x > 0) hitRightWall = true;
                    if (velocity.x < 0 || moveInput.x < 0) hitLeftWall = true;
                }
            }
            else
            {
                // --- TRƯỜNG HỢP 2: ĐANG KÉO (NGƯỜI CHƠI SẼ ĐI TRƯỚC) ---
                isPullingBox = true;
                pullAmount = velocity.x * Time.fixedDeltaTime;
            }
        }

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

        // DI CHUYỂN PUSHABLE SAU (Nếu đang kéo)
        if (isPullingBox && currentPushable != null)
        {
            currentPushable.TryMove(pullAmount);
        }

        // Flip character
        HandleSpriteVisuals();

        // KIỂM TRA ĐIỀU KIỆN DỪNG GRAPPLE SAU KHI ĐÃ TÍNH TOÁN VA CHẠM XONG
        if (currentState == PlayerState.Grappling && grappleTarget != Vector2.zero)
        {
            // Khoảng cách còn lại (Dùng khoảng cách rất nhỏ để bù trừ frame-rate)
            float distToTarget = Vector2.Distance(transform.position, grappleTarget);

            // TÍNH TOÁN HƯỚNG BỊ KÉO CỦA Y
            Vector2 pullDir = (grappleTarget - (Vector2)transform.position).normalized;

            // CHỈ NGẮT DO SÀN NẾU ĐANG BỊ KÉO XUỐNG DƯỚI
            // (Dùng < -0.1f thay vì < 0 để tránh sai số dấu phẩy động khi bắn ngang hoàn toàn)
            bool isPullingDownward = pullDir.y < -0.1f;
            bool validFloorHit = hitFloor && isPullingDownward;

            // DỪNG LẠI NẾU: Tới nơi HOẶC Đập đầu vào tường/đất giữa chừng do Collision Check phát hiện
            if (distToTarget <= (pullSpeed * Time.fixedDeltaTime) || hitLeftWall || hitRightWall || hitCeiling || validFloorHit)
            {
                if (targetGrappleNode != null)
                {
                    // TỚI ĐÍCH LÀ NODE -> BUNG LÊN
                    velocity.y = targetGrappleNode.boostForce;
                    velocity.x = 0;
                    currentGrappleCount--; // Tăng thêm 1 lần phóng tay
                    SetState(PlayerState.OnAir);
                    isGrounded = false;
                    targetGrappleNode = null;
                    OnJumpPerformed?.Invoke();
                }
                else
                {
                    // ĐỤNG TƯỜNG HOẶC TỚI NƠI -> BÁM TƯỜNG (Hoặc Rớt xuống nếu tường không bám được)
                    SetState(PlayerState.OnAir);
                    if (!validFloorHit) isGrounded = false;
                    velocity = Vector3.zero; // Xóa quán tính bay

                    // FIX: Ép xóa sạch trí nhớ tường để tránh bị bắn văng ở frame sau
                    wallSpeedRetentionTimer = 0;

                    // Đảm bảo không đẩy vào tường nếu đang dừng vì đụng sàn hoặc trần
                    if (!validFloorHit && !hitCeiling)
                    {
                        // Ép lún thêm 1 pixel để frame sau hàm CheckWallGrab chắc chắn bắt được
                        if (hitLeftWall || hitRightWall)
                            transform.position += Vector3.right * wallDirX * skinWidth;
                    }

                    rb.position = transform.position;
                }

                grappleTarget = Vector2.zero;
                if (robotHand != null) robotHand.FinishGrapple();
            }
        }
    }

    private void HandleSpriteVisuals()
    {
        Vector3 finalScale = transform.localScale;

        // --- 1. XỬ LÝ LẬT MẶT (X) ---
        if (isWallSliding || isWallClimbing)
        {
            if (wallDirX != 0)
                finalScale.x = baseScale.x * wallDirX; // Lật mặt ra ngoài hoặc úp vào tường
        }
        else if (moveInput.x != 0 && wallJumpInputTimer <= 0 && CanMove)
        {
            finalScale.x = baseScale.x * Mathf.Sign(moveInput.x); // Lật theo hướng di chuyển
        }
        else
        {
            finalScale.x = baseScale.x * Mathf.Sign(finalScale.x); // Đứng yên thì giữ nguyên hướng mặt hiện tại
        }

        // --- 2. XỬ LÝ SQUASH & STRETCH FAST FALL (Y) ---
        // Nếu đang rơi nhanh và đè phím xuống
        if (velocity.y < 0 && moveInput.y < -0.1f)
        {
            // Tính tỷ lệ rơi từ MaxFallSpeed -> FastMaxFallSpeed (0 -> 1)
            float fallLerp = Mathf.InverseLerp(maxFallSpeed, fastMaxFallSpeed, velocity.y);
            fallLerp = Mathf.Clamp01(fallLerp); // Chặn trong khoảng [0, 1]

            // Ép bề ngang (X) nhỏ lại 50%, kéo bề dọc (Y) dài ra 150% so với BASE SCALE
            float scaleXMultiplier = Mathf.Lerp(1f, 0.5f, fallLerp);
            float scaleYMultiplier = Mathf.Lerp(1f, 1.5f, fallLerp);

            finalScale.x = Mathf.Sign(finalScale.x) * (baseScale.x * scaleXMultiplier); // Giữ nguyên dấu lật mặt
            finalScale.y = baseScale.y * scaleYMultiplier;
        }
        else
        {
            // Trả lại hình dáng bình thường dựa trên BASE SCALE thay vì số 1f
            finalScale.x = Mathf.Sign(finalScale.x) * baseScale.x;
            finalScale.y = baseScale.y;
        }

        transform.localScale = finalScale;
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

        // 1. Tính vận tốc mục tiêu (Thêm logic giảm tốc độ nếu đang đẩy)
        float speedMult = (currentState == PlayerState.Pushing) ? pushSpeedMultiplier : 1f;
        float targetSpeed = moveInput.x * moveSpeed * speedMult;

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
        SetState(PlayerState.ScriptedMove);

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
    public void ForceWalkIntoRoom(Vector2 pushVelocity, float duration, float cameraTime)
    {
        if (currentState == PlayerState.Dead || currentState == PlayerState.ScriptedMove) return;
        robotHand.FinishGrapple(); // Đảm bảo tay robot rút vào nếu đang móc
        StartCoroutine(RoomTransitionWalkRoutine(pushVelocity, duration, cameraTime));
    }

    private IEnumerator RoomTransitionWalkRoutine(Vector2 pushVelocity, float duration, float cameraTime)
    {
        // Khóa hoàn toàn Input của người chơi
        SetState(PlayerState.ScriptedMove);

        // Bật animation chạy nếu đi ngang
        if (pushVelocity.x != 0) OnRunChanged?.Invoke(true);

        // 1. Đẩy nhân vật vào trong (0.2 giây)
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            transform.position += (Vector3)pushVelocity * Time.deltaTime;
            if (rb != null) rb.position = transform.position;

            if (pushVelocity.x != 0)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(pushVelocity.x);
                transform.localScale = scale;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 2. Chờ Camera lia tới
        // Nhân vật đã lọt vào phòng, tắt animation chạy để đứng Idle
        if (pushVelocity.x != 0) OnRunChanged?.Invoke(false);
        velocity = Vector3.zero; // Xóa sạch quán tính bị đẩy

        // Tính toán xem Camera còn phải quay bao lâu nữa thì tới nơi
        float waitTimeForCamera = Mathf.Max(0f, cameraTime - duration);
        if (waitTimeForCamera > 0)
        {
            yield return new WaitForSeconds(waitTimeForCamera); // Chỉ dùng WaitForSeconds bình thường, không đụng TimeScale
        }

        // 3. Thêm thời gian nghỉ để người chơi quan sát phòng
        // Màn hình đã cố định, cho người chơi thêm 0.15s nhìn ngắm bố cục bẫy
        //yield return new WaitForSeconds(0.15f);

        // 4. Ép nhảy (Nếu đi lên) và trả lại quyền di chuyển cho người chơi
        // Nếu nhảy từ dưới lên trên -> Ép một lực nhảy
        if (pushVelocity.y > 0)
        {
            velocity.y = jumpForce;
            OnJumpPerformed?.Invoke();
        }
        else if (pushVelocity.y < 0) // Rớt từ trên xuống
        {
            velocity.y = 0;
        }

        // Mở khóa Input!
        SetState(PlayerState.Idle);
        Debug.Log("Room transition complete. Player control returned.");
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
        // 1. Kiểm tra xem có lệnh nhảy hợp lệ đang chờ không
        bool hasBufferedJump = (Time.time - lastJumpInputTime <= jumpBufferTime);

        // NẾU KHÔNG CÓ LỆNH NHẢY HOẶC KHÔNG ĐƯỢC PHÉP NHẢY -> Dừng ngay
        if (!hasBufferedJump || !CanJump) return;

        // ƯU TIÊN 1: RỚT XUỐNG SÀN (Có Input Forgiveness 0.05s)
        // Chuẩn hóa tuyệt đối: Hướng phải BẰNG 0 (không ấn trái/phải) và Y BẰNG -1 (Kéo gạt hướng xuống dưới)
        bool isTryingToDrop = (moveInput.x == 0 && moveInput.y == -1) || (Time.time - lastDownInputTime <= downInputBufferTime);

        if (isTryingToDrop && isGrounded)
        {
            // CHỈ CHO RỚT NẾU THỰC SỰ ĐỨNG TRÊN ONE-WAY
            if (isStandingOnOneWay)
            {
                ignoreOneWayTimer = oneWayDropGhostTime;
                lastJumpInputTime = -999f;
                return; // Xử lý xong, thoát hàm
            }
            // Nếu đứng trên đất cứng, nó sẽ bỏ qua lệnh Drop này và chạy thẳng xuống lệnh Nhảy Thường bên dưới!
        }

        // ƯU TIÊN 2: NHẢY THƯỜNG (Từ mặt đất)
        if (Time.time - lastGroundedTime <= coyoteTime)
        {
            // Nếu người chơi đang đẩy/kéo hộp mà bấm nhảy -> Tự động buông tay!
            if (currentState == PlayerState.Pushing)
            {
                SetState(PlayerState.Idle);
                Debug.Log("Player released pushable object. Idle state set");
                currentPushable = null;
            }

            Jump(false, 0, false);
            lastJumpInputTime = -999f;
            return; // Thoát hàm!
        }

        // ƯU TIÊN 3: NHẢY TƯỜNG (Wall Jump)
        // (Lưu ý: Check nhảy tường trước nhảy đất để tránh lỗi khi đứng sát tường ở dưới đất)
        if (Time.time - lastWallTouchTime <= wallCoyoteTime)
        {
            bool isHoldingTowardsWall = (moveInput.x != 0 && Mathf.Sign(moveInput.x) == lastWallDirX);
            bool isHoldingUp = (moveInput.y == 1);
            bool isNotHolding = moveInput.magnitude < 0.1f;
            bool isNotHoldingHorizontal = Mathf.Abs(moveInput.x) < 0.1f;

            if (input.IsGrabbing && (isHoldingTowardsWall || isHoldingUp || isNotHolding) && currentStamina > 0)
            {
                Jump(true, lastWallDirX, true); // Climb Jump
            }
            else if (isNotHoldingHorizontal)
            {
                Jump(true, lastWallDirX, false, true); // Neutral Jump
                Debug.Log("Neutral Jump");
            }
            else
            {
                Jump(true, lastWallDirX, false); // Wall Leap
            }

            lastJumpInputTime = -999f;
            lastWallTouchTime = -999f;
            return; // Thoát hàm!
        }

        // ƯU TIÊN 4: NHẢY TRÊN KHÔNG (Air Jump / Double Jump)
        if (currentAirJumps < maxAirJumps)
        {
            currentAirJumps++;
            Jump(false, 0, false);
            lastJumpInputTime = -999f;
            //Debug.Log($"Air Jump performed! Performed {currentAirJumps}/{maxAirJumps} attempts.");
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

            if (moveInput.x != 0)
            {
                // Cộng thêm một lực đẩy ngang vào quán tính hiện tại
                velocity.x += jumpHorizontalBoost * moveInput.x;

                //Khóa không cho vượt quá tốc độ chạy tối đa (để tránh lạm dụng nhảy liên tục để tăng tốc)
                if (Mathf.Abs(velocity.x) > moveSpeed)
                {
                    velocity.x = moveSpeed * Mathf.Sign(velocity.x);
                }
            }
        }

        // Cập nhật trạng thái
        SetState(PlayerState.OnAir);
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
            if (moveInput.y == 1)
            {
                velocity.y = moveInput.y * wallClimbSpeed;
                CheckForLedge(); // Hàm này có thể dùng để gọi ClimbHop() khi đã leo hẳn lên mép
            }
            else if (moveInput.y == -1)
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
            SetState(PlayerState.OnAir);
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
            SetState(PlayerState.Climbing);
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

        SetState(PlayerState.OnAir);
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
        SetState(PlayerState.GrappleFreeze);
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
        SetState(PlayerState.Grappling);
        if (robotHand != null)
        {
            robotHand.Shoot(shootDir, this);
        }
    }

    // RobotHand gọi hàm này khi nó cắm trúng vách tường
    public void StartPullingToHand(Vector2 pullDelta, Transform wallHit, GrappleNode node = null)
    {
        // Vị trí đích = Vị trí hiện tại của Player + Quãng đường bay của tay
        grappleTarget = (Vector2)transform.position + pullDelta;
        //Debug.Log("Pull delta: " + pullDelta + ", Target grapple position: " + grappleTarget);

        // Hướng bay để ép mặt vào tường
        wallDirX = (pullDelta.x > 0) ? 1 : -1;
        targetGrappleNode = node; // LƯU LẠI NODE
    }

    // RobotHand gọi hàm này khi cắm trượt (bay hết cỡ không trúng gì)
    public void CancelGrapple()
    {
        // Hủy trạng thái Grapple để nhân vật rơi xuống lại
        if (currentState == PlayerState.Grappling || currentState == PlayerState.GrappleFreeze)
        {
            SetState(PlayerState.OnAir);
            grappleTarget = Vector2.zero;
        }
    }

    #endregion

    #region Holdable Object Handling
    public void CatchObject(GameObject obj)
    {
        IHoldable holdable = obj.GetComponent<IHoldable>();
        if (holdable != null)
        {
            currentlyHeldObject = holdable;
            holdable.OnPickedUp(holdSlot);
            // Chuyển sang animation "Ôm người" ở đây
            // animator.SetBool("isHolding", true);
        }
    }

    public void DropObject()
    {
        if (currentlyHeldObject != null)
        {
            // Gọi hàm xử lý vật lý bên trong vật thể (trả về Dynamic, thoát khỏi cha)
            currentlyHeldObject.OnDropped();
            currentlyHeldObject = null;

            // animator.SetBool("isHolding", false);
            Debug.Log("Đã thả vật thể xuống!");
        }
    }
    #endregion

    #region Push/pull mechanic handling
    private void CheckPushPull()
    {
        // 1. ĐIỀU KIỆN THOÁT STATE PUSHING
        if (currentState == PlayerState.Pushing)
        {
            // Nếu thả nút bám, rớt khỏi mặt đất, mất hộp, HOẶC NGƯỜI CHƠI BẤM LÊN (Muốn leo lên hộp)
            if (!input.IsGrabbing || !isGrounded || currentPushable == null || moveInput.y == 1)
            {
                currentPushable = null;
                SetState(PlayerState.Idle);
                Debug.Log("Player released pushable object. Idle state set");
                return;
            }
        }

        // 2. ĐIỀU KIỆN VÀO STATE PUSHING
        // Chú ý: Chỉ được phép vào trạng thái đẩy nếu KHÔNG BẤM LÊN (moveInput.y != 1)
        if (isGrounded && input.IsGrabbing && currentState != PlayerState.Pushing && currentStamina > 0 && moveInput.y != 1)
        {
            float facingDir = Mathf.Sign(transform.localScale.x);
            Vector2 origin = col.bounds.center;

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * facingDir, col.bounds.extents.x + grabReachDistance, collisionMask);

            if (hit && hit.collider.TryGetComponent(out IPushable pushable))
            {
                SetState(PlayerState.Pushing);
                currentPushable = pushable;
                velocity.x = 0;
            }
        }
    }
    #endregion

    #region Spring Handling
    /// <summary>
    /// Called by Spring when player hits it. It will directly set the player's velocity to the bounce velocity on a certain direction provided by the spring.
    /// </summary>
    public void ApplyBounce(Vector2 bounceVelocity)
    {
        // 1. GHI ĐÈ TOÀN BỘ VẬN TỐC
        // Ghi đè cả X và Y giúp quỹ đạo bay chuẩn xác 100% theo hướng lò xo chỉ định,
        // triệt tiêu hoàn toàn quán tính chạy hoặc rơi trước đó của người chơi.
        velocity = bounceVelocity;

        // 2. CẬP NHẬT TRẠNG THÁI
        SetState(PlayerState.OnAir);
        isGrounded = false;
        isWallClimbing = false;
        isWallSliding = false;
        lastGroundedTime = -999f;

        // 3. XỬ LÝ SỰ KIỆN ANIMATION
        // Chỉ kích hoạt Animation "Jump" nếu lò xo đẩy hướng lên trên (Y > 0)
        // Nếu lò xo đẩy cắm đầu xuống đất (Y < 0), nhân vật nên giữ nguyên animation Rơi (Fall)
        if (bounceVelocity.y > 0)
        {
            OnJumpPerformed?.Invoke();
        }

        // Bắt buộc đẩy nhân vật văng ra khỏi lò xo một chút xíu (Tránh lỗi kẹt va chạm)
        // Khi bị đẩy ngang/xuống, nếu không nhích ra 1 pixel, tia raycast có thể vẫn kẹt trong lò xo
        transform.position += (Vector3)bounceVelocity.normalized * 0.05f;
        rb.position = transform.position;
    }
    #endregion
    #endregion

    #region State Machine
    //Set trạng thái của người chơi
    public void SetState(PlayerState newState)
    {
        // NẾU state cũ đang là Grappling/Freeze MÀ state mới bị ép đổi sang cái khác (vd: OnAir do dẫm lò xo)
        bool wasGrappling = (currentState == PlayerState.Grappling || currentState == PlayerState.GrappleFreeze);
        bool isChangingAway = (newState != PlayerState.Grappling && newState != PlayerState.GrappleFreeze);

        currentState = newState;

        if (wasGrappling && isChangingAway)
        {
            // Thu hồi tay ngay lập tức
            if (robotHand != null) robotHand.FinishGrapple();
            grappleTarget = Vector2.zero;
        }
    }

    public PlayerState GetCurrentState()
    {
        return currentState;
    }

    public bool CanMove
    {
        get
        {
            return currentState == PlayerState.Idle ||
                   currentState == PlayerState.Running ||
                   currentState == PlayerState.OnAir ||
                   currentState == PlayerState.Sliding ||
                   currentState == PlayerState.Pushing ||
                   currentState == PlayerState.Climbing ||
                   currentState == PlayerState.WaitingForCutscene;
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
                   currentState == PlayerState.ScriptedMove ||
                   currentState == PlayerState.WaitingForCutscene;
        }
    }

    // Chuẩn bị cho Cutscene: Lưu lại sequence đang chờ và chuyển sang state chờ cutscene
    public void PrepareCutscene(CutsceneSequence sequence)
    {
        pendingSequence = sequence;
        SetState(PlayerState.WaitingForCutscene);
        velocity.x = 0; // Dừng mọi chuyển động ngang khi chuẩn bị cutscene
    }

    private void UpdatePhysicsState()
    {
        // Kiểm tra nếu đang chờ rớt xuống đất để chạy Cutscene
        if (currentState == PlayerState.WaitingForCutscene)
        {
            if (isGrounded)
            {
                if (pendingSequence != null)
                {
                    pendingSequence.PlayCutscene();
                    pendingSequence = null;
                    // Lưu ý: Thông thường khối lệnh đầu tiên của Cutscene 
                    // sẽ gọi SetState(ScriptedMove) để giữ khóa Input.
                }
            }
            return; // Thoát sớm để không bị logic Idle/Run ghi đè State
        }

        // Nếu đang bận việc riêng (Talking/Dead) thì không can thiệp
        if (currentState == PlayerState.Talking || currentState == PlayerState.Dead
            || currentState == PlayerState.Grappling || currentState == PlayerState.GrappleFreeze
            || currentState == PlayerState.ScriptedMove) return;

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

        CheckPushPull();
        if (currentState == PlayerState.Pushing) return; // Nếu đang đẩy hộp, không chạy logic chuyển sang state khác nữa

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
                SetState(PlayerState.Running);
            }
            else
            {
                SetState(PlayerState.Idle);
                //Debug.Log("Player stop moving. Idle state set");
            }
        }
        else
        {
            // Cập nhật trạng thái trượt tường cho Animation
            if (isWallSliding)
            {
                SetState(PlayerState.Sliding);
            }
            else
            {
                SetState(PlayerState.OnAir);
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

        // Xử lý Event Đẩy / Kéo hộp
        if (currentState == PlayerState.Pushing)
        {
            // Hướng mặt của nhân vật (1 là phải, -1 là trái)
            float facingDir = Mathf.Sign(transform.localScale.x);

            // Hướng di chuyển từ Input
            float moveDir = Mathf.Sign(moveInput.x);

            // NẾU HƯỚNG MẶT CÙNG CHIỀU VỚI HƯỚNG ĐI -> ĐANG ĐẨY TỚI
            if (facingDir == moveDir && moveInput.x != 0)
            {
                // Báo cho Animator chạy Animation ĐẨY
                // OnPushingStateChanged?.Invoke(true, false); 
            }
            // NẾU HƯỚNG MẶT NGƯỢC CHIỀU VỚI HƯỚNG ĐI -> ĐANG KÉO LÙI
            else if (facingDir != moveDir && moveInput.x != 0)
            {
                // Báo cho Animator chạy Animation KÉO
                // OnPushingStateChanged?.Invoke(false, true);
            }
            else
            {
                // Đứng yên ôm hộp (Idle Push)
            }
        }
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
        Debug.Log("Emergency reset. Idle state set");
        isGrounded = false; // Reset để hàm VerticalCollisionCheck tự động tính lại vào frame sau

        // 6. Reset lại bộ đếm Watchdog
        currentStuckTimer = 0f;
    }

    // Xử lý Unity Timeline
    public void OnTimelineStart()
    {
        StopAllCoroutines(); // Dừng hết mọi Coroutine đang chạy (tránh xung đột với AutoWalk hoặc các hiệu ứng thời gian khác có thể ghi đè PlayerState)
        // Đưa vào nhóm State bị cấm Input và cấm Vật lý
        SetState(PlayerState.ScriptedMove);
        velocity = Vector3.zero;
    }

    public void OnTimelineEnd()
    {
        // Đánh thức hệ thống vật lý
        SetState(PlayerState.Idle);
        Debug.Log("Timeline ended. Idle state set");

        // Reset các trạng thái chạm đất để raycast tự cập nhật vào frame sau
        wasGrounded = false;
        isGrounded = false;
    }
    #endregion

    #region Room Transition Fail-safe
    private IEnumerator RoomFailSafeRoutine()
    {
        // Tạo biến đếm thời gian 1 giây để tối ưu bộ nhớ (không dùng new liên tục)
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (true)
        {
            yield return wait;

            // Nếu game đang Pause thì không cần kiểm tra để tiết kiệm tài nguyên
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause) continue;

            CheckCurrentRoomFailSafe();
        }
    }

    private void CheckCurrentRoomFailSafe()
    {
        // 1. Bắn tia kiểm tra xem TẠI VỊ TRÍ CHÍNH GIỮA của nhân vật đang có những Collider nào
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        RoomTransitionScript actualRoom = null;

        // 2. Lọc ra Collider thuộc về ranh giới phòng
        foreach (Collider2D col in colliders)
        {
            RoomTransitionScript room = col.GetComponent<RoomTransitionScript>();
            if (room != null)
            {
                actualRoom = room;
                break; // Tìm thấy phòng rồi thì dừng vòng lặp
            }
        }

        // 3. SO SÁNH VÀ SỬA SAI
        // Nếu tìm thấy phòng nhân vật đang đứng, VÀ phòng đó KHÁC với ActiveRoom hiện tại của hệ thống
        if (actualRoom != null && RoomTransitionScript.ActiveRoom != actualRoom)
        {
            // Lập tức gọi hàm ép chuyển Camera về đúng phòng!
            actualRoom.ForceRoomTransition(this);
        }
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
            isStandingOnOneWay = false;
        }

        Transform hitPlatform = null; // Biến tạm để lưu bệ đỡ nếu dẫm trúng trong frame này

        bool foundSolid = false;
        bool foundOneWay = false;

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

                    // 3. Đang rớt xuống trong thời gian xuyên qua One-way platform (ignoreOneWayTimer > 0) -> Bỏ qua va chạm
                    if (ignoreOneWayTimer > 0) continue;

                    foundOneWay = true; // Ghi nhận có chạm One-way Platform
                }

                else
                {
                    foundSolid = true; // Ghi nhận có chạm vào mặt đất bình thường (không phải One-way)
                }

                velocity.y = (hit.distance - skinWidth) * directionY / Time.fixedDeltaTime; // Tính lại vận tốc thực dựa trên quãng đường còn lại
                rayLength = hit.distance; // Thu ngắn tia lại để không check xuyên qua vật cản gần nhất

                if (directionY == -1) // Đang rơi xuống
                {
                    hitFloor = true;
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
                    hitCeiling = true;
                    velocity.y = 0; // Tùy chọn: Đụng trần thì dừng hẳn lực nảy
                }
            }
        }

        // Nếu dẫm lên One-way mà KHÔNG bị đè lên nền đất cứng (tránh lỗi mép lề)
        if (isGrounded)
        {
            isStandingOnOneWay = foundOneWay && !foundSolid;
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

                // LƯU LẠI VẬN TỐC TƯỜNG (Wall Speed Retention)
                // Nếu đang bay với tốc độ cực nhanh (Lớn hơn moveSpeed) và đập mặt vào tường góc hẹp
                // FIX: Không lưu trí nhớ tường nếu đang bị kéo bởi tay móc!
                if (Mathf.Abs(velocity.x) > moveSpeed && hit.distance > 0 && currentState != PlayerState.Grappling)
                {
                    if (wallSpeedRetentionTimer <= 0) // Chỉ ghi nhận lần chạm đầu tiên
                    {
                        wallSpeedRetained = velocity.x;
                        wallSpeedRetentionTimer = wallSpeedRetentionTime;
                    }
                }

                // BỎ QUA VA CHẠM VỚI HỘP NẾU ĐANG TRONG STATE PUSHING VÀ ĐẨY TỚI
                if (currentState == PlayerState.Pushing && hit.collider.TryGetComponent(out IPushable hitPushable))
                {
                    if (currentPushable == hitPushable && Mathf.Sign(directionX) == Mathf.Sign(moveInput.x))
                    {
                        continue;
                    }
                }

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

    // Hàm đẩy nhân vật lên khi vấp phải gờ đất nhỏ (Dịch chuyển tức thời - Pixel Perfect)
    bool CorrectStep(Vector2 moveVelocity)
    {
        // Chỉ quét khi nhân vật đang di chuyển ngang
        if (Mathf.Abs(moveVelocity.x) < 0.01f) return false;

        float directionX = Mathf.Sign(moveVelocity.x);

        // Bắn tia dài hơn vận tốc một chút để đoán trước va chạm
        float rayLength = Mathf.Abs(moveVelocity.x * Time.fixedDeltaTime) + skinWidth * 2;

        // Xác định góc dưới cùng (Trái hoặc Phải) tùy theo hướng đi
        Vector2 baseOrigin = (directionX == 1) ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;

        // 1. TIA DƯỚI (Bottom Ray): Nâng lên một chút để né mặt đất phẳng
        Vector2 bottomOrigin = baseOrigin + Vector2.up * bottomRayOffset;
        RaycastHit2D hitBottom = Physics2D.Raycast(bottomOrigin, Vector2.right * directionX, rayLength, collisionMask);

        // Bỏ qua nếu tia dưới chạm One-way platform (chỉ leo đất cứng)
        if (hitBottom && hitBottom.collider.CompareTag("OneWay")) hitBottom = default;

        // Nếu tia dưới không chạm gì -> Không có gờ đất -> Bỏ qua
        if (!hitBottom) return false;

        // 2. TIA TRÊN (Top Ray): Quét ở độ cao tối đa có thể leo
        Vector2 topOrigin = baseOrigin + Vector2.up * stepHeight;
        RaycastHit2D hitTop = Physics2D.Raycast(topOrigin, Vector2.right * directionX, rayLength, collisionMask);

        if (hitTop && hitTop.collider.CompareTag("OneWay")) hitTop = default;

        // LOGIC CHÍNH: Tia dưới đụng tường (Vấp ngón chân), nhưng Tia trên trống (Thoáng đầu gối)
        if (hitBottom && !hitTop)
        {
            // 3. TIA ĐO CHIỀU CAO (Downward Ray): Bắn từ trên đỉnh cắm xuống mặt bậc thềm
            // Tính toán X nhích vào bên trong bậc thềm một chút xíu (khoảng 0.02f)
            float stepInnerX = baseOrigin.x + directionX * (hitBottom.distance + 0.02f);
            Vector2 stepTopOrigin = new Vector2(stepInnerX, topOrigin.y);

            // Bắn tia cắm xuống dưới để tìm bề mặt của bậc thềm
            RaycastHit2D hitDown = Physics2D.Raycast(stepTopOrigin, Vector2.down, stepHeight, collisionMask);

            if (hitDown && !hitDown.collider.CompareTag("OneWay"))
            {
                // Tính toán chính xác khoảng cách cần phải nhấc chân lên
                float exactStepHeight = hitDown.point.y - baseOrigin.y;

                // Dịch chuyển TỨC THỜI nhân vật lên bằng đúng chiều cao bậc thềm + bù sai số skinWidth
                transform.position += Vector3.up * (exactStepHeight + skinWidth) + Vector3.right * directionX * skinWidth;
                rb.position = transform.position; // Đồng bộ với Rigidbody

                return true; // Báo hiệu đã xử lý leo bậc thành công
            }
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

            // Bỏ qua va chạm với Pushable đang cầm để chống giật khi kéo!
            if (currentState == PlayerState.Pushing && currentPushable != null)
            {
                if (results[i].transform == ((Component)currentPushable).transform) continue;
            }

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

                    //Debug.Log("OVerlap Position Before: " + transform.position + ", Separation Vector: " + separationVector);
                    transform.position += (Vector3)separationVector;
                    rb.position = (Vector2)transform.position;
                    Debug.Log("Resolved Overlap");
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
    private void InitializeClosestCheckpoint()
    {
        // Tìm tất cả các checkpoint có trong level hiện tại. Chọn chế độ không sắp xếp để tối ưu hiệu năng
        Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

        if (allCheckpoints.Length == 0) return;

        Checkpoint closestCheckpoint = null;
        float minDistance = Mathf.Infinity;

        // Duyệt qua 1 lần duy nhất để tìm điểm gần nhất
        foreach (Checkpoint cp in allCheckpoints)
        {
            float distance = Vector3.Distance(transform.position, cp.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCheckpoint = cp;
            }
        }

        // Gán điểm hồi sinh
        if (closestCheckpoint != null)
        {
            SetRespawnPoint(closestCheckpoint.transform.position);
        }
    }
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
        yield return new WaitForSeconds(TransitionManager.Instance.transitionTime);

        // ANIMATION START đã xong, giờ mới cho nhân vật hiện lại và hồi sinh thực sự

        if (currentRoom != null) currentRoom.OnRoomEntered(); // Reset trạng thái phòng nếu cần


        // 1. Dịch chuyển nhân vật về điểm Checkpoint
        transform.position = currentRespawnPoint;
        if (rb != null) rb.position = currentRespawnPoint; // Cập nhật cả Rigidbody cho chắc chắn
        lastSpawnTime = Time.unscaledTime;

        // 2. Reset lại các chỉ số quan trọng
        velocity = Vector3.zero;
        currentStamina = maxStamina; // Hồi đầy thể lực
        isGrounded = false; // Bắt buộc false để tính toán lại chạm đất an toàn

        // Nếu tay móc đang bắn ra, thu hồi lại
        if (robotHand != null) robotHand.FinishGrapple();

        // 3. Bật lại hình ảnh nhân vật
        GetComponentInChildren<SpriteRenderer>().enabled = true;

        // Delay một chút để đủ tổng thời gian 1 giây
        //yield return new WaitForSeconds(0.4f);

        // KÍCH HOẠT ANIMATION END
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.PlayEndTransition();
        }

        // 4. Trả lại quyền điều khiển
        SetState(PlayerState.Idle);
        Debug.Log("Player respawned. Idle state set.");
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

        Vector2 bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        Vector2 bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        DrawStepCorrectionRays(bottomLeft, bottomRight, direction);

        Vector2 topLeft = new Vector2(bounds.min.x, bounds.max.y);
        Vector2 topRight = new Vector2(bounds.max.x, bounds.max.y);
        DrawCeilingCorrectionRays(topLeft, topRight);

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

    void DrawStepCorrectionRays(Vector2 bottomLeft, Vector2 bottomRight, float direction)
    {
        float rayLength = skinWidth * 5; // Độ dài tia (chỉ để nhìn cho rõ)
        Vector2 baseOrigin = (direction == 1) ? bottomRight : bottomLeft;

        Vector2 bottomOrigin = baseOrigin + Vector2.up * bottomRayOffset;
        Vector2 topOrigin = baseOrigin + Vector2.up * stepHeight;

        // Vẽ tia dưới (Màu Đỏ - Mong muốn chạm vách)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bottomOrigin, bottomOrigin + Vector2.right * direction * rayLength);
        Gizmos.DrawSphere(bottomOrigin, 0.015f);

        // Vẽ tia trên (Màu Xanh lá - Mong muốn không chạm)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(topOrigin, topOrigin + Vector2.right * direction * rayLength);
        Gizmos.DrawSphere(topOrigin, 0.015f);

        // Nối 2 tia lại để thấy rõ chiều cao của bậc thềm
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bottomOrigin, topOrigin);
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