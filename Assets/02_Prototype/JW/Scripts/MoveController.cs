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

public enum MOVETYPE_ENUM
{
    None,
    Crouch,
    Walk,
    Run
}

public enum POSTURE_STATE_ENUM
{
    Standing,
    Crouch
}

public enum LOCOMOTION_STATE_ENUM
{
    Idle,
    Walk,
    Run,
    Airborne
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
    [SerializeField] private float _jumpHeight = 1.5f;
    private readonly float _gravity = -9.81f;


    private POSTURE_STATE_ENUM _posture = POSTURE_STATE_ENUM.Standing;
    private LOCOMOTION_STATE_ENUM _locomotion = LOCOMOTION_STATE_ENUM.Idle;

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

    public float CurrentMoveSpeed;

    private void Update()
    {
        SetPostureLocomotion();

        UpdateVerticalVelocity();

        CurrentMoveSpeed = GetMoveSpeed();

        _moveDir.y = _jumpVelocity;
        _controller.Move(_moveDir * Time.deltaTime * GetMoveSpeed());
    }

    private void UpdateVerticalVelocity()
    {
        // 바닥에 붙어 있도록 작은 하강 속도를 유지
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

            // 입력 요청은 성공 여부와 관계없이 한 번만 처리
            _jumpRequest = false;
        }

        _jumpVelocity += _gravity * Time.deltaTime;
    }

    private void SetPostureLocomotion()
    {
        if (_moveDir == Vector3.zero)
        {
            _locomotion = LOCOMOTION_STATE_ENUM.Idle;
        }
        else
        {
            if (_isRun)
                _locomotion = LOCOMOTION_STATE_ENUM.Run;
            else
                _locomotion = LOCOMOTION_STATE_ENUM.Walk;
        }

        if (_jumpRequest)
            _locomotion = LOCOMOTION_STATE_ENUM.Airborne;

        if (_isCrouch)
            _posture = POSTURE_STATE_ENUM.Crouch;
        else
            _posture = POSTURE_STATE_ENUM.Standing;
    }
    private float GetMoveSpeed()
    {
        if (_locomotion == LOCOMOTION_STATE_ENUM.Idle)
            return 0f;

        if (_posture == POSTURE_STATE_ENUM.Crouch)
            return _moveSpeed.CrouchSpeed;

        return _locomotion switch
        {
            LOCOMOTION_STATE_ENUM.Walk => _moveSpeed.WalkSpeed,
            LOCOMOTION_STATE_ENUM.Run => _moveSpeed.RunSpeed,
            LOCOMOTION_STATE_ENUM.Airborne => _moveSpeed.WalkSpeed,
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
        _isCrouch = value.isPressed;
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
    }

}
