using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InteractPresenter _interactPresenter = new InteractPresenter();

    [SerializeField] private InteractViewer _viewer;
    [SerializeField] private PlayerInteractionController _interactController;

    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private MoveController _move;
    [SerializeField] private CharacterRotationController _rotation;
    [SerializeField] private PlayerCameraController _camera;
    [SerializeField] private PlayerInteractionController _interact;
    [SerializeField] private PlayerHandController _hand;

    [Header("Viewer")]
    [SerializeField] private ThrowUIViewer _throwViewer;

    private readonly PlayerStateController _stat = new PlayerStateController();

    //이동
    //회전
    private void Awake()
    {
        _interactPresenter.Init(_viewer, _interactController);

        _camera.SetCameraHeight(_move.Posture);
        _camera.SetShakeIntensity(_move.Posture, _move.Locomotion);
    }
    private void Bind()
    {
        _inputReader.OnMoveEvent += OnMoveAction;
        _inputReader.OnLookEvent += OnLookAction;
        _inputReader.OnSprintEvent += OnSprintAction;
        _inputReader.OnCrouchEvent += OnCrouchAction;
        _inputReader.OnJumpEvent += OnJumpAction;
        _inputReader.OnInteractionEvent += OnInteractAction;
        _inputReader.OnAttackEvent += OnAttackAction;
        _inputReader.OnCancelAimEvent += OnCancelAimAction;

        _move.OnPostureChanged += OnPostureChangedActioned;
        _move.OnLocomotionChanged += OnLocomotionChangedActioned;

        _hand.OnThrowPowerChanged += _throwViewer.ChargeGage;
        _hand.OnAimStateChanged += _throwViewer.OnAimStateChanged;
    }
    private void Unbind()
    {
        _inputReader.OnMoveEvent -= OnMoveAction;
        _inputReader.OnLookEvent -= OnLookAction;
        _inputReader.OnSprintEvent -= OnSprintAction;
        _inputReader.OnCrouchEvent -= OnCrouchAction;
        _inputReader.OnJumpEvent -= OnJumpAction;
        _inputReader.OnInteractionEvent -= OnInteractAction;
        _inputReader.OnAttackEvent -= OnAttackAction;
        _inputReader.OnCancelAimEvent -= OnCancelAimAction;

        _move.OnPostureChanged -= OnPostureChangedActioned;
        _move.OnLocomotionChanged -= OnLocomotionChangedActioned;

    }

    #region Actions
    private void OnMoveAction(Vector2 value)
    {
        if (_stat.CanMove)
            _move.SetMoveInput(value);
    }
    private void OnLookAction(Vector2 value)
    {
        if (!_stat.CanRotate)
            return;

        _camera.RotateXAxis(value.y);
        _rotation.Rotate(value);
    }
    private void OnSprintAction(bool value)
    {
        if (_stat.CanMove)
            _move.SetSprintInput(value);
    }
    private void OnCrouchAction(bool value)
    {
        if (_stat.CanCrouch && value)
            _move.RequestCrouch();
    }
    private void OnJumpAction(bool value)
    {
        if (_stat.CanMove && value)
            _move.RequestJump();
    }
    private void OnInteractAction(bool value)
    {
        Debug.Log("?");
        if(value)
            _interact.OnInteractAction();
    }
    private void OnPostureChangedActioned(POSTURE_STATE_ENUM posture)
    {
        _camera.SetCameraHeight(posture);
        _camera.SetShakeIntensity(posture, _move.Locomotion);
    }
    private void OnLocomotionChangedActioned(LOCOMOTION_STATE_ENUM locomotion)
    {
        _camera.SetShakeIntensity(_move.Posture, locomotion);
    }
    private void OnAttackAction(bool value)
    {
        _hand.OnAimAction(value);
    }
    private void OnCancelAimAction(bool value)
    {
        if (value)
            _hand.OnAimCalcelAction();
    }
    #endregion

    private void OnEnable()
    {
        Bind();
    }
    private void OnDisable()
    {
        Unbind();
    }
}
