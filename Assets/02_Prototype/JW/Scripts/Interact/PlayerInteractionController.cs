using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    private PlayerStateController _controller;

    //물건 줍기
    //숨기
    private void Awake()
    {
        if (_moveController == null)
            _moveController = GetComponent<MoveController>();
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
                _currentInteractable = null;
                OnOutsightInteractionEvent?.Invoke();
            }
        }
        else
        {
            _currentInteractable = null;
            OnOutsightInteractionEvent?.Invoke();
        }
    }
    public void OnInteractAction()
    {
        if (_currentInteractable == null)
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
    public void OnEndTransition(EPLAYER_STATE_TYPE state)
    {
        _controller.SetState(state);
    }
}
