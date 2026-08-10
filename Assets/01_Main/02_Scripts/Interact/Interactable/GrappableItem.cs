using HideSeek.AI;
using UnityEngine;

public class GrappableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Collider _collider;
    [SerializeField] private ImpactNoiseEmitter _impactNoiseEmitter;
    [SerializeField] private LayerMask _impactLayerMask = ~0;
    [SerializeField, Min(0f)] private float _minimumImpactAmount = 0.1f;

    private bool _isThrow = false;

    public string InteractionPrompt => "줍기";

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
        _isThrow = false;
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

        if (!IsImpactLayer(collision.gameObject.layer))
            return;

        float impactAmount = collision.relativeVelocity.magnitude;
        if (impactAmount < _minimumImpactAmount)
            return;

        Vector3 impactPosition = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        _impactNoiseEmitter?.OccursSound(impactPosition, impactAmount);
    }

    private bool IsImpactLayer(int layer)
    {
        return (_impactLayerMask.value & (1 << layer)) != 0;
    }
}
