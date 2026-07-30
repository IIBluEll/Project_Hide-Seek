using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIPerceptionTester : MonoBehaviour
    {
        [SerializeField] private ChaseAIPerception _perception;

        private CHASE_AI_VISUAL_STATE _previousState = CHASE_AI_VISUAL_STATE.NONE;

        private void Update()
        {
            if ( _perception == null )
            {
                return;
            }

            ChaseAIVisualObservation observation = _perception.UpdatePerception(Time.deltaTime);

            if ( observation.State == _previousState )
            {
                return;
            }

            Debug.Log(
                $"[PerceptionTester] 시야 상태 변경: " +
                $"{_previousState} → {observation.State}, " +
                $"인지율: {observation.DetectionRatio:F2}" ,
                this);

            _previousState = observation.State;
        }

        private void OnEnable()
        {
            if ( _perception == null )
            {
                return;
            }

            _perception.NoiseDetected -= OnNoiseDetected;
            _perception.NoiseDetected += OnNoiseDetected;
        }

        private void OnDisable()
        {
            if ( _perception == null )
            {
                return;
            }

            _perception.NoiseDetected -= OnNoiseDetected;
        }

        private void OnNoiseDetected(ChaseAIAudioObservation observation)
        {
            NoiseData noiseData = observation.NoiseData;

            string sourceName = noiseData.SourceObj != null ? noiseData.SourceObj.name : "Unknown";

            Debug.Log(
                $"[PerceptionTester] 소음 감지: " +
                $"Type={noiseData.NoiseType}, " +
                $"Source={sourceName}, " +
                $"Distance={observation.Distance:F1}, " +
                $"Intensity={observation.PerceivedIntensity:F2}" ,
                this);
        }
    }
}
