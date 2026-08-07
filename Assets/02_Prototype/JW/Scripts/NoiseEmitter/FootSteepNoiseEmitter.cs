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

        if (NoiseProvider.Emit(data))
        {
            Debug.Log(
           $"[NoiseEmitter] {data.NoiseType} 소음 발생, " +
           $"반경: {data.Radius:F1}, 강도: {data.Intensity:F2}",
           this);
        }
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
            _noiseInterval = _walkNoise.NoiseInterval;
        else if (locomotion == LOCOMOTION_STATE_ENUM.RUN)
            _noiseInterval = _runNoise.NoiseInterval;
    }
}
