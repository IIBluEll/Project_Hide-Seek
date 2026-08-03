using System.Collections.Generic;
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

        [Header("Anger")]
        [SerializeField, Min(1f)] private float _maximumAnger = 100f;
        [SerializeField] private List<float> _generatorAngerFloors = new() { 0f , 20f , 40f , 60f };
        [SerializeField, Min(1f)] private float _maximumAngerChaseSpeedMultiplier = 1.15f;
        [SerializeField, Min(1f)] private float _maximumAngerSearchRadiusMultiplier = 1.25f;
        [SerializeField, Min(1)] private int _minimumAngerSearchPointCount = 2;
        [SerializeField, Min(1)] private int _maximumAngerSearchPointCount = 4;
        [SerializeField, Range(0f, 1f)] private float _directorHintAngerInfluence = 0.5f;

        [Header("Audio Search")]
        [SerializeField, Min(0f)] private float _minAudioSearchRadius = 2f;
        [SerializeField, Min(0f)] private float _maxAudioSearchRadius = 8f;
        [SerializeField, Min(0f)] private float _minAudioSearchDuration = 2f;
        [SerializeField, Min(0f)] private float _maxAudioSearchDuration = 8f;

        [Header("Search")]
        [SerializeField, Min(0f)] private float _visualSearchRadius = 5f;
        [SerializeField, Min(0f)] private float _lastSeenPredictionDistance = 3f;
        [SerializeField, Range(0f, 1f)] private float _directionalSearchPointRatio = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _zoneCoverageSearchPointRatio = 0.3f;
        [SerializeField, Range(0f, 180f)] private float _directionalSearchAngle = 120f;
        [SerializeField, Min(0f)] private float _minimumSearchPointDistance = 1.5f;
        [SerializeField, Min(0f)] private float _hidingSpotEvidenceDistance = 2.5f;
        [SerializeField, Range(0f, 1f)] private float _visualHidingSpotInspectionChance = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _strongAudioHidingSpotInspectionChance = 0.35f;
        [SerializeField, Min(1)] private int _searchPointGenerationAttemptCountPerPoint = 10;

        [Header("Search Action")]
        [SerializeField, Min(0f)] private float _directionalSearchActionTimeMultiplier = 0.8f;
        [SerializeField, Min(0f)] private float _areaSearchActionTimeMultiplier = 1f;
        [SerializeField, Min(0f)] private float _hidingSpotSearchActionTimeMultiplier = 1.6f;

        [Header("State Machine")]
        [SerializeField, Min(0f)] private float _chaseSpeed = 5.5f;
        [SerializeField, Min(0f)] private float _patrolWaitTime = 1f;
        [SerializeField, Min(0f)] private float _investigateWaitTime = 2f;
        [SerializeField, Min(0f)] private float _searchWaitTime = 3f;
        [SerializeField, Min(0.02f)] private float _chaseRepathInterval = 0.2f;
        [SerializeField, Min(0f)] private float _chaseDestinationUpdateDistance = 0.5f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float _attackRange = 1.5f;

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

        public float MaximumAnger => _maximumAnger;
        public float MaximumAngerChaseSpeedMultiplier => _maximumAngerChaseSpeedMultiplier;
        public float MaximumAngerSearchRadiusMultiplier => _maximumAngerSearchRadiusMultiplier;
        public int MinimumAngerSearchPointCount => _minimumAngerSearchPointCount;
        public int MaximumAngerSearchPointCount => _maximumAngerSearchPointCount;
        public float DirectorHintAngerInfluence => _directorHintAngerInfluence;

        public float MinAudioSearchRadius => _minAudioSearchRadius;
        public float MaxAudioSearchRadius => _maxAudioSearchRadius;
        public float MinAudioSearchDuration => _minAudioSearchDuration;
        public float MaxAudioSearchDuration => _maxAudioSearchDuration;

        public float VisualSearchRadius => _visualSearchRadius;
        public float LastSeenPredictionDistance => _lastSeenPredictionDistance;
        public float DirectionalSearchPointRatio => _directionalSearchPointRatio;
        public float ZoneCoverageSearchPointRatio => _zoneCoverageSearchPointRatio;
        public float DirectionalSearchAngle => _directionalSearchAngle;
        public float MinimumSearchPointDistance => _minimumSearchPointDistance;
        public float HidingSpotEvidenceDistance => _hidingSpotEvidenceDistance;
        public float VisualHidingSpotInspectionChance => _visualHidingSpotInspectionChance;
        public float StrongAudioHidingSpotInspectionChance => _strongAudioHidingSpotInspectionChance;
        public int SearchPointGenerationAttemptCountPerPoint => _searchPointGenerationAttemptCountPerPoint;
        public float DirectionalSearchActionTimeMultiplier => _directionalSearchActionTimeMultiplier;
        public float AreaSearchActionTimeMultiplier => _areaSearchActionTimeMultiplier;
        public float HidingSpotSearchActionTimeMultiplier => _hidingSpotSearchActionTimeMultiplier;

        public float ChaseSpeed => _chaseSpeed;
        public float PatrolWaitTime => _patrolWaitTime;
        public float InvestigateWaitTime => _investigateWaitTime;
        public float SearchWaitTime => _searchWaitTime;
        public float ChaseRepathInterval => _chaseRepathInterval;

        public float ChaseDestinationUpdateDistance => _chaseDestinationUpdateDistance;
        public float AttackRange => _attackRange;

        public float GetGeneratorAngerFloor(int completedGeneratorCount)
        {
            if ( _generatorAngerFloors == null || _generatorAngerFloors.Count == 0 )
            {
                return 0f;
            }

            int floorIndex = Mathf.Clamp(completedGeneratorCount , 0 , _generatorAngerFloors.Count - 1);

            return _generatorAngerFloors[ floorIndex ];
        }

        private void OnValidate()
        {
            _maximumAnger = Mathf.Max(1f , _maximumAnger);
            _maximumAngerChaseSpeedMultiplier = Mathf.Max(1f , _maximumAngerChaseSpeedMultiplier);
            _maximumAngerSearchRadiusMultiplier = Mathf.Max(1f , _maximumAngerSearchRadiusMultiplier);
            _minimumAngerSearchPointCount = Mathf.Max(1 , _minimumAngerSearchPointCount);
            _maximumAngerSearchPointCount = Mathf.Max(_minimumAngerSearchPointCount , _maximumAngerSearchPointCount);
            _lastSeenPredictionDistance = Mathf.Max(0f , _lastSeenPredictionDistance);
            _directionalSearchPointRatio = Mathf.Clamp01(_directionalSearchPointRatio);
            _zoneCoverageSearchPointRatio = Mathf.Clamp01(_zoneCoverageSearchPointRatio);
            _directionalSearchAngle = Mathf.Clamp(_directionalSearchAngle , 0f , 180f);
            _hidingSpotEvidenceDistance = Mathf.Max(0f , _hidingSpotEvidenceDistance);
            _visualHidingSpotInspectionChance = Mathf.Clamp01(_visualHidingSpotInspectionChance);
            _strongAudioHidingSpotInspectionChance = Mathf.Clamp01(_strongAudioHidingSpotInspectionChance);
            _directionalSearchActionTimeMultiplier = Mathf.Max(0f , _directionalSearchActionTimeMultiplier);
            _areaSearchActionTimeMultiplier = Mathf.Max(0f , _areaSearchActionTimeMultiplier);
            _hidingSpotSearchActionTimeMultiplier = Mathf.Max(0f , _hidingSpotSearchActionTimeMultiplier);
            _attackRange = Mathf.Max(0f , _attackRange);

            if ( _generatorAngerFloors == null || _generatorAngerFloors.Count == 0 )
            {
                _generatorAngerFloors = new List<float> { 0f };
            }

            float previousAngerFloor = 0f;

            for ( int floorIndex = 0; floorIndex < _generatorAngerFloors.Count; floorIndex++ )
            {
                float angerFloor = Mathf.Clamp(_generatorAngerFloors[ floorIndex ] , previousAngerFloor , _maximumAnger);

                _generatorAngerFloors[ floorIndex ] = angerFloor;
                previousAngerFloor = angerFloor;
            }
        }
    }
}
