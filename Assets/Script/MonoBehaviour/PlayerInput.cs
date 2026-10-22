using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction grappleAction;
    public InputAction attackAction;
    public InputAction grabAction;

    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        grappleAction = InputSystem.actions.FindAction("Grapple");
        attackAction = InputSystem.actions.FindAction("Attack");
        grabAction = InputSystem.actions.FindAction("Grab");
    }

    // --- CÁC BIẾN TRẠNG THÁI (Giữ đè) ---
    public Vector2 MoveInput => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public bool IsGrabbing => grabAction != null && grabAction.IsPressed();
    public bool IsJumping => jumpAction != null && jumpAction.IsPressed();

    // --- CÁC BIẾN POLLING (Chỉ True trong 1 Frame) ---
    public bool JumpWasPressedThisFrame => jumpAction != null && jumpAction.WasPressedThisFrame();
    public bool JumpWasReleasedThisFrame => jumpAction != null && jumpAction.WasReleasedThisFrame();
    public bool GrappleWasPressedThisFrame => grappleAction != null && grappleAction.WasPressedThisFrame();
    public bool GrabWasPressedThisFrame => grabAction != null && grabAction.WasPressedThisFrame();
    public bool AttackWasPressedThisFrame => attackAction != null && attackAction.WasPressedThisFrame();

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        grappleAction?.Enable();
        attackAction?.Enable();
        grabAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        grappleAction?.Disable();
        attackAction?.Disable();
        grabAction?.Disable();
    }
}