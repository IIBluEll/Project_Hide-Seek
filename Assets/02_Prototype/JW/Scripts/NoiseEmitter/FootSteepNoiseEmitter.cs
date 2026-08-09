using HideSeek.AI;
using UnityEngine;

[System.Serializable]
public struct Noise
{
    public NOISE_TYPE NoiseType;
    [Min(0.1f)] public float Radius;
    [Min(0.01f)] public float Intensity;
    [Min(0.01f)] public float NoiseInterval;
}

public class FootSteepNoiseEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _walkClip;
    [SerializeField] private AudioClip _runClip;

    [SerializeField] private Noise _walkNoise;
    [SerializeField] private Noise _runNoise;

    private float _noiseInterval;
    private float _currentTime;

    private LOCOMOTION_STATE_ENUM _footStepType = LOCOMOTION_STATE_ENUM.IDLE;

    private void Update()
    {
        if (_footStepType == LOCOMOTION_STATE_ENUM.IDLE)
            return;

        _currentTime += Time.deltaTime;

        if (_currentTime < _noiseInterval)
            return;

        _currentTime = 0;
        NoiseData data = default;

        switch (_footStepType)
        {
            case LOCOMOTION_STATE_ENUM.WALK:
                data = GetNoiseData(_walkNoise.Radius, _walkNoise.Intensity, NOISE_TYPE.FOOTSTEP);
                break;
            case LOCOMOTION_STATE_ENUM.RUN:
                data = GetNoiseData(_runNoise.Radius, _runNoise.Intensity, NOISE_TYPE.RUN);
                break;
        }

        NoiseProvider.Emit(data);
    }

    private NoiseData GetNoiseData(float radius, float intensity, NOISE_TYPE type)
    {
        return new NoiseData(
               this.transform.position,
               radius,
               intensity,
               NOISE_TYPE.FOOTSTEP,
               Time.time,
               gameObject);
    }

    public void OnChangedPlayerFootStep(LOCOMOTION_STATE_ENUM locomotion)
    {
        _footStepType = locomotion;

        if (locomotion == LOCOMOTION_STATE_ENUM.WALK)
        {
            _noiseInterval = _walkNoise.NoiseInterval;
        }
        else if (locomotion == LOCOMOTION_STATE_ENUM.RUN)
        {
            _noiseInterval = _runNoise.NoiseInterval;
        }
    }

    public void UpdateFootSound(LOCOMOTION_STATE_ENUM locomotion, POSTURE_STATE_ENUM posutre)
    {
        if (posutre == POSTURE_STATE_ENUM.CROUCH)
        {
            _source.Stop();
            return;
        }

        AudioClip clip = null;

        if (locomotion == LOCOMOTION_STATE_ENUM.WALK)
        {
            _noiseInterval = _walkNoise.NoiseInterval;
            clip = _walkClip;
        }
        else if (locomotion == LOCOMOTION_STATE_ENUM.RUN)
        {
            _noiseInterval = _runNoise.NoiseInterval;
            clip = _runClip;
        }

        if (clip != null)
        {
            _source.clip = clip;
            _source.Play();
        }
        else
            _source.Stop();
    }
}
