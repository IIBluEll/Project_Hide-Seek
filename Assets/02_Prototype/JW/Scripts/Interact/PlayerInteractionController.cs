using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerHandController _itemController;
    [SerializeField] private Transform _player;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CharacterRotationController _rotationController;
    [SerializeField] private MoveController _moveController;

    [SerializeField] private Camera _camera;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _interactionRaycastLayerMask;
    [SerializeField] private ScreenFade_view _screenFadeView;
    [SerializeField] private float _screenFadeDuration = 0.25f;

    private IInteractable _currentInteractable;
    private ScreenFadePresenter_presenter _screenFadePresenter;

    public event Action<string> OnInsightInteractEvent;
    public event Action OnOutsightInteractionEvent;

    private void Awake()
    {
        if (_moveController == null)
            _moveController = GetComponent<MoveController>();

        if (_screenFadeView == null)
            _screenFadeView = FindFirstObjectByType<ScreenFade_view>();

        if (_screenFadeView == null)
            return;

        ScreenFadeModel_model screenFadeModel = new ScreenFadeModel_model(_screenFadeDuration);
        _screenFadePresenter = new ScreenFadePresenter_presenter(
            screenFadeModel,
            _screenFadeView);
        _screenFadePresenter.Open();
    }

    private void OnDestroy()
    {
        _screenFadePresenter?.Dispose();
    }

    private void Update()
    {
        DetectInteractable();
    }
    private void DetectInteractable()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _interactionRaycastLayerMask))
        {
            IInteractable interactable = hit.transform.GetComponent<IInteractable>();

            if(interactable.CanInteract(this))
            {
                _currentInteractable = interactable;
                OnInsightInteractEvent?.Invoke(_currentInteractable.InteractionPrompt);
            }
            else
            {
                OnOutsightInteractionEvent?.Invoke();
            }
        }
        else
        {
            OnOutsightInteractionEvent?.Invoke();
        }
    }
    private void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (_currentInteractable == null)
            return;

        if (_currentInteractable.CanInteract(this))
            _currentInteractable.Interact(this);
    }

    public void TryGrap(GrapItem grapItem)
    {
        _itemController.GrapItem(grapItem);
    }

    public void SetPosition(Vector3 position)
    {
        Debug.Log(position);
        _characterController.enabled = false;
        _player.transform.position = position;
        _characterController.enabled = true;
    }
    public void SetRotation(Vector3 rotation)
    {
        _rotationController.SetYRotation(rotation);
    }

    public async UniTask<bool> TransitionPlayer_async(
        Transform targetPosition,
        bool enableMovementAfterTransition,
        CancellationToken cancellationToken)
    {
        if (targetPosition == null ||
            _screenFadePresenter == null ||
            _screenFadePresenter.IsTransitioning)
        {
            return false;
        }

        bool previousMovementEnabled = _moveController.MovementEnabled;
        _moveController.SetMovementEnabled(false);

        bool completed = false;

        try
        {
            completed = await _screenFadePresenter.PlayTransition_async(
                () =>
                {
                    SetPosition(targetPosition.position);
                    SetRotation(targetPosition.rotation.eulerAngles);
                },
                cancellationToken);

            return completed;
        }
        finally
        {
            _moveController.SetMovementEnabled(
                completed
                    ? enableMovementAfterTransition
                    : previousMovementEnabled);
        }
    }
}
