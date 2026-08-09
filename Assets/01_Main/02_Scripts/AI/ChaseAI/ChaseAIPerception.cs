using System;
using UnityEngine;

namespace HideSeek.AI
{
    public enum CHASE_AI_VISUAL_STATE
    {
        NONE,
        SUSPICIOUS,
        CONFIRMED
    }

    public readonly struct ChaseAIVisualObservation
    {
        public bool HasLineOfSight { get; }
        public CHASE_AI_VISUAL_STATE State { get; }
        public Vector3 VisiblePosition { get; }
        public float DetectionRatio { get; }
        public float DetectionSpeedMultiplier { get; }
        public bool CanAttackTarget { get; }

        public ChaseAIVisualObservation(
            bool hasLineOfSight ,
            CHASE_AI_VISUAL_STATE state ,
            Vector3 visiblePosition ,
            float detectionRatio ,
            float detectionSpeedMultiplier ,
            bool canAttackTarget)
        {
            HasLineOfSight = hasLineOfSight;
            State = state;
            VisiblePosition = visiblePosition;
            DetectionRatio = detectionRatio;
            DetectionSpeedMultiplier = detectionSpeedMultiplier;
            CanAttackTarget = canAttackTarget;
        }
    }

    public readonly struct ChaseAIAudioObservation
    {
        public NoiseData NoiseData { get; }
        public float Distance { get; }
        public float PerceivedIntensity { get; }

        public ChaseAIAudioObservation(
            NoiseData noiseData ,
            float distance ,
            float perceivedIntensity)
        {
            NoiseData = noiseData;
            Distance = distance;
            PerceivedIntensity = perceivedIntensity;
        }
    }

    public sealed class ChaseAIPerception : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChaseAIConfig _config;
        [SerializeField] private Transform _eyeTransform;
        [SerializeField] private Transform _targetTransform;

        [Header("Collision")]
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Hearing")]
        [SerializeField] private Transform _hearingTransform;

        private global::IPlayerVisibilityState _targetVisibilityState;
        public event Action<ChaseAIAudioObservation> NoiseDetected;

        private float _detectionRatio;

        public bool HasTargetVisibilityState => _targetVisibilityState != null;
        public bool IsTargetFullyHidden =>
            _targetVisibilityState != null &&
            _targetVisibilityState.IsFullyHidden;

        public ChaseAIVisualObservation CurrentObservation
        {
            get;
            private set;
        }

        private void Awake()
        {
            if ( _config == null )
            {
                Debug.LogError(
                    "[ChaseAIPerception] ChaseAIConfig가 할당되지 않았습니다." ,
                    this);
            }

            if ( _eyeTransform == null )
            {
                Debug.LogError(
                    "[ChaseAIPerception] Eye Transform이 할당되지 않았습니다." ,
                    this);
            }

            if ( _hearingTransform == null )
            {
                _hearingTransform = transform;
            }
        }

        private void OnEnable()
        {
            NoiseProvider.NoiseEmitted -= OnNoiseEmitted;
            NoiseProvider.NoiseEmitted += OnNoiseEmitted;
        }

        private void OnDisable()
        {
            NoiseProvider.NoiseEmitted -= OnNoiseEmitted;
        }

        public void SetTarget(Transform targetTransform)
        {
            _targetTransform = targetTransform;
            _targetVisibilityState = FindTargetVisibilityState(targetTransform);
            ResetPerception();
        }

        public bool IsTargetInsideHidingSpot(AIHidingSpot hidingSpot)
        {
            return hidingSpot != null &&
                hidingSpot.ContainsPlayer(_targetTransform , _targetVisibilityState);
        }

        public ChaseAIVisualObservation UpdatePerception(float deltaTime)
        {
            bool hasLineOfSight = TryGetVisiblePosition(
                out Vector3 visiblePosition ,
                out float detectionSpeedMultiplier);

            UpdateDetectionRatio(hasLineOfSight , detectionSpeedMultiplier , deltaTime);

            CHASE_AI_VISUAL_STATE visualState = GetVisualState(hasLineOfSight);
            bool canAttackTarget = CanAttackTarget();

            CurrentObservation = new ChaseAIVisualObservation(
                hasLineOfSight ,
                visualState ,
                hasLineOfSight ? visiblePosition : Vector3.zero ,
                _detectionRatio ,
                hasLineOfSight ? detectionSpeedMultiplier : 0f ,
                canAttackTarget);

            return CurrentObservation;
        }

        public void ResetPerception()
        {
            _detectionRatio = 0f;

            CurrentObservation = new ChaseAIVisualObservation(
                false ,
                CHASE_AI_VISUAL_STATE.NONE ,
                Vector3.zero ,
                0f ,
                0f ,
                false);
        }

        private bool CanAttackTarget()
        {
            if ( _config == null ||
                _eyeTransform == null ||
                _targetTransform == null )
            {
                return false;
            }

            if ( IsTargetFullyHidden )
            {
                return false;
            }

            Vector3 directionToTarget = _targetTransform.position - transform.position;
            directionToTarget.y = 0f;

            float attackRange = _config.AttackRange;

            if ( directionToTarget.sqrMagnitude > attackRange * attackRange )
            {
                return false;
            }

            Vector3 attackRayDirection = _targetTransform.position - _eyeTransform.position;
            float attackRayDistance = attackRayDirection.magnitude;

            if ( attackRayDistance <= Mathf.Epsilon )
            {
                return true;
            }

            bool isBlocked = Physics.Raycast(
                _eyeTransform.position ,
                attackRayDirection / attackRayDistance ,
                attackRayDistance ,
                _obstacleMask ,
                QueryTriggerInteraction.Ignore);

            return !isBlocked;
        }

        private bool TryGetVisiblePosition(
            out Vector3 visiblePosition ,
            out float detectionSpeedMultiplier)
        {
            visiblePosition = Vector3.zero;
            detectionSpeedMultiplier = 0f;

            if ( _config == null || _eyeTransform == null || _targetTransform == null )
            {
                return false;
            }

            if ( IsTargetFullyHidden )
            {
                return false;
            }

            Vector3 targetPosition = _targetTransform.position;
            Vector3 directionToTarget = targetPosition - _eyeTransform.position;

            float distanceToTarget = directionToTarget.magnitude;

            if ( distanceToTarget > _config.SightRange )
            {
                return false;
            }

            if ( distanceToTarget <= Mathf.Epsilon )
            {
                visiblePosition = targetPosition;
                detectionSpeedMultiplier = 1f;
                return true;
            }

            Vector3 normalizedDirection = directionToTarget / distanceToTarget;

            Vector3 localDirection = _eyeTransform.InverseTransformDirection(normalizedDirection);

            if ( localDirection.z <= 0f )
            {
                return false;
            }

            float horizontalAngle = Mathf.Atan2(
                localDirection.x,
                localDirection.z) * Mathf.Rad2Deg;

            float horizontalLength = new Vector2(
                localDirection.x,
                localDirection.z).magnitude;

            float verticalAngle = Mathf.Atan2(
                localDirection.y,
                horizontalLength) * Mathf.Rad2Deg;

            if ( Mathf.Abs(horizontalAngle) >
                _config.HorizontalSightAngle * 0.5f )
            {
                return false;
            }

            if ( Mathf.Abs(verticalAngle) >
                _config.VerticalSightAngle * 0.5f )
            {
                return false;
            }

            bool isBlocked = Physics.Raycast(
                _eyeTransform.position,
                normalizedDirection,
                distanceToTarget,
                _obstacleMask,
                QueryTriggerInteraction.Ignore);

            if ( isBlocked )
            {
                return false;
            }

            visiblePosition = targetPosition;
            detectionSpeedMultiplier = CalculateDetectionSpeedMultiplier(
                distanceToTarget ,
                horizontalAngle ,
                verticalAngle);
            return true;
        }

        private void UpdateDetectionRatio(
            bool hasLineOfSight ,
            float detectionSpeedMultiplier ,
            float deltaTime)
        {
            if ( hasLineOfSight )
            {
                float increaseAmount =
                    deltaTime /
                    _config.VisualConfirmTime *
                    Mathf.Clamp01(detectionSpeedMultiplier);

                _detectionRatio = Mathf.Clamp01(_detectionRatio + increaseAmount);

                return;
            }

            float decreaseAmount = deltaTime / _config.VisualLoseTime;

            _detectionRatio = Mathf.Clamp01(_detectionRatio - decreaseAmount);
        }

