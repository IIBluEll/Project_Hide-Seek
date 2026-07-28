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

        public ChaseAIVisualObservation(
            bool hasLineOfSight ,
            CHASE_AI_VISUAL_STATE state ,
            Vector3 visiblePosition ,
            float detectionRatio)
        {
            HasLineOfSight = hasLineOfSight;
            State = state;
            VisiblePosition = visiblePosition;
            DetectionRatio = detectionRatio;
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

        private float _detectionRatio;

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
        }

        public void SetTarget(Transform targetTransform)
        {
            _targetTransform = targetTransform;
            ResetPerception();
        }

        public ChaseAIVisualObservation UpdatePerception(float deltaTime)
        {
            bool hasLineOfSight = TryGetVisiblePosition(out Vector3 visiblePosition);

            UpdateDetectionRatio(hasLineOfSight , deltaTime);

            CHASE_AI_VISUAL_STATE visualState = GetVisualState(hasLineOfSight);

            CurrentObservation = new ChaseAIVisualObservation(
                hasLineOfSight ,
                visualState ,
                hasLineOfSight ? visiblePosition : Vector3.zero ,
                _detectionRatio);

            return CurrentObservation;
        }

        public void ResetPerception()
        {
            _detectionRatio = 0f;

            CurrentObservation = new ChaseAIVisualObservation(
                false ,
                CHASE_AI_VISUAL_STATE.NONE ,
                Vector3.zero ,
                0f);
        }

        private bool TryGetVisiblePosition(
            out Vector3 visiblePosition)
        {
            visiblePosition = Vector3.zero;

            if ( _config == null ||
                _eyeTransform == null ||
                _targetTransform == null )
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
                return true;
            }

            Vector3 normalizedDirection =
                directionToTarget / distanceToTarget;

            Vector3 localDirection =
                _eyeTransform.InverseTransformDirection(
                    normalizedDirection);

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
            return true;
        }

        private void UpdateDetectionRatio(bool hasLineOfSight , float deltaTime)
        {
            if ( hasLineOfSight )
            {
                float increaseAmount = deltaTime / _config.VisualConfirmTime;

                _detectionRatio = Mathf.Clamp01(_detectionRatio + increaseAmount);

                return;
            }

            float decreaseAmount = deltaTime / _config.VisualLoseTime;

            _detectionRatio = Mathf.Clamp01(_detectionRatio - decreaseAmount);
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

        private void OnDrawGizmosSelected()
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

            bool hasLineOfSight = TryGetVisiblePosition(out Vector3 visiblePosition);

            Gizmos.color = hasLineOfSight ? Color.green : Color.red;

            Gizmos.DrawLine(_eyeTransform.position , hasLineOfSight ? visiblePosition : _targetTransform.position);
        }

        private void DrawSightRay(float verticalAngle , float horizontalAngle)
        {
            Quaternion directionRotation =
                _eyeTransform.rotation *
                Quaternion.Euler(verticalAngle, horizontalAngle, 0f);

            Vector3 direction = directionRotation * Vector3.forward;

            Gizmos.DrawRay(_eyeTransform.position ,direction * _config.SightRange);
        }
    }
}
