using System;
using UnityEngine;

[System.Serializable]
public class DefactData
{
    public Transform CameraTrans;
    public float RayDistance;
    public LayerMask InteractionRaycastLayerMask;
}

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private DefactData _defactData;

    private PlayerHandController _hand;
    private MoveController _moveController;
    private CharacterRotationController _rotationController;

    private IInteractable _currentInteractable;
    private IInteractable _contextInteractable;
    private IStateService _stat;

    public event Action<string> OnInsightInteractEvent;
    public event Action OnOutsightInteractionEvent;

    internal void Init(IStateService stat, MoveController move, CharacterRotationController rotator, PlayerHandController hand)
    {
        _moveController = move;
        _rotationController = rotator;
        _hand = hand;
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
        if (_contextInteractable != null)
        {
            if (_contextInteractable.CanInteract(this) && _stat.CanInteraction)
                OnInsightInteractEvent?.Invoke(_contextInteractable.InteractionPrompt);
            else
                OnOutsightInteractionEvent?.Invoke();

            return;
        }

        Ray ray = new Ray(_defactData.CameraTrans.position, _defactData.CameraTrans.forward);

        if(Physics.Raycast(ray, out RaycastHit hit, _defactData.RayDistance, _defactData.InteractionRaycastLayerMask))
        {
            IInteractable interactable = hit.transform.GetComponent<IInteractable>();

            if(interactable != null)
            {
                if (interactable.CanInteract(this) && _stat.CanInteraction)
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
        IInteractable target = _contextInteractable ?? _currentInteractable;

        if ((_stat != null && !_stat.CanInteraction) || target == null)
            return;

        if (target.CanInteract(this))
            target.InteractAct(this);
    }
    public void OnInteractReleaseAction()
    {
        IInteractable target = _contextInteractable ?? _currentInteractable;

        if (target != null)
            target.InteractRelease(this);
    }
    public void SetContextInteractable(IInteractable interactable)
    {
        _currentInteractable = null;
        _contextInteractable = interactable;
    }
    public void ClearContextInteractable(IInteractable interactable)
    {
        if (ReferenceEquals(_contextInteractable, interactable))
            _contextInteractable = null;
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
        _moveController.Teleport(position);
    }
    public void SetRotation(Vector3 rotation)
    {
        _rotationController.SetYRotation(rotation);
    }
    public void SetPositionState(PLAYER_POSITION_STATE state)
    {
        _stat.SetPositionState(state);
    }
    public void SetPosture(POSTURE_STATE_ENUM posture)
    {
        _moveController.SetPosture(posture);
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
        _stat.SetActionState(PLAYER_ACTION_STATE.AIMING);
    }
    public void EndActing()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.IDLE);
    }
    private void OnAimStateChangedActioned(bool isAiming)
    {
        PLAYER_ACTION_STATE state = isAiming
            ? PLAYER_ACTION_STATE.AIMING
            : PLAYER_ACTION_STATE.IDLE;

        _stat.SetActionState(state);
    }
    public void BeginGeneratorRepair()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.REPAIRING_GENERATOR);
    }

    public void EndGeneratorRepair()
    {
        _stat.SetActionState(PLAYER_ACTION_STATE.IDLE);
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