        private float CalculateDetectionSpeedMultiplier(
            float distanceToTarget ,
            float horizontalAngle ,
            float verticalAngle)
        {
            float distanceRatio = Mathf.Clamp01(distanceToTarget / _config.SightRange);
            float halfHorizontalAngle = Mathf.Max(0.5f , _config.HorizontalSightAngle * 0.5f);
            float halfVerticalAngle = Mathf.Max(0.5f , _config.VerticalSightAngle * 0.5f);
            float horizontalPeripheralRatio = Mathf.Clamp01(Mathf.Abs(horizontalAngle) / halfHorizontalAngle);
            float verticalPeripheralRatio = Mathf.Clamp01(Mathf.Abs(verticalAngle) / halfVerticalAngle);
            float peripheralRatio = Mathf.Max(horizontalPeripheralRatio , verticalPeripheralRatio);

            float distanceMultiplier = Mathf.Lerp(
                1f ,
                _config.MinimumDistanceDetectionMultiplier ,
                distanceRatio);
            float peripheralMultiplier = Mathf.Lerp(
                1f ,
                _config.MinimumPeripheralDetectionMultiplier ,
                peripheralRatio);

            return Mathf.Clamp01(distanceMultiplier * peripheralMultiplier);
        }

        private static global::IPlayerVisibilityState FindTargetVisibilityState(
            Transform targetTransform)
        {
            if ( targetTransform == null )
            {
                return null;
            }

            MonoBehaviour[] targetComponents =
                targetTransform.GetComponentsInParent<MonoBehaviour>(true);

            for ( int componentIndex = 0; componentIndex < targetComponents.Length; componentIndex++ )
            {
                if ( targetComponents[ componentIndex ] is
                     global::IPlayerVisibilityState visibilityState )
                {
                    return visibilityState;
                }
            }

            return null;
        }

        private CHASE_AI_VISUAL_STATE GetVisualState(bool hasLineOfSight)
        {
            if ( !hasLineOfSight )
            {
                return CHASE_AI_VISUAL_STATE.NONE;
            }

            if ( _detectionRatio >= 1f )
            {
                return CHASE_AI_VISUAL_STATE.CONFIRMED;
            }

            return CHASE_AI_VISUAL_STATE.SUSPICIOUS;
        }

        private void OnNoiseEmitted(NoiseData noiseData)
        {
            if ( _hearingTransform == null )
            {
                return;
            }

            Vector3 directionToNoise = noiseData.Position - _hearingTransform.position;

            float squaredDistance = directionToNoise.sqrMagnitude;
            float squaredRadius = noiseData.Radius * noiseData.Radius;

            if ( squaredDistance > squaredRadius )
            {
                return;
            }

            float distance = Mathf.Sqrt(squaredDistance);

            float distanceRatio = Mathf.Clamp01(distance / noiseData.Radius);

            float attenuation = 1f - distanceRatio;

            float perceivedIntensity = noiseData.Intensity * attenuation;

            ChaseAIAudioObservation observation = new ChaseAIAudioObservation(noiseData, distance, perceivedIntensity);

            NoiseDetected?.Invoke(observation);
        }

        private void OnDrawGizmos()
        {
            if ( _config == null || _eyeTransform == null )
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_eyeTransform.position , _config.SightRange);

            float halfHorizontal = _config.HorizontalSightAngle * 0.5f;

            float halfVertical = _config.VerticalSightAngle * 0.5f;

            DrawSightRay(-halfVertical , -halfHorizontal);
            DrawSightRay(-halfVertical , halfHorizontal);
            DrawSightRay(halfVertical , -halfHorizontal);
            DrawSightRay(halfVertical , halfHorizontal);

            if ( _targetTransform == null )
            {
                return;
            }

            bool hasLineOfSight = TryGetVisiblePosition(
                out Vector3 visiblePosition ,
                out _);

            Gizmos.color = hasLineOfSight ? Color.green : Color.red;

            Gizmos.DrawLine(_eyeTransform.position , hasLineOfSight ? visiblePosition : _targetTransform.position);
        }

        private void DrawSightRay(float verticalAngle , float horizontalAngle)
        {
            Quaternion directionRotation = _eyeTransform.rotation * Quaternion.Euler(verticalAngle, horizontalAngle, 0f);

            Vector3 direction = directionRotation * Vector3.forward;

            Gizmos.DrawRay(_eyeTransform.position ,direction * _config.SightRange);
        }
    }
}
