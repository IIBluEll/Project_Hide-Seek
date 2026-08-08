using HideSeek.AI;
using UnityEngine;

public class GrappableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Collider _collider;
    [SerializeField] private ImpactNoiseEmitter _impactNoiseEmitter;

    private bool _isThrow = false;

    public string InteractionPrompt => "ащ╠Б";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return true;
    }
    public void InteractAct(PlayerInteractionController playerInteractor)
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
        Release();
        _rb.AddForce(direction.normalized * power, ForceMode.Impulse);

        _isThrow = true;
    }
    public void InteractRelease(PlayerInteractionController playerInteractionController)
    {
        
    }


    public void OnCollisionEnter(Collision collision)
    {
        if (!_isThrow)
            return;

        var impactAmount = collision.relativeVelocity.magnitude;
        _impactNoiseEmitter.OccursSound(this.transform.position, impactAmount);

        _isThrow = false;
    }
}
