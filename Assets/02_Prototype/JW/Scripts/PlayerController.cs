using UnityEngine;

public class PlayerController : MonoBehaviour, IPlayerVisibilityState
{
    private InteractPresenter _interactPresenter = new InteractPresenter();
    private readonly PlayerStateController _state = new PlayerStateController();

    [SerializeField] private PlayerInteractionController _interactController;

    [SerializeField] private PlayerAnimationController _animationController;

    [SerializeField] private CharacterRotationController _rotator;
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private MoveController _move;
    [SerializeField] private PlayerCameraController _camera;
    [SerializeField] private PlayerInteractionController _interact;
    [SerializeField] private PlayerHandController _hand;
    [SerializeField] private FootSteepNoiseEmitter _footNoiseEmitter;
    [SerializeField] private PlayerWakeUpBlink _wakeUpBlink;

    [Header("Viewer")]
    [SerializeField] private InteractViewer _interactionViewer;
    [SerializeField] private ThrowUIViewer _throwViewer;
    [SerializeField] private PlayerSprintStaminaViewer _sprintViewer;
    public IStateService State => _state;
    public bool IsFullyHidden => _state.IsFullyHidden;

    public IInputReader InputReader => _inputReader;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _interactPresenter.Init(_interactionViewer, _interactController);

        _camera.SetCameraHeight(_move.Posture);
        _camera.SetShakeIntensity(_move.Posture, _move.Locomotion);

        _interact.Init(_state, _move, _rotator, _hand);

        _rotator.Init(this.transform);

        _wakeUpBlink.OnFinishedBlinkEvent += OnFinishedWakeup;
    }

    private void Bind()
    {
        _inputReader.OnMoveEvent += OnMoveAction;
        _inputReader.OnLookEvent += OnLookAction;
        _inputReader.OnSprintEvent += OnSprintAction;
        _inputReader.OnCrouchEvent += OnCrouchAction;
        _inputReader.OnInteractionEvent += OnInteractAction;
        _inputReader.OnAttackEvent += OnAttackAction;
        _inputReader.OnCancelAimEvent += OnCancelAimAction;

        _hand.OnThrowPowerChanged += _throwViewer.ChargeGage;
        _hand.OnAimStateChanged += _throwViewer.OnAimStateChanged;

        _state.OnChangedPositionStateEvent += OnChangePositionState;
        _state.OnChangedActionStateEvent += OnChangeActionState;

        _move.OnChangedStamina += _sprintViewer.UpdateSprintStamina;
        _move.OnMoveEvent += _animationController.SetMoveAnima;

        _move.OnPostureChanged += OnPostureChangedActioned;
        _move.OnLocomotionChanged += OnLocomotionChangedActioned;
    }
    private void OnChangeActionState(PLAYER_ACTION_STATE action)
    {
        if (action == PLAYER_ACTION_STATE.REPAIRING_GENERATOR)
        {
            _move.StopMove();
            _animationController.SetLayerWeight(2, 1);
            _animationController.SetTrigger("Working");
        }

        if(action == PLAYER_ACTION_STATE.IDLE)
        {
            _animationController.SetLayerWeight(2, 0, 0.3f);
            _animationController.SetTrigger("EndWorking");
        }
    }
    private void OnChangePositionState(PLAYER_POSITION_STATE position)
    {
        if (position == PLAYER_POSITION_STATE.HIDING) 
            _move.StopMove();
    }
    #region Actions
    private void OnMoveAction(Vector2 value)
    {
        if (_state.CanMove)
            _move.SetMoveInput(value);
    }
    private void OnLookAction(Vector2 value)
    {
        if (!_state.CanRotate)
            return;

        _camera.RotateXAxis(value.y);
        _rotator.Rotate(value);
    }
    private void OnSprintAction(bool value)
    {
        if (_state.CanMove)
            _move.SetSprintInput(value);
    }
    private void OnCrouchAction(bool value)
    {
        if (_state.CanCrouch && value)
            _move.RequestCrouch();
    }
    private void OnInteractAction(bool value)
    {
        if (value)
            _interact.OnInteractAction();
        else
            _interact.OnInteractReleaseAction();
    }
    private void OnPostureChangedActioned(POSTURE_STATE_ENUM posture, bool value)
    {
        if (value)
            Debug.Log($"{posture}");

        _animationController.SetPostureParam(posture, value);

        if (value)
        {
            _camera.SetCameraHeight(posture);
            _camera.SetShakeIntensity(posture, _move.Locomotion);
        }
    }
    private void OnLocomotionChangedActioned(LOCOMOTION_STATE_ENUM locomotion, bool value)
    {
        _animationController.SetLocomotionAnima(locomotion, value);
        if (value)
        {
            _camera.SetShakeIntensity(_move.Posture, locomotion);
            _footNoiseEmitter.OnChangedPlayerFootStep(locomotion);
            _footNoiseEmitter.UpdateFootSound(locomotion, _move.Posture);
        }
    }
    private void OnAttackAction(bool value)
    {
        if(_state.CanAction)
            _hand.OnAimAction(value);
    }
    private void OnCancelAimAction(bool value)
    {
        if (value)
            _hand.OnAimCalcelAction();
    }
    #endregion
    public void WakeUpDirect()
    {
        _animationController.SetTrigger("Standing");
        State.SetActionState(PLAYER_ACTION_STATE.TRANSITION);
        _wakeUpBlink.Play();
    }
    private void OnFinishedWakeup()
    {
        State.SetActionState(PLAYER_ACTION_STATE.IDLE);
    }
    private void Unbind()
    {
        _inputReader.OnMoveEvent -= OnMoveAction;
        _inputReader.OnLookEvent -= OnLookAction;
        _inputReader.OnSprintEvent -= OnSprintAction;
        _inputReader.OnCrouchEvent -= OnCrouchAction;
        _inputReader.OnInteractionEvent -= OnInteractAction;
        _inputReader.OnAttackEvent -= OnAttackAction;
        _inputReader.OnCancelAimEvent -= OnCancelAimAction;

        _move.OnPostureChanged -= OnPostureChangedActioned;
        _move.OnLocomotionChanged -= OnLocomotionChangedActioned;

        _hand.OnThrowPowerChanged -= _throwViewer.ChargeGage;
        _hand.OnAimStateChanged -= _throwViewer.OnAimStateChanged;
    }
    private void OnEnable()
    {
        Bind();
    }
    private void OnDisable()
    {
        Unbind();
    }
}
