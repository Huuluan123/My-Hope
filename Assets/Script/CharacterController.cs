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

    GrappleFreeze,
    Grappling,

    /* Nhóm khoá input */
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
    [SerializeField] private float airDeceleration = 10f; // Ma sát trên không (Thấp hơn để bay lượn mượt hơn)

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
    private int wallDirX; // Hướng tường (1: bên phải, -1: bên trái)

    [Space]

    [SerializeField] private float wallSlideSpeed = 2f; // Tốc độ trượt xuống chậm
    [SerializeField] private float wallClimbSpeed = 4f; // Tốc độ leo lên/xuống
    [SerializeField] private float wallClimbDownSpeed = 8f; // Tốc độ trượt tường nhanh khi bám
    [SerializeField] private Vector2 wallJumpForce = new Vector2(15f, 25f); // Lực nhảy tường (X, Y)
    [SerializeField] private float wallClimbJumpForceY = 20f; // Nhảy leo thẳng lên (Chỉ cần lực Y)
    private float climbJumpCooldownTimer;

    [Space]

    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float climbStaminaDrain = 20f; // Tốn thể lực khi leo
    [SerializeField] private float holdStaminaDrain = 5f;   // Tốn ít hơn khi chỉ bám yên
    [SerializeField] private float currentStamina;

    [Header("Grappling Hand")]
    [SerializeField] private RobotHand robotHand;
    [SerializeField] private float pullSpeed = 30f;
    [SerializeField] private float grappleInputWindow = 0.05f; // Cửa sổ chờ Input (0.05s)
    private Vector2 grappleTarget;

    [Header("Climb hop Settings")]
    [SerializeField] private Vector2 climbHopForce = new Vector2(10f, 15f); // Lực đẩy lên sàn (X nhỏ, Y lớn)
    [SerializeField] private float climbHopNoInputTime = 0.2f; // Thời gian khóa Input ngang để nhân vật lọt vào sàn
    private float climbHopInputTimer; // Timer đếm ngược

    [Header("Corner Correction")]
    [SerializeField] private float cornerCorrectionSpeed = 0.1f; // Khoảng cách đẩy lên mỗi frame (Pixel perfect)
    [SerializeField] private float ceilingTolerance = 0.15f; // Độ rộng tối đa của góc có thể lách qua (Tùy theo chiều rộng nhân vật)
    [SerializeField] private float ceilingNudgeAmount = 0.2f;

    [Header("Movement and control")]
    // Biến để lưu trữ giá trị di chuyển
    [SerializeField] private PlayerState currentState;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private bool isGrounded; // Biến kiểm tra nhân vật có đang chạm đất không
    [SerializeField] private bool hitLeftWall;
    [SerializeField] private bool hitRightWall;

    [Space]
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float jumpTime;

    public Vector2 Velocity
    {
        get { return velocity; }
    }

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
    #endregion

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

        if (climbHopInputTimer > 0)
        {
            climbHopInputTimer -= Time.deltaTime;
        }

        // Giảm timer cấm bám tường (nếu lớn hơn 0)
        if (climbJumpCooldownTimer > 0)
        {
            climbJumpCooldownTimer -= Time.deltaTime;
        }

        if (IsInputDisabled)
        {
            moveInput = Vector2.zero;
        }

        else
        {
            // Nếu được phép điều khiển:
            moveInput = (input != null) ? input.MoveInput : Vector2.zero;

            if (CanMove)
                CalculateHorizontalMovement();
            else
                velocity.x = 0;
        }
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
                    // Chuyển ngay sang bám tường
                    currentState = PlayerState.Climbing;
                    isWallClimbing = true;
                    isWallSliding = false;
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

        if (!CorrectCorner(velocity))
        {
            HorizonalCollisionCheck();
        }

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
        Vector3 targetPosition = (Vector3)rb.position + moveAmount;
        rb.MovePosition(targetPosition);

        // Flip character
        if (moveInput.x != 0 && !isWallClimbing)
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

    // Tính toán vận tốc ngang
    private void CalculateHorizontalMovement()
    {
        // Khoá di chuyển ngang khi đang leo tường.
        if (isWallClimbing)
        {
            velocity.x = 0;
            return; // Thoát hàm ngay, không chạy logic bên dưới
        }

        if (climbHopInputTimer > 0)
        {
            // Ép nhân vật di chuyển theo hướng Hop (để chắc chắn leo lọt vào sàn)
            // Hoặc đơn giản là giữ nguyên velocity.x hiện tại do quán tính
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

    #region Jump Handling
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

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
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
            // ƯU TIÊN 2: NHẢY TƯỜNG (Trên không + Sát tường)
            else if (hitLeftWall || hitRightWall)
            {
                int currentWallDir = hitLeftWall ? -1 : 1;

                // Kiểm tra xem người chơi có đang bấm nút NGƯỢC hướng với tường không?
                // (VD: Tường bên trái (-1) mà đang bấm sang phải (1) -> isHoldingAway = true)
                bool isHoldingInputAwayFromWall = (moveInput.x != 0 && Mathf.Sign(moveInput.x) != currentWallDir);

                // NẾU: Đang trạng thái bám tường (Climbing) VÀ KHÔNG bấm ngược hướng
                if (input.IsGrabbing && !isHoldingInputAwayFromWall && currentStamina > 0)
                {
                    // Thực hiện Nhảy dọc tường (Wall Climb Jump)
                    Jump(true, currentWallDir, true);
                }
                else
                {
                    // Các trường hợp khác: Đang rơi, trượt, hoặc ấn ngược hướng
                    // Thực hiện Nhảy bật ra (Wall Leap)
                    Jump(true, currentWallDir, false);
                }

                lastJumpInputTime = -999f;
            }
        }
    }

    private void Jump(bool isWallJump, int wallDir, bool isClimbJump)
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
            }
            else
            {
                velocity.x = -wallDir * wallJumpForce.x;
                velocity.y = wallJumpForce.y;

                //currentStamina -= 10f;

            }
        }
        else
        // ---------------------------------
        {
            // Normal Jump
            velocity.y = jumpForce;
        }

        // Cập nhật trạng thái
        currentState = PlayerState.OnAir;
        isGrounded = false;
        isWallClimbing = false; // Ngắt trạng thái leo ngay khi nhảy
        isWallSliding = false;

        lastGroundedTime = -999f; // Reset ground time

        SoundManager.Instance.PlaySound2D("Jump");
        OnJumpPerformed?.Invoke();
    }
    #endregion

    #region Wall Climbing Handling
    private void HandleWallClimbingPhysics() // Gọi trong MoveCharacter()
    {
        // 1. Áp dụng vận tốc leo dựa trên input Y
        if (moveInput.y > 0)
        {
            // Đang bấm LÊN -> Dùng tốc độ leo lên
            velocity.y = moveInput.y * wallClimbSpeed;
            CheckForLedge();
        }
        else if (moveInput.y < 0)
        {
            // Đang bấm XUỐNG -> Dùng tốc độ trượt xuống nhanh (Fast Drop)
            velocity.y = moveInput.y * wallClimbDownSpeed;
        }
        else
        {
            // Không bấm gì -> Đứng yên trên tường
            velocity.y = 0;
        }

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
        // Nếu đang trong thời gian Cooldown sau khi nhảy dọc tường -> Cấm bám
        if (climbJumpCooldownTimer > 0)
        {
            isWallClimbing = false;
            return;
        }

        // Điều kiện: Đang giữ phím Grab + Chạm tường + Không ở mặt đất + Còn thể lực
        bool isTouchingWall = hitLeftWall || hitRightWall;

        if (input.IsGrabbing && isTouchingWall && currentStamina > 0)
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
        // 1. Xác định hướng đẩy (Vào trong tường)
        // Nếu bám tường phải (wallDirX = 1) -> Cần đẩy sang trái? KHÔNG!
        // Climb Hop là leo LÊN sàn, nên ta cần đẩy sâu vào hướng của tường.
        // Ví dụ: Tường bên Phải -> Đẩy sang Phải + Lên Trên.
        int hopDirection = wallDirX;

        // 2. Gán vận tốc vật lý (Tạo cú nhảy tự động)
        velocity.x = hopDirection * climbHopForce.x;
        velocity.y = climbHopForce.y;

        // 3. Chuyển trạng thái về OnAir (Để hệ thống vật lý tự lo phần còn lại)
        currentState = PlayerState.OnAir;
        isWallClimbing = false;
        isWallSliding = false;
        isGrounded = false;

        // 4. Khóa Input ngang trong 0.2s
        // Để người chơi không lỡ tay bấm ngược lại làm nhân vật rớt xuống
        climbHopInputTimer = climbHopNoInputTime;

        // 5. Reset các trạng thái khác
        lastGroundedTime = -999f;

        OnJumpPerformed?.Invoke();
        //Debug.Log("Climb Hop Performed!");
    }
    #endregion

    #region Grabbling Handling
    private void OnGrappleInput(InputAction.CallbackContext context)
    {
        // Chặn nếu đang bận, đang dừng hình, hoặc đang bị kéo
        if (!CanMove || currentState == PlayerState.GrappleFreeze || currentState == PlayerState.Grappling) return;

        if (context.performed)
        {
            StartCoroutine(PerformGrappleWithLeniency());
        }
    }

    private IEnumerator PerformGrappleWithLeniency()
    {
        // 1. ĐÓNG BĂNG VẬT LÝ VÀ CHUYỂN TRẠNG THÁI
        currentState = PlayerState.GrappleFreeze;
        velocity = Vector3.zero; // Dừng toàn bộ vận tốc (trọng lực, quán tính)

        // 2. CHỜ 0.05s ĐỂ NGƯỜI CHƠI NHẬP HƯỚNG
        yield return new WaitForSecondsRealtime(grappleInputWindow);

        // 3. ĐỌC HƯỚNG BẮN
        Vector2 shootDir = Vector2.zero;

        // Nếu có bấm phím điều hướng (Analog hoặc D-pad)
        if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
        {
            // Ép hướng về 8 hướng cơ bản (Lên, Xuống, Trái, Phải và 4 góc chéo)
            shootDir = new Vector2(Mathf.Sign(moveInput.x) * Mathf.Ceil(Mathf.Abs(moveInput.x)),
                                   Mathf.Sign(moveInput.y) * Mathf.Ceil(Mathf.Abs(moveInput.y))).normalized;
        }
        else
        {
            // Mặc định bắn ngang theo hướng mặt nhân vật đang nhìn
            shootDir = new Vector2(Mathf.Sign(transform.localScale.x), 0);
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
        bool isClimbing = (currentState == PlayerState.Climbing && moveInput.y > 0.1f);
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

        if (directionY == -1 || velocity.y > 0)
        {
            isGrounded = false;
        }

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

                //Phát hiện tường
                wallDirX = (directionX == 1) ? 1 : -1;
                // Trạng thái trượt tường: Đang chạm tường, đang rơi và người chơi đang ấn phím hướng vào tường
                bool isPressingAgainstWall = (moveInput.x > 0 && wallDirX == 1) || (moveInput.x < 0 && wallDirX == -1);

                if (!isGrounded && velocity.y < 0 && isPressingAgainstWall)
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

        // Nếu đầu va phải tường -> Không phải là góc -> Không sửa được
        if (hitHead) return false;

        // 2. TIA THÂN (Body Ray): Kiểm tra xem vai/ngực có bị vướng không?
        // Hạ thấp xuống một đoạn nhỏ (bạn có thể tạo biến cornerDetectOffset)
        Vector2 bodyOrigin = origin + Vector2.down * 0.1f;
        RaycastHit2D hitBody = Physics2D.Raycast(bodyOrigin, Vector2.right * directionX, rayLength, collisionMask);

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