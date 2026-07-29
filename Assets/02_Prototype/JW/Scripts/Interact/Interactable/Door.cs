using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _DoorHindge;
    [SerializeField] private float _rotateSpeed;

    private Vector3 _openRotaion = new Vector3(0, -120, 0);
    private Vector3 _closeRotaion = new Vector3(0, 0, 0);

    private bool _isRotate = false;
    private bool _isOpen = false;

    public string InteractionPrompt => "문 사용하기";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return !_isRotate;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        _isOpen = !_isOpen;

        StartCoroutine(RoateDoorCo(_isOpen));
    }

    private IEnumerator RoateDoorCo(bool isOpen)
    {
        float timer = 0;

        _isRotate = true;

        Vector3 start = isOpen ? _closeRotaion : _openRotaion;
        Vector3 end = isOpen ? _openRotaion : _closeRotaion;

        while(timer < 1)
        {
            timer += Time.deltaTime * _rotateSpeed;

            _DoorHindge.rotation = Quaternion.Euler(Vector3.Lerp(start, end, timer));

            yield return null;
        }

        _isRotate = false;
    }
}
