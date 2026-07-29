using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerHandController _itemController;

    [SerializeField] private Camera _camera;
    [SerializeField] private float _rayDistance;
    [SerializeField] private LayerMask _interactionRaycastLayerMask;

    private IInteractable _currentInteractable;

    public event Action<string> OnInsightInteractEvent;
    public event Action OnOutsightInteractionEvent;

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
        Debug.Log(value.isPressed);

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
}
