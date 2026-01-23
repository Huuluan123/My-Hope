using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction dashAction;
    public InputAction attackAction;

    public event Action<InputAction.CallbackContext> JumpPerformed;
    public event Action<InputAction.CallbackContext> JumpCanceled;
    public event Action<InputAction.CallbackContext> DashPerformed;
    public event Action<InputAction.CallbackContext> AttackPerformed;

    private Action<InputAction.CallbackContext> jumpPerformedHandler;
    private Action<InputAction.CallbackContext> jumpCanceledHandler;
    private Action<InputAction.CallbackContext> dashPerformedHandler;
    private Action<InputAction.CallbackContext> attackPerformedHandler;

    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        attackAction = InputSystem.actions.FindAction("Attack");
    }

    public Vector2 MoveInput => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

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
    }
}