using UnityEngine;

public class GrapItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Collider _collider;

    public string InteractionPrompt => "ащ╠Б";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return true;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        playerInteractor.TryGrap(this);
    }

    public void Grapped()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _collider.isTrigger = true;
    }
    public void Release()
    {
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _collider.isTrigger = false;
    }

    public void Throw(Vector3 direction, float power)
    {
        Debug.Log(power);
        Release();
        _rb.AddForce(direction.normalized * power, ForceMode.Impulse);
    }
}
