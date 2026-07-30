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


public class MoveController : MonoBehaviour
{
    [SerializeField] private MoveSpeed _moveSpeed;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _crouchControllerHeight = 1f;
    private readonly float _gravity = -9.81f;

    private POSTURE_STATE_ENUM _posture = POSTURE_STATE_ENUM.STANDING;
    private LOCOMOTION_STATE_ENUM _locomotion = LOCOMOTION_STATE_ENUM.IDLE;

    private Vector3 _moveDir;
    private float _jumpVelocity;
    private bool _isCrouch;
    private bool _jumpRequest;
    private bool _isRun;
    private bool _movementEnabled = true;
    private float _standingControllerHeight;
    private Vector3 _standingControllerCenter;

    public POSTURE_STATE_ENUM Posture => _posture;
    public bool MovementEnabled => _movementEnabled;

    public event Action<POSTURE_STATE_ENUM> OnPostureChanged;

    private void Awake()
    {
        if (_controller == null)
            _controller = this.GetComponent<CharacterController>();

        _standingControllerHeight = _controller.height;
        _standingControllerCenter = _controller.center;
    }

    private void Update()
    {
        if (!_movementEnabled)
            return;

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
        if (!_movementEnabled)
        {
            _moveDir = Vector3.zero;
            return;
        }

        Vector2 input = value.Get<Vector2>();

        _moveDir = new Vector3(input.x, 0, input.y);
    }

    void OnCrouch(InputValue value)
    {
        if (!_movementEnabled || !value.isPressed)
            return;

        POSTURE_STATE_ENUM nextPosture = _posture == POSTURE_STATE_ENUM.CROUCH
            ? POSTURE_STATE_ENUM.STANDING
            : POSTURE_STATE_ENUM.CROUCH;

        SetPosture(nextPosture);
    }
    void OnJump(InputValue value)
    {
        if (!_movementEnabled)
            return;

        if (_controller.isGrounded && !_jumpRequest)
        {
            _jumpRequest = true;
            Debug.Log("Jump");
        }

    }
    void OnSprint(InputValue value)
    {
        if (!_movementEnabled)
            return;

        _isRun = value.isPressed;

        if (_isRun)
        {
            SetPosture(POSTURE_STATE_ENUM.STANDING);

            if (_posture == POSTURE_STATE_ENUM.CROUCH)
                _isRun = false;
        }

        _animator.SetBool("IsRun", _isRun);
        _animator.SetBool("IsCrouch", _isCrouch);
    }

    public void SetMovementEnabled(bool movementEnabled)
    {
        _movementEnabled = movementEnabled;

        if (_movementEnabled)
            return;

        _moveDir = Vector3.zero;
        _jumpVelocity = 0f;
        _jumpRequest = false;
        _isRun = false;
        _locomotion = LOCOMOTION_STATE_ENUM.IDLE;

        _animator.SetFloat("XMove", 0f);
        _animator.SetFloat("ZMove", 0f);
        _animator.SetBool("IsRun", false);
    }

    private void SetPosture(POSTURE_STATE_ENUM posture)
    {
        if (_posture == posture)
            return;

        if (posture == POSTURE_STATE_ENUM.STANDING && !CanStand())
            return;

        _posture = posture;
        _isCrouch = _posture == POSTURE_STATE_ENUM.CROUCH;

        if (_isCrouch)
            _isRun = false;

        ApplyControllerHeight();

        _animator.SetBool("IsCrouch", _isCrouch);
        _animator.SetBool("IsRun", _isRun);

        OnPostureChanged?.Invoke(_posture);
    }

    private void ApplyControllerHeight()
    {
        if (_posture == POSTURE_STATE_ENUM.STANDING)
        {
            _controller.height = _standingControllerHeight;
            _controller.center = _standingControllerCenter;
            return;
        }

        float crouchHeight = Mathf.Max(_crouchControllerHeight, _controller.radius * 2f);
        Vector3 crouchCenter = _standingControllerCenter;
        crouchCenter.y -= (_standingControllerHeight - crouchHeight) * 0.5f;

        _controller.height = crouchHeight;
        _controller.center = crouchCenter;
    }

    private bool CanStand()
    {
        float radius = _controller.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.z));
        float height = Mathf.Max(
            _standingControllerHeight * Mathf.Abs(transform.lossyScale.y),
            radius * 2f);
        Vector3 center = transform.TransformPoint(_standingControllerCenter);
        float halfSegment = Mathf.Max((height * 0.5f) - radius, 0f);
        Vector3 point1 = center + transform.up * halfSegment;
        Vector3 point2 = center - transform.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            point1,
            point2,
            radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (!overlap.transform.IsChildOf(transform))
                return false;
        }

        return true;
    }
}
