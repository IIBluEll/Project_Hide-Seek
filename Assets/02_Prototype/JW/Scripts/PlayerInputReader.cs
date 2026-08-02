using System;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IInputReader
{
    event Action<Vector2> OnMoveEvent;
    event Action<Vector2> OnLookEvent;
    event Action<bool> OnSprintEvent;
    event Action<bool> OnCrouchEvent;
    event Action<bool> OnJumpEvent;
    event Action<bool> OnInteractionEvent;
    event Action<bool> OnAttackEvent;
    event Action<bool> OnCancelAimEvent;
}

public class PlayerInputReader : MonoBehaviour, IInputReader
{
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action<bool> OnSprintEvent;
    public event Action<bool> OnCrouchEvent;
    public event Action<bool> OnJumpEvent;
    public event Action<bool> OnInteractionEvent;
    public event Action<bool> OnAttackEvent;
    public event Action<bool> OnCancelAimEvent;


    private void OnMove(InputValue value)
    {
        OnMoveEvent?.Invoke(value.Get<Vector2>());
    }
    private void OnLook(InputValue value)
    {
        OnLookEvent?.Invoke(value.Get<Vector2>());
    }
    private void OnInteract(InputValue value)
    {
        OnInteractionEvent?.Invoke(value.isPressed);
    }
    private void OnCrouch(InputValue value)
    {
        OnCrouchEvent?.Invoke(value.isPressed);
    }
    private void OnSprint(InputValue value)
    {
        OnSprintEvent?.Invoke(value.isPressed);
    }
    private void OnJump(InputValue value)
    {
        OnJumpEvent?.Invoke(value.isPressed);
    }
    private void OnAttack(InputValue value)
    {
        OnAttackEvent?.Invoke(value.isPressed);
    }
    private void OnCancelAim(InputValue value)
    {
        OnCancelAimEvent?.Invoke(value.isPressed);
    }
}
