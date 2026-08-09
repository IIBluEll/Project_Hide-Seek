using HideSeek.AI;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ImpactNoiseEmitter : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _impactClip;
    [SerializeField, Min(0f)] private float _minimumImpactToPlay = 0.5f;
    [SerializeField, Min(0.01f)] private float _maximumImpactForVolume = 8f;
    [SerializeField, Range(0f, 1f)] private float _minimumVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _maximumVolume = 1f;
    [SerializeField, Min(0f)] private float _soundCooldown = 0.03f;

    [Header("AI Noise")]
    [SerializeField] private NOISE_TYPE _noiseType;

    [SerializeField] private float _radiusMutiple;
    [SerializeField] private float _intensityMultiple;

    private float _lastSoundTime = float.NegativeInfinity;

    private void Awake()
    {
        SetUpAudioSource();
    }

    public void OccursSound(Vector3 position, float impact)
    {
        if (impact < _minimumImpactToPlay)
            return;

        float normalizedImpact = Mathf.InverseLerp(_minimumImpactToPlay, _maximumImpactForVolume, impact);

        PlayImpactSound(normalizedImpact);
        EmitImpactNoise(position, impact);
    }

    private void PlayImpactSound(float normalizedImpact)
    {
        if (Time.time - _lastSoundTime < _soundCooldown)
            return;

        SetUpAudioSource();

        if (_source == null)
            return;

        AudioClip clip = _impactClip != null ? _impactClip : _source.clip;
        if (clip == null)
            return;

        _source.volume = Mathf.Lerp(_minimumVolume, _maximumVolume, normalizedImpact);
        _source.PlayOneShot(clip);
        _lastSoundTime = Time.time;
    }

    private void EmitImpactNoise(Vector3 position, float impact)
    {
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

        NoiseProvider.Emit(noiseData);
    }

    private void SetUpAudioSource()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.spatialBlend = 1f;
    }

    private void OnValidate()
    {
        _maximumImpactForVolume = Mathf.Max(_minimumImpactToPlay + 0.01f, _maximumImpactForVolume);
        _maximumVolume = Mathf.Max(_minimumVolume, _maximumVolume);
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
