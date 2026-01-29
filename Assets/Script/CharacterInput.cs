using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction dashAction;
    public InputAction attackAction;
    public InputAction grabAction;

    public event Action<InputAction.CallbackContext> JumpPerformed;
    public event Action<InputAction.CallbackContext> JumpCanceled;
    public event Action<InputAction.CallbackContext> DashPerformed;
    public event Action<InputAction.CallbackContext> AttackPerformed;
    public event Action<InputAction.CallbackContext> GrabPerformed;

    private Action<InputAction.CallbackContext> jumpPerformedHandler;
    private Action<InputAction.CallbackContext> jumpCanceledHandler;
    private Action<InputAction.CallbackContext> dashPerformedHandler;
    private Action<InputAction.CallbackContext> attackPerformedHandler;
    private Action<InputAction.CallbackContext> grabPerformedHandler;

    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
        grabAction = InputSystem.actions.FindAction("Grab");
    }

    public Vector2 MoveInput => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public bool IsGrabbing => grabAction != null && grabAction.IsPressed();

    void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpPerformedHandler = ctx => JumpPerformed?.Invoke(ctx);
            jumpCanceledHandler = ctx => JumpCanceled?.Invoke(ctx);
            jumpAction.performed += jumpPerformedHandler;
            jumpAction.canceled += jumpCanceledHandler;
        }

        if (dashAction != null)
        {
            dashAction.Enable();
            dashPerformedHandler = ctx => DashPerformed?.Invoke(ctx);
            dashAction.performed += dashPerformedHandler;
        }

        if (attackAction != null)
        {
            attackAction.Enable();
            attackPerformedHandler = ctx => AttackPerformed?.Invoke(ctx);
            attackAction.performed += attackPerformedHandler;
        }

        if (grabAction != null)
        {
            grabAction.Enable();
            grabPerformedHandler = ctx => GrabPerformed?.Invoke(ctx);
            grabAction.performed += grabPerformedHandler;
        }
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();

        if (jumpAction != null)
        {
            if (jumpPerformedHandler != null) jumpAction.performed -= jumpPerformedHandler;
            if (jumpCanceledHandler != null) jumpAction.canceled -= jumpCanceledHandler;
            jumpAction.Disable();
        }

        if (dashAction != null)
        {
            if (dashPerformedHandler != null) dashAction.performed -= dashPerformedHandler;
            dashAction.Disable();
        }

        if (attackAction != null)
        {
            if (attackPerformedHandler != null) attackAction.performed -= attackPerformedHandler;
            attackAction.Disable();
        }

        if (grabAction != null)
        {
            if (grabPerformedHandler != null) grabAction.performed -= grabPerformedHandler;
            grabAction.Disable();
        }
    }
}