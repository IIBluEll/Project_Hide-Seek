using UnityEngine;

namespace HideSeek.AI
{
    public enum NOISE_TYPE
    {
        FOOTSTEP,
        RUN,
        DECOY,
        GENERATOR,
        QTE_FAILURE,
        DOOR,
        ENVIRONMENT
    }

    public readonly struct NoiseData
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public float Intensity { get; }
        public NOISE_TYPE NoiseType { get; }
        public float OccurredTime { get; }
        public GameObject SourceObj { get; }

        public NoiseData(
            Vector3 position ,
            float radius ,
            float intensity ,
            NOISE_TYPE noiseType ,
            float occurredTime ,
            GameObject sourceObj)
        {
            Position = position;
            Radius = radius;
            Intensity = intensity;
            NoiseType = noiseType;
            OccurredTime = occurredTime;
            SourceObj = sourceObj;
        }

        public bool IsValid()
        {
            return Radius > 0f && Intensity > 0f;
        }
    }
}
