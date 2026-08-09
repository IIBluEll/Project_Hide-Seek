using HideSeek.AI;
using UnityEngine;

public class ImpactNoiseEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource _source;
    [SerializeField] private NOISE_TYPE _noiseType;

    [SerializeField] private float _radiusMutiple;
    [SerializeField] private float _intensityMultiple;

    public void OccursSound(Vector3 position, float impact)
    {
        if(_source != null)
            _source.Play();

        Debug.Log(impact);

        float applyRadius = impact * _radiusMutiple;
        float applyIntensity = impact * _intensityMultiple;

        _radius = applyRadius;

        NoiseData noiseData = new NoiseData(
               position,
               applyRadius,
               applyIntensity,
               _noiseType,
               Time.time,
               gameObject);

        bool wasEmitted = NoiseProvider.Emit(noiseData);
    }

    public float _radius;
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(
            1f,
            0.5f,
            0f,
            0.35f);

        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
