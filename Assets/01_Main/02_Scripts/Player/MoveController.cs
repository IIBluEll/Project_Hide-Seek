using System;
using UnityEngine;

[System.Serializable]
public struct MoveSpeed
{
    public float CrouchSpeed;
    public float WalkSpeed;
    public float RunSpeed;
}
[System.Serializable]
public struct PostureHeight
{
    public float StandingHeight;
    public float CrouchHeight;
    public float ProneHeight;

    public float GetHeight(POSTURE_STATE_ENUM posture)
    {
        switch (posture)
        {
            case POSTURE_STATE_ENUM.STANDING:
                return StandingHeight;
            case POSTURE_STATE_ENUM.CROUCH:
                return CrouchHeight;
            case POSTURE_STATE_ENUM.PRONE:
                return ProneHeight;
            default:
                throw new InvalidOperationException("Enum Type Error");
        }
    }
}

[System.Serializable]
public struct SprintStaminaSetting
{
    public float Maximum;
    public float UseRatePerSecond;
    public float RecoveryRatePerSecond;
}

public enum POSTURE_STATE_ENUM
{
    STANDING,
    CROUCH,
    PRONE
}
public enum LOCOMOTION_STATE_ENUM
{
    IDLE,
    WALK,
    RUN
}

[RequireComponent(typeof(CharacterController))]
public class MoveController : MonoBehaviour
{
    private const float GRAVITY = -9.81f;
    private const float GROUNDED_VERTICAL_VELOCITY = -2f;
    private const float MOVE_INPUT_THRESHOLD = 0.0001f;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private SprintStaminaSetting _staminaSetting;
    [SerializeField] private MoveSpeed _moveSpeed;
    [SerializeField] private PostureHeight _height;
    [SerializeField] private PostureHeight _centerHeight;

    private POSTURE_STATE_ENUM _posture = POSTURE_STATE_ENUM.STANDING;
    private LOCOMOTION_STATE_ENUM _locomotion = LOCOMOTION_STATE_ENUM.IDLE;

    private Vector2 _moveInput;
    private float _verticalVelocity;
    private bool _sprintRequested;
    private float _currentSprintHP;

    public event Action<float> OnChangedStamina;
    public event Action<Vector2> OnMoveEvent;
    public event Action<POSTURE_STATE_ENUM, bool> OnPostureChanged;
    public event Action<LOCOMOTION_STATE_ENUM, bool> OnLocomotionChanged;

    public POSTURE_STATE_ENUM Posture => _posture;
    public LOCOMOTION_STATE_ENUM Locomotion => _locomotion;
    private float SprintStaminaNomalize => _currentSprintHP / _staminaSetting.Maximum;

    private void Awake()
    {
        if (_characterController == null)
            _characterController = this.GetComponent<CharacterController>();

        _characterController.height = _height.StandingHeight;
        _characterController.center = new Vector3(0, _centerHeight.StandingHeight, 0);

        _currentSprintHP = _staminaSetting.Maximum;
    }

    private void Update()
    {
        UpdateSprintStamina();
        UpdateVerticalVelocity();
        UpdateLocomotionState();

        UpdateMoveResult();
    }
    private void UpdateMoveResult()
    {
        OnMoveEvent?.Invoke(_moveInput);

        Vector3 horizontalDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        Vector3 velocity = horizontalDirection * GetMoveSpeed();
        velocity.y = _verticalVelocity;

        if(_characterController.enabled)
            _characterController.Move(velocity * Time.deltaTime);
    }
    private void UpdateVerticalVelocity()
    {
        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = GROUNDED_VERTICAL_VELOCITY;
        else
            _verticalVelocity += GRAVITY * Time.deltaTime;
    }
    private void UpdateSprintStamina()
    {
        if (_locomotion == LOCOMOTION_STATE_ENUM.RUN)
            _currentSprintHP -= Time.deltaTime * _staminaSetting.UseRatePerSecond;
        else
            _currentSprintHP += Time.deltaTime * _staminaSetting.RecoveryRatePerSecond;

        _currentSprintHP = Mathf.Clamp(_currentSprintHP, 0, _staminaSetting.Maximum);

        OnChangedStamina?.Invoke(SprintStaminaNomalize);

        if (_currentSprintHP == 0 && _locomotion == LOCOMOTION_STATE_ENUM.RUN)
            _sprintRequested = false;
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
    public void ResetMoveState()
    {
        StopMove();

        _verticalVelocity = 0f;

        SetPosture(POSTURE_STATE_ENUM.STANDING);
    }
    public void StopMove()
    {
        _moveInput = Vector2.zero;
        _sprintRequested = false;
        SetLocomotion(LOCOMOTION_STATE_ENUM.IDLE);
        OnMoveEvent?.Invoke(_moveInput);
    }
    public void SetPosture(POSTURE_STATE_ENUM posture)
    {
        if (_posture == posture)
            return;

        OnPostureChanged?.Invoke(_posture, false);
        _posture = posture;
        OnPostureChanged?.Invoke(_posture, true);

        ApplyControllerHeightAndCenter();
    }
    private void ApplyControllerHeightAndCenter()
    {
        float height = _height.GetHeight(_posture);
        float centerHeight = _centerHeight.GetHeight(_posture);

        _characterController.height = height;
        _characterController.center = new Vector3(0, centerHeight, 0);
    }
    private void UpdateLocomotionState()
    {
        LOCOMOTION_STATE_ENUM nextLocomotion;

        if (_moveInput.sqrMagnitude <= MOVE_INPUT_THRESHOLD)
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

        OnLocomotionChanged?.Invoke(_locomotion, false);

        _locomotion = locomotion;

        OnLocomotionChanged?.Invoke(_locomotion, true);
    }
    private bool IsCanStand()
    {
        if (_posture != POSTURE_STATE_ENUM.CROUCH)
            return false;

        float margin = 0.01f;
        float radius = _characterController.radius;

        Vector3 crouchTop = Vector3.up * _height.CrouchHeight;
        Vector3 standingTop = Vector3.up * _height.StandingHeight;

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
    public void Teleport(Vector3 position)
    {
        _characterController.transform.position = position;
    }
    public void CharacterControllerEnabled(bool value)
    {
        _characterController.enabled = value;
    }
}
