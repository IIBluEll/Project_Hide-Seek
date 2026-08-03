using System;
using UnityEngine;

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
    private const float GRAVITY = -9.81f;
    private const float GROUNDED_VERTICAL_VELOCITY = -2f;
    private const float MOVE_INPUT_THRESHOLD = 0.0001f;

    private static readonly int X_MOVE_HASH = Animator.StringToHash("XMove");
    private static readonly int Z_MOVE_HASH = Animator.StringToHash("ZMove");
    private static readonly int IS_RUN_HASH = Animator.StringToHash("IsRun");
    private static readonly int IS_CROUCH_HASH = Animator.StringToHash("IsCrouch");

    [SerializeField] private MoveSpeed _moveSpeed;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _crouchHeight = 1f;
    
    private float _currentSprintHP;
    [SerializeField] private float _useStaminaAmount;
    [SerializeField] private float _chargeAmount;
    private float SprintStaminaNomalize => _currentSprintHP / 100;

    private IPlayerStatViewer _sprintViewer;
    private POSTURE_STATE_ENUM _posture = POSTURE_STATE_ENUM.STANDING;

    internal void Init(IPlayerStatViewer sprintViewer)
    {
        _sprintViewer = sprintViewer;
    }

    private LOCOMOTION_STATE_ENUM _locomotion = LOCOMOTION_STATE_ENUM.IDLE;

    private Vector2 _moveInput;
    private float _verticalVelocity;
    public bool _sprintRequested;

    private float _standingHeight;
    private Vector3 _standingCenter;

    public POSTURE_STATE_ENUM Posture => _posture;
    public LOCOMOTION_STATE_ENUM Locomotion => _locomotion;

    public event Action<POSTURE_STATE_ENUM> OnPostureChanged;
    public event Action<LOCOMOTION_STATE_ENUM> OnLocomotionChanged;

    private void Awake()
    {
        if (_controller == null)
            _controller = this.GetComponent<CharacterController>();

        _standingHeight = _controller.height;
        _standingCenter = _controller.center;
    }
    private void Update()
    {
        UpdateVerticalVelocity();
        UpdateLocomotionState();

        Vector3 horizontalDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        Vector3 velocity = horizontalDirection * GetMoveSpeed();
        velocity.y = _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
        UpdateAnimator();

        if (_locomotion == LOCOMOTION_STATE_ENUM.RUN)
            _currentSprintHP -= Time.deltaTime * _useStaminaAmount;
        else
            _currentSprintHP += Time.deltaTime * _chargeAmount;

        _currentSprintHP = Mathf.Clamp(_currentSprintHP, 0, 100);
        _sprintViewer.UpdateSprintStamina(SprintStaminaNomalize);
    }

    private void UpdateVerticalVelocity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = GROUNDED_VERTICAL_VELOCITY;
        else
            _verticalVelocity += GRAVITY * Time.deltaTime;
    }
    private float GetMoveSpeed()
    {
        if (_locomotion == LOCOMOTION_STATE_ENUM.IDLE)
            return 0f;

        if (_posture == POSTURE_STATE_ENUM.CROUCH)
            return _moveSpeed.CrouchSpeed;
        else
        {
            if (_locomotion == LOCOMOTION_STATE_ENUM.WALK)
                return _moveSpeed.WalkSpeed;
            else if (_locomotion == LOCOMOTION_STATE_ENUM.RUN)
                return _moveSpeed.RunSpeed;
            else
                return 0;
        }
    }
    private void UpdateAnimator()
    {
        _animator.SetFloat(X_MOVE_HASH, _moveInput.x);
        _animator.SetFloat(Z_MOVE_HASH, _moveInput.y);
        _animator.SetBool(IS_RUN_HASH, _locomotion == LOCOMOTION_STATE_ENUM.RUN);
        _animator.SetBool(IS_CROUCH_HASH, _posture == POSTURE_STATE_ENUM.CROUCH);
    }
    public void ResetMoveState()
    {
        _moveInput = Vector2.zero;
        _verticalVelocity = 0f;
        _sprintRequested = false;

        SetLocomotion(LOCOMOTION_STATE_ENUM.IDLE);
        UpdateAnimator();
    }
    private void SetPosture(POSTURE_STATE_ENUM posture)
    {
        if (_posture == posture)
            return;

        _posture = posture;

        ApplyControllerHeightAndCenter();
        UpdateLocomotionState();

        OnPostureChanged?.Invoke(_posture);
    }
    private void ApplyControllerHeightAndCenter()
    {
        if (_posture == POSTURE_STATE_ENUM.STANDING)
        {
            _controller.height = _standingHeight;
            _controller.center = _standingCenter;
            return;
        }
        
        Vector3 crouchCenter = _standingCenter;
        crouchCenter.y -= (_standingHeight - _crouchHeight) * 0.5f;

        _controller.height = _crouchHeight;
        _controller.center = crouchCenter;
    }
    private void UpdateLocomotionState()
    {
        LOCOMOTION_STATE_ENUM nextLocomotion;

        if (!_controller.isGrounded || _verticalVelocity > 0f)
        {
            nextLocomotion = LOCOMOTION_STATE_ENUM.AIR;
        }
        else if (_moveInput.sqrMagnitude <= MOVE_INPUT_THRESHOLD)
        {
            nextLocomotion = LOCOMOTION_STATE_ENUM.IDLE;
        }
        else if (_sprintRequested && _posture == POSTURE_STATE_ENUM.STANDING)
        {
            nextLocomotion = LOCOMOTION_STATE_ENUM.RUN;
        }
        else
        {
            nextLocomotion = LOCOMOTION_STATE_ENUM.WALK;
        }

        SetLocomotion(nextLocomotion);
    }
    private void SetLocomotion(LOCOMOTION_STATE_ENUM locomotion)
    {
        if (_locomotion == locomotion)
            return;

        _locomotion = locomotion;
        OnLocomotionChanged?.Invoke(_locomotion);
    }
    private bool IsCanStand()
    {
        if (_posture != POSTURE_STATE_ENUM.CROUCH)
            return false;

        float margin = 0.01f;
        float radius = _controller.radius;

        Vector3 crouchTop = _controller.center + Vector3.up * (_controller.height * 0.5f);
        Vector3 standingTop = _standingCenter + Vector3.up * (_standingHeight * 0.5f);

        Vector3 point1 = transform.TransformPoint(crouchTop + Vector3.up * (radius + margin));
        Vector3 point2 = transform.TransformPoint(standingTop - Vector3.up * radius);

        return !Physics.CheckCapsule(point1, point2, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
    }
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = Vector2.ClampMagnitude(input, 1f);
    }
    public void SetSprintInput(bool sprintRequested)
    {
        if (sprintRequested && _posture == POSTURE_STATE_ENUM.CROUCH && !IsCanStand())
            return;

        _sprintRequested = sprintRequested;

        if (_sprintRequested && _posture == POSTURE_STATE_ENUM.CROUCH)
            SetPosture(POSTURE_STATE_ENUM.STANDING);
    }
    public void RequestCrouch()
    {
        if(_posture == POSTURE_STATE_ENUM.CROUCH && IsCanStand())
        {
            SetPosture(POSTURE_STATE_ENUM.STANDING);
        }
        else
            SetPosture(POSTURE_STATE_ENUM.CROUCH);
    }


    //TODO 달리기 체력 시스템, 체력 UI
    //TODO 점프 빼기
    public void RequestJump()
    {
        if (!_controller.isGrounded || _verticalVelocity > 0)
            return;

        if (_posture == POSTURE_STATE_ENUM.CROUCH)
            _posture = POSTURE_STATE_ENUM.STANDING;

    }
}
