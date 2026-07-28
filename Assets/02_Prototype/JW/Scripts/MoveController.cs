using System;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct MoveSpeed
{
    public float CrouchSpeed;
    public float WalkSpeed;
    public float RunSpeed;
}

public enum POSTURE_STATE_ENUM
{
    STANDING,
    CROUCH
}

public enum LOCOMOTION_STATE_ENUM
{
    IDLE,
    WALK,
    RUN,
    AIR
}

//상태 패턴

public interface IMoveState
{
    void OnMove(Vector3 moveDir);
    void OnJump();
    void OnSeat();
}



public class MoveController : MonoBehaviour
{
    [SerializeField] private MoveSpeed _moveSpeed;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _jumpHeight = 1.5f;
    private readonly float _gravity = -9.81f;

    private POSTURE_STATE_ENUM _posture = POSTURE_STATE_ENUM.STANDING;
    private LOCOMOTION_STATE_ENUM _locomotion = LOCOMOTION_STATE_ENUM.IDLE;

    private Vector3 _moveDir;
    private float _jumpVelocity;
    private bool _isCrouch;
    private bool _jumpRequest;
    private bool _isRun;

    private void Awake()
    {
        if (_controller == null)
            _controller = this.GetComponent<CharacterController>();
    }

    private void Update()
    {
        SetPostureLocomotion();

        UpdateVerticalVelocity();

        _moveDir.y = _jumpVelocity;

        _animator.SetFloat("XMove", _moveDir.x);
        _animator.SetFloat("ZMove", _moveDir.z);

        Vector3 moveDir = transform.right * _moveDir.x + transform.forward * _moveDir.z;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f);

        Vector3 velocity = moveDir * GetMoveSpeed();
        velocity.y = _jumpVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateVerticalVelocity()
    {
        if (_controller.isGrounded && _jumpVelocity < 0f)
        {
            _jumpVelocity = -2f;
        }

        if (_jumpRequest)
        {
            if (_controller.isGrounded)
            {
                _jumpVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _jumpRequest = false;
        }

        _jumpVelocity += _gravity * Time.deltaTime;
    }

    private void SetPostureLocomotion()
    {
        if (_moveDir == Vector3.zero)
        {
            _locomotion = LOCOMOTION_STATE_ENUM.IDLE;
        }
        else
        {
            if (_isRun)
                _locomotion = LOCOMOTION_STATE_ENUM.RUN;
            else
                _locomotion = LOCOMOTION_STATE_ENUM.WALK;
        }

        if (_jumpRequest)
            _locomotion = LOCOMOTION_STATE_ENUM.AIR;

        if (_isCrouch)
            _posture = POSTURE_STATE_ENUM.CROUCH;
        else
            _posture = POSTURE_STATE_ENUM.STANDING;
    }
    private float GetMoveSpeed()
    {
        if (_locomotion == LOCOMOTION_STATE_ENUM.IDLE)
            return 0f;

        if (_posture == POSTURE_STATE_ENUM.CROUCH)
            return _moveSpeed.CrouchSpeed;

        return _locomotion switch
        {
            LOCOMOTION_STATE_ENUM.WALK => _moveSpeed.WalkSpeed,
            LOCOMOTION_STATE_ENUM.RUN => _moveSpeed.RunSpeed,
            LOCOMOTION_STATE_ENUM.AIR => _moveSpeed.WalkSpeed,
            _ => 0f
        };
    }
    void OnMove(InputValue value)
    {
        Debug.Log(value.Get<Vector2>());

        Vector2 input = value.Get<Vector2>();

        _moveDir = new Vector3(input.x, 0, input.y);
    }

    void OnCrouch(InputValue value)
    {
        Debug.Log(value.isPressed);

        if (!value.isPressed)
            return;

        _isCrouch = !_isCrouch;

        if (_isCrouch)
            _isRun = false;

        _animator.SetBool("IsCrouch", _isCrouch);
        _animator.SetBool("IsRun", _isRun);
    }
    void OnJump(InputValue value)
    {
        if (_controller.isGrounded && !_jumpRequest)
        {
            _jumpRequest = true;
            Debug.Log("Jump");
        }

    }
    void OnSprint(InputValue value)
    {
        Debug.Log($"Run : {value.isPressed}");
        _isRun = value.isPressed;

        if (_isRun)
            _isCrouch = false;

        _animator.SetBool("IsRun", _isRun);
        _animator.SetBool("IsCrouch", _isCrouch);
    }

}
