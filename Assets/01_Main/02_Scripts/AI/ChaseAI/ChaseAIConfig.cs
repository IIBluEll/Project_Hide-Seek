using UnityEngine;

namespace HideSeek.AI
{
    [CreateAssetMenu(fileName = "ChaseAIConfig" , menuName = "HideSeek/AI/Chase AI Config")]
    public sealed class ChaseAIConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float _acceleration = 12f;
        [SerializeField, Min(0f)] private float _angularSpeed = 360f;
        [SerializeField, Min(0f)] private float _stoppingDistance = 0.2f;

        [Header("NavMesh Validation")]
        [SerializeField, Min(0.1f)] private float _sampleRadius = 2f;
        [SerializeField, Min(0f)] private float _arrivalTolerance = 0.1f;
        [SerializeField, Min(0f)] private float _arrivalVelocityThreshold = 0.05f;

        [Header("Stuck Detection")]
        [SerializeField, Min(0f)] private float _stuckVelocityThreshold = 0.05f;
        [SerializeField, Min(0.1f)] private float _stuckTimeLimit = 2f;

        [Header("Visual Perception")]
        [SerializeField, Min(0.1f)] private float _sightRange = 15f;
        [SerializeField, Range(1f, 360f)] private float _horizontalSightAngle = 100f;
        [SerializeField, Range(1f, 180f)] private float _verticalSightAngle = 60f;
        [SerializeField, Min(0.01f)] private float _visualConfirmTime = 0.8f;
        [SerializeField, Min(0.01f)] private float _visualLoseTime = 0.3f;

        [Header("Memory")]
        [SerializeField, Min(0.1f)] private float _visualEvidenceDuration = 10f;
        [SerializeField, Min(0.1f)] private float _weakNoiseEvidenceDuration = 4f;
        [SerializeField, Min(0.1f)] private float _strongNoiseEvidenceDuration = 8f;
        [SerializeField, Min(0f)] private float _strongNoiseThreshold = 0.5f;

        public float WalkSpeed => _walkSpeed;
        public float Acceleration => _acceleration;
        public float AngularSpeed => _angularSpeed;
        public float StoppingDistance => _stoppingDistance;

        public float SampleRadius => _sampleRadius;
        public float ArrivalTolerance => _arrivalTolerance;
        public float ArrivalVelocityThreshold => _arrivalVelocityThreshold;

        public float StuckVelocityThreshold => _stuckVelocityThreshold;
        public float StuckTimeLimit => _stuckTimeLimit;

        public float SightRange => _sightRange;
        public float HorizontalSightAngle => _horizontalSightAngle;
        public float VerticalSightAngle => _verticalSightAngle;
        public float VisualConfirmTime => _visualConfirmTime;
        public float VisualLoseTime => _visualLoseTime;

        public float VisualEvidenceDuration => _visualEvidenceDuration;
        public float WeakNoiseEvidenceDuration => _weakNoiseEvidenceDuration;
        public float StrongNoiseEvidenceDuration => _strongNoiseEvidenceDuration;
        public float StrongNoiseThreshold => _strongNoiseThreshold;
    }
}
