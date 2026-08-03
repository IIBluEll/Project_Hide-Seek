using System;
using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerHandController _hand;
    [SerializeField] private Transform _player;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CharacterRotationController _rotationController;
    [SerializeField] private MoveController _moveController;
    
    [SerializeField] private Camera _camera;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _interactionRaycastLayerMask;
    [SerializeField] private float _screenFadeDuration = 0.25f;

    private IInteractable _currentInteractable;

    public event Action<string> OnInsightInteractEvent;
    public event Action OnOutsightInteractionEvent;
    private IStateService _stat;

    //물건 줍기
    //숨기
    private void Awake()
    {
        if (_moveController == null)
            _moveController = GetComponent<MoveController>();
    }

    internal void Init(IStateService stat)
    {
        _stat = stat;
        _hand.OnAimStateChanged -= OnAimStateChangedActioned;
        _hand.OnAimStateChanged += OnAimStateChangedActioned;
    }

    private void Update()
    {
        DetectInteractable();
    }
    private void DetectInteractable()
    {
        if (_stat != null && !_stat.CanInteraction)
        {
            ClearCurrentInteractable();
            return;
        }

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _interactionRaycastLayerMask))
        {
            IInteractable interactable = hit.transform.GetComponent<IInteractable>();

            if(interactable != null && interactable.CanInteract(this))
            {
                _currentInteractable = interactable;
                OnInsightInteractEvent?.Invoke(_currentInteractable.InteractionPrompt);
            }
            else
            {
                ClearCurrentInteractable();
            }
        }
        else
        {
            ClearCurrentInteractable();
        }
    }
    public void OnInteractAction()
    {
        if ((_stat != null && !_stat.CanInteraction) || _currentInteractable == null)
            return;

        if (_currentInteractable.CanInteract(this))
            _currentInteractable.Interact(this);
    }
    public void TryGrap(GrapItem grapItem)
    {
        _hand.GrapItem(grapItem);
    }
    public void OnTeleport(Transform transform)
    {
        SetPosition(transform.position);
        SetRotation(transform.rotation.eulerAngles);
    }
    public void SetPosition(Vector3 position)
    {
        _characterController.enabled = false;
        _player.transform.position = position;
        _characterController.enabled = true;
    }
    public void SetRotation(Vector3 rotation)
    {
        _rotationController.SetYRotation(rotation);
    }
    public void BeginTransition()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.TRANSITION);
    }

    public void SetPositionState(PLAYER_POSITION_STATE state)
    {
        _stat.SetPositionState(state);
    }

    public void EndTransition()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.IDLE);
    }

    private void OnAimStateChangedActioned(bool isAiming)
    {
        PLAYER_ACTION_STATE state = isAiming
            ? PLAYER_ACTION_STATE.ACTIONING
            : PLAYER_ACTION_STATE.IDLE;

        _stat.SetActionState(state);
    }

    private void ClearCurrentInteractable()
    {
        _currentInteractable = null;
        OnOutsightInteractionEvent?.Invoke();
    }

    private void OnDestroy()
    {
        if (_hand != null)
            _hand.OnAimStateChanged -= OnAimStateChangedActioned;
    }
}
