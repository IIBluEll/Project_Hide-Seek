using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    public enum CHASE_AI_MOVE_REQUEST_RESULT
    {
        ACCEPTED,
        CONFIG_NOT_ASSIGNED,
        AGENT_NOT_ON_NAVMESH,
        NAVMESH_POSITION_NOT_FOUND,
        PATH_NOT_COMPLETE
    }

    public enum CHASE_AI_MOVE_STATUS
    {
        IDLE,
        MOVING,
        ARRIVED,
        PATH_FAILED,
        STUCK
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ChaseAIMovement : MonoBehaviour
    {
        [SerializeField] private ChaseAIConfig _config;

        private NavMeshAgent _agent;
        private NavMeshPath _calculatedPath;

        private Vector3 _currentDestination;
        private float _stuckTimer;
        private bool _hasDestination;

        public Vector3 CurrentDestination => _currentDestination;
        public Vector3 Position => transform.position;

        public bool HasDestination => _hasDestination;
        public int AreaMask => _agent != null ? _agent.areaMask : NavMesh.AllAreas;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _calculatedPath = new NavMeshPath();

            ApplyConfig();
        }

        public CHASE_AI_MOVE_REQUEST_RESULT TrySetDestination(
            Vector3 targetPosition ,
            out Vector3 correctedDestination)
        {
            correctedDestination = Vector3.zero;

            if ( _config == null )
            {
                return CHASE_AI_MOVE_REQUEST_RESULT.CONFIG_NOT_ASSIGNED;
            }

            if ( !_agent.isActiveAndEnabled || !_agent.isOnNavMesh )
            {
                return CHASE_AI_MOVE_REQUEST_RESULT.AGENT_NOT_ON_NAVMESH;
            }

            bool hasNavMeshPosition = NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit navMeshHit,
                _config.SampleRadius,
                _agent.areaMask);

            if ( !hasNavMeshPosition )
            {
                return CHASE_AI_MOVE_REQUEST_RESULT.NAVMESH_POSITION_NOT_FOUND;
            }

            _calculatedPath.ClearCorners();

            bool hasPath = _agent.CalculatePath(
                navMeshHit.position,
                _calculatedPath);

            if ( !hasPath ||
                _calculatedPath.status != NavMeshPathStatus.PathComplete )
            {
                return CHASE_AI_MOVE_REQUEST_RESULT.PATH_NOT_COMPLETE;
            }

            _agent.isStopped = false;

            if ( !_agent.SetPath(_calculatedPath) )
            {
                return CHASE_AI_MOVE_REQUEST_RESULT.PATH_NOT_COMPLETE;
            }

            correctedDestination = navMeshHit.position;
            _currentDestination = correctedDestination;
            _hasDestination = true;
            _stuckTimer = 0f;

            return CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED;
        }

        public bool TryWarp(Vector3 targetPosition , out Vector3 correctedPosition)
        {
            correctedPosition = Vector3.zero;

            if ( _config == null || _agent == null || !_agent.isActiveAndEnabled )
            {
                return false;
            }

            bool hasNavMeshPosition = NavMesh.SamplePosition(
                targetPosition ,
                out NavMeshHit navMeshHit ,
                _config.SampleRadius ,
                _agent.areaMask);

            if ( !hasNavMeshPosition || !_agent.Warp(navMeshHit.position) )
            {
                return false;
            }

            correctedPosition = navMeshHit.position;
            _currentDestination = correctedPosition;
            _hasDestination = false;
            _stuckTimer = 0f;

            if ( _agent.isOnNavMesh )
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            return true;
        }

        public void SetSpeed(float speed)
        {
            if ( _agent == null )
            {
                return;
            }

            _agent.speed = Mathf.Max(0f , speed);
        }

        public CHASE_AI_MOVE_STATUS UpdateMovement(float deltaTime)
        {
            if ( !_hasDestination )
            {
                return CHASE_AI_MOVE_STATUS.IDLE;
            }

            if ( !_agent.isActiveAndEnabled || !_agent.isOnNavMesh )
            {
                return FailMovement(CHASE_AI_MOVE_STATUS.PATH_FAILED);
            }

            if ( _agent.pathPending )
            {
                return CHASE_AI_MOVE_STATUS.MOVING;
            }

            if ( _agent.pathStatus != NavMeshPathStatus.PathComplete )
            {
                return FailMovement(CHASE_AI_MOVE_STATUS.PATH_FAILED);
            }

            if ( HasArrived() )
            {
                Stop();

                return CHASE_AI_MOVE_STATUS.ARRIVED;
            }

            if ( IsStuck(deltaTime) )
            {
                return FailMovement(CHASE_AI_MOVE_STATUS.STUCK);
            }

            return CHASE_AI_MOVE_STATUS.MOVING;
        }

        public void Stop()
        {
            _hasDestination = false;
            _stuckTimer = 0f;

            if ( !_agent.isActiveAndEnabled || !_agent.isOnNavMesh )
            {
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        private void ApplyConfig()
        {
            if ( _config == null )
            {
                Debug.LogError(
                    "[ChaseAIMovement] ChaseAIConfig가 할당되지 않았습니다." ,
                    this);

                return;
            }

            _agent.speed = _config.WalkSpeed;
            _agent.acceleration = _config.Acceleration;
            _agent.angularSpeed = _config.AngularSpeed;
            _agent.stoppingDistance = _config.StoppingDistance;
        }

        private bool HasArrived()
        {
            float arrivalDistance =
                _agent.stoppingDistance + _config.ArrivalTolerance;

            bool isWithinDistance =
                _agent.remainingDistance <= arrivalDistance;

            bool isAlmostStopped =
                _agent.velocity.sqrMagnitude <=
                _config.ArrivalVelocityThreshold *
                _config.ArrivalVelocityThreshold;

            return isWithinDistance && isAlmostStopped;
        }

        private bool IsStuck(float deltaTime)
        {
            float arrivalDistance =
                _agent.stoppingDistance + _config.ArrivalTolerance;

            if ( _agent.remainingDistance <= arrivalDistance )
            {
                _stuckTimer = 0f;
                return false;
            }

            bool isMovingSlowly =
                _agent.velocity.sqrMagnitude <=
                _config.StuckVelocityThreshold *
                _config.StuckVelocityThreshold;

            if ( !isMovingSlowly )
            {
                _stuckTimer = 0f;
                return false;
            }

            _stuckTimer += deltaTime;

            return _stuckTimer >= _config.StuckTimeLimit;
        }

        private CHASE_AI_MOVE_STATUS FailMovement(
            CHASE_AI_MOVE_STATUS failureStatus)
        {
            Stop();

            return failureStatus;
        }

        private void OnDrawGizmos()
        {
            if ( !_hasDestination )
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position , _currentDestination);
            Gizmos.DrawWireSphere(_currentDestination , 0.3f);

            if ( _agent == null ||
                !_agent.isActiveAndEnabled ||
                !_agent.isOnNavMesh )
            {
                return;
            }

            Vector3[] pathCorners = _agent.path.corners;

            Gizmos.color = Color.yellow;

            for ( int index = 0; index < pathCorners.Length - 1; index++ )
            {
                Gizmos.DrawLine(
                    pathCorners[ index ] ,
                    pathCorners[ index + 1 ]);
            }
        }
    }
}
