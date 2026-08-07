using System.Collections;
using UnityEngine;

[System.Serializable]
public struct DoorState
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private float _rotateSpeed;

    [SerializeField] private DoorState _originState;
    [SerializeField] private DoorState _openState;

    private bool _isRotate = false;
    private bool _isOpen = false;

    public string InteractionPrompt => "문 사용하기";

    [ContextMenu("Origin 세팅")]
    public void SetOriginDoorState()
    {
        _originState.Position = _doorTransform.localPosition;
        _originState.Rotation = _doorTransform.localRotation;
    }

    [ContextMenu("Open 세팅")]
    public void SetOpenDoorState()
    {
        _openState.Position = _doorTransform.localPosition;
        _openState.Rotation = _doorTransform.localRotation;
    }

    [ContextMenu("Set Origin")]
    public void ResetState()
    {
        _doorTransform.localPosition = _originState.Position;
        _doorTransform.localRotation = _originState.Rotation;
    }

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return !_isRotate;
    }
    public void InteractAct(PlayerInteractionController playerInteractor)
    {
        _isOpen = !_isOpen;
        _isRotate = true;
        StartCoroutine(ActDoorCo(_isOpen));
    }
    public void InteractRelease(PlayerInteractionController playerInteractionController)
    {
        
    }
    private IEnumerator ActDoorCo(bool isOpen)
    {
        Vector3 targetPosition = isOpen ? _openState.Position : _originState.Position;
        Quaternion targetQuaternion = isOpen ? _openState.Rotation : _originState.Rotation;

        Vector3 startPosotion = _doorTransform.localPosition;
        Quaternion startQuaternion = _doorTransform.localRotation;

        float timer = 0;

        while (timer < 1)
        {
            timer += Time.deltaTime * _rotateSpeed;

            _doorTransform.localPosition = Vector3.Lerp(startPosotion, targetPosition, timer);
            _doorTransform.localRotation = Quaternion.Lerp(startQuaternion, targetQuaternion, timer);

            yield return null;
        }

        _isRotate = false;
    }
}
