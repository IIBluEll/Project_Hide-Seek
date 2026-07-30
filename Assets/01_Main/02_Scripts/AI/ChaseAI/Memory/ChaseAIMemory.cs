using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIMemory
    {
        private const float MOVEMENT_DIRECTION_MIN_SQUARED_DISTANCE = 0.0001f;

        private Vector3 _previousSeenPosition;
        private bool _hasPreviousSeenPosition;

        public ChaseAIEvidence VisualEvidence
        {
            get;
            private set;
        } = ChaseAIEvidence.Empty;

        public ChaseAIEvidence AudioEvidence
        {
            get;
            private set;
        } = ChaseAIEvidence.Empty;

        public Vector3 LastSeenMovementDirection
        {
            get;
            private set;
        }

        public NOISE_TYPE LastNoiseType
        {
            get;
            private set;
        }

        public void RecordVisualEvidence(ChaseAIVisualObservation observation, float currentTime, float duration)
        {
            if ( !observation.HasLineOfSight || observation.State != CHASE_AI_VISUAL_STATE.CONFIRMED )
            {
                return;
            }

            UpdateMovementDirection(observation.VisiblePosition);

            VisualEvidence = new ChaseAIEvidence(
                CHASE_AI_EVIDENCE_TYPE.VISUAL ,
                observation.VisiblePosition ,
                currentTime ,
                duration ,
                observation.DetectionRatio);
        }

        public void RecordAudioEvidence(ChaseAIAudioObservation observation, float duration)
        {
            NoiseData noiseData = observation.NoiseData;

            AudioEvidence = new ChaseAIEvidence(
                CHASE_AI_EVIDENCE_TYPE.AUDIO ,
                noiseData.Position ,
                noiseData.OccurredTime ,
                duration ,
                observation.PerceivedIntensity);

            LastNoiseType = noiseData.NoiseType;
        }

        public void UpdateMemory(float currentTime)
        {
            if ( !VisualEvidence.IsValid(currentTime) )
            {
                ClearVisualEvidence();
            }

            if ( !AudioEvidence.IsValid(currentTime) )
            {
                ClearAudioEvidence();
            }
        }

        public bool HasValidVisualEvidence(float currentTime)
        {
            return VisualEvidence.IsValid(currentTime);
        }

        public bool HasValidAudioEvidence(float currentTime)
        {
            return AudioEvidence.IsValid(currentTime);
        }

        public void Clear()
        {
            ClearVisualEvidence();
            ClearAudioEvidence();
        }

        private void UpdateMovementDirection(Vector3 currentSeenPosition)
        {
            if ( !_hasPreviousSeenPosition )
            {
                _previousSeenPosition = currentSeenPosition;
                _hasPreviousSeenPosition = true;
                return;
            }

            Vector3 movement = currentSeenPosition - _previousSeenPosition;
            movement.y = 0f;

            if ( movement.sqrMagnitude >= MOVEMENT_DIRECTION_MIN_SQUARED_DISTANCE )
            {
                LastSeenMovementDirection = movement.normalized;
            }

            _previousSeenPosition = currentSeenPosition;
        }

        private void ClearVisualEvidence()
        {
            VisualEvidence = ChaseAIEvidence.Empty;
            LastSeenMovementDirection = Vector3.zero;
            _previousSeenPosition = Vector3.zero;
            _hasPreviousSeenPosition = false;
        }

        private void ClearAudioEvidence()
        {
            AudioEvidence = ChaseAIEvidence.Empty;
        }
    }
}
