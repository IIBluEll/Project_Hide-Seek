using HideSeek.AI;
using UnityEngine;

[System.Serializable]
public struct Noise
{
    public NOISE_TYPE NoiseType;
    [Min(0.1f)] public float Radius;
    [Min(0.01f)] public float Intensity;
}

public class FootSteepNoiseEmitter : MonoBehaviour
{
    [SerializeField] private Noise _walkNoise;
    [SerializeField] private Noise _runNoise;
    public NoiseData noiseData;
    public void OccurredFootNoise(LOCOMOTION_STATE_ENUM locomotion)
    {
        bool wasEmitted = false;
        switch (locomotion)
        {
            case LOCOMOTION_STATE_ENUM.WALK:
                noiseData = new NoiseData(
                this.transform.position,
                _walkNoise.Radius,
                _walkNoise.Intensity,
                _walkNoise.NoiseType,
                Time.time,
                gameObject);
                wasEmitted = NoiseProvider.Emit(noiseData);
                break;
            case LOCOMOTION_STATE_ENUM.RUN:
                noiseData = new NoiseData(
                this.transform.position,
                _runNoise.Radius,
                _runNoise.Intensity,
                _runNoise.NoiseType,
                Time.time,
                gameObject);
                wasEmitted = NoiseProvider.Emit(noiseData);
                break;
        }

        if (!wasEmitted)
        {
            return;
        }

        Debug.Log(
            $"[NoiseEmitter] {noiseData.NoiseType} 소음 발생, " +
            $"반경: {noiseData.Radius:F1}, 강도: {noiseData.Intensity:F2}",
            this);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(
            1f,
            0.5f,
            0f,
            0.35f);

        Gizmos.DrawWireSphere(transform.position, noiseData.Radius);
    }
}
