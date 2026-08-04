using System;
using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerHandController _hand;
    [SerializeField] private Transform _player;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CharacterRotationController _rotationController;
    
    [SerializeField] private Camera _camera;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _interactionRaycastLayerMask;

    private IInteractable _currentInteractable;

    public event Action<string> OnInsightInteractEvent;
    public event Action OnOutsightInteractionEvent;
    private IStateService _stat;

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
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _interactionRaycastLayerMask))
        {
            IInteractable interactable = hit.transform.GetComponent<IInteractable>();

            if(interactable != null)
            {
                if (interactable.CanInteract(this) && _stat.CanInteraction)
                {
                    Debug.Log("1111");
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
            _currentInteractable.InteractAct(this);
    }
    public void OnInteractReleaseAction()
    {
        if (_currentInteractable != null)
            _currentInteractable.InteractRelease(this);
    }
    public void OnSubInteractAction()
    {

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
    public void SetPositionState(PLAYER_POSITION_STATE state)
    {
        _stat.SetPositionState(state);
    }
    public void SetPosutre(POSTURE_STATE_ENUM postureType)
    {

    }
    public void BeginTransition()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.TRANSITION);
    }
    public void EndTransition()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.IDLE);
    }
    public void BeginActing()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.ACTIONING);
    }
    public void EndActing()
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
        Debug.Log("ASDFASASDFASd");
        _currentInteractable = null;
        OnOutsightInteractionEvent?.Invoke();
    }
    private void OnDestroy()
    {
        if (_hand != null)
            _hand.OnAimStateChanged -= OnAimStateChangedActioned;
    }
}
