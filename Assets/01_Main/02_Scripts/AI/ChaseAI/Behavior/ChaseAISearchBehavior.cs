using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAISearchBehavior
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;
        private readonly ChaseAIMovement CHASE_AI_MOVEMENT;
        private readonly ChaseAISearch CHASE_AI_SEARCH;
        private readonly ChaseAIAnger CHASE_AI_ANGER;

        private Vector3 _searchCenterPosition;
        private float _currentSearchRadius;
        private float _currentSearchDuration;
        private int _currentSearchPointCount;
        private Vector3 _currentSearchDirection;
        private float _searchWaitDurationPerPoint;
        private float _waitTimer;
        private bool _isMovingToSearchCenter;
        private bool _isWaiting;

        public Vector3 SearchCenterPosition => _searchCenterPosition;
        public float CurrentSearchRadius => _currentSearchRadius;
        public string LastResultReason { get; private set; } = string.Empty;

        public ChaseAISearchBehavior(
            ChaseAIConfig chaseAIConfig ,
            ChaseAIMovement chaseAIMovement ,
            ChaseAISearch chaseAISearch ,
            ChaseAIAnger chaseAIAnger)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));
            CHASE_AI_MOVEMENT = chaseAIMovement != null ? chaseAIMovement : throw new ArgumentNullException(nameof(chaseAIMovement));
            CHASE_AI_SEARCH = chaseAISearch != null ? chaseAISearch : throw new ArgumentNullException(nameof(chaseAISearch));
            CHASE_AI_ANGER = chaseAIAnger != null ? chaseAIAnger : throw new ArgumentNullException(nameof(chaseAIAnger));
        }

        public CHASE_AI_BEHAVIOR_STATUS Begin(ChaseAISearchRequest searchRequest)
        {
            Stop();

            CHASE_AI_MOVEMENT.SetSpeed(CHASE_AI_CONFIG.WalkSpeed);

            _searchCenterPosition = searchRequest.CenterPosition;
            _currentSearchDirection = NormalizeHorizontalDirection(searchRequest.PreferredDirection);
            _currentSearchRadius = Mathf.Max(
                0f ,
                searchRequest.SearchRadius * CHASE_AI_ANGER.GetSearchRadiusMultiplier(searchRequest.AngerInfluence));
            _currentSearchPointCount = CHASE_AI_ANGER.GetSearchPointCount(searchRequest.AngerInfluence);

            float pointCountRatio = _currentSearchPointCount / (float)CHASE_AI_CONFIG.MinimumAngerSearchPointCount;

            _currentSearchDuration = Mathf.Max(0f , searchRequest.SearchDuration * pointCountRatio);
            _isMovingToSearchCenter = searchRequest.ShouldMoveToCenter;

            if ( searchRequest.ShouldMoveToCenter && RequestDestination(searchRequest.CenterPosition , searchRequest.Context) )
            {
                return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
            }

            return BeginAreaSearch();
        }

        public CHASE_AI_BEHAVIOR_STATUS Tick(float deltaTime)
        {
            if ( _isWaiting )
            {
                if ( UpdateWaiting(deltaTime) )
                {
                    return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
                }

                return AdvanceSearchPoint();
            }

            CHASE_AI_MOVE_STATUS moveStatus = CHASE_AI_MOVEMENT.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                    return _isMovingToSearchCenter
                        ? BeginAreaSearch()
                        : RequestCurrentSearchPointOrComplete();

                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    if ( _isMovingToSearchCenter )
                    {
                        return BeginAreaSearch();
                    }

                    StartWaiting(_searchWaitDurationPerPoint);
                    return CHASE_AI_BEHAVIOR_STATUS.RUNNING;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    Debug.LogWarning($"[ChaseAIStateMachine] 수색 이동 실패: {moveStatus}");

                    return _isMovingToSearchCenter
                        ? BeginAreaSearch()
                        : AdvanceSearchPoint();

                default:
                    return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
            }
        }

        public void Stop()
        {
            CHASE_AI_SEARCH.Clear();
            _searchCenterPosition = Vector3.zero;
            _currentSearchRadius = 0f;
            _currentSearchDuration = 0f;
            _currentSearchPointCount = 0;
            _currentSearchDirection = Vector3.zero;
            _searchWaitDurationPerPoint = 0f;
            _waitTimer = 0f;
            _isMovingToSearchCenter = false;
            _isWaiting = false;
            LastResultReason = string.Empty;
        }

        private CHASE_AI_BEHAVIOR_STATUS BeginAreaSearch()
        {
            _isMovingToSearchCenter = false;

            bool hasSearchPoints = CHASE_AI_SEARCH.BuildSearchPoints(
                CHASE_AI_MOVEMENT.Position ,
                _searchCenterPosition ,
                _currentSearchRadius ,
                _currentSearchPointCount ,
                _currentSearchDirection ,
                CHASE_AI_CONFIG.LastSeenPredictionDistance ,
                CHASE_AI_CONFIG.DirectionalSearchPointRatio ,
                CHASE_AI_CONFIG.DirectionalSearchAngle ,
                CHASE_AI_CONFIG.MinimumSearchPointDistance ,
                CHASE_AI_CONFIG.SampleRadius ,
                CHASE_AI_MOVEMENT.AreaMask ,
                CHASE_AI_CONFIG.SearchPointGenerationAttemptCountPerPoint);

            if ( !hasSearchPoints )
            {
                LastResultReason = "Search point generation failed";

                return CHASE_AI_BEHAVIOR_STATUS.FAILED;
            }

            _searchWaitDurationPerPoint = _currentSearchDuration / CHASE_AI_SEARCH.PointCount;

            Debug.Log(
                $"[ChaseAIStateMachine] 수색 지점 생성 완료: " +
                $"Count={CHASE_AI_SEARCH.PointCount}, " +
                $"Center={_searchCenterPosition}, " +
                $"Direction={_currentSearchDirection}, " +
                $"Radius={_currentSearchRadius:F1}, " +
                $"WaitPerPoint={_searchWaitDurationPerPoint:F1}");

            return RequestCurrentSearchPointOrComplete();
        }

        private CHASE_AI_BEHAVIOR_STATUS RequestCurrentSearchPointOrComplete()
        {
            while ( CHASE_AI_SEARCH.TryGetCurrentPoint(out Vector3 searchPoint) )
            {
                string context = $"Search point {CHASE_AI_SEARCH.CurrentPointIndex + 1}/{CHASE_AI_SEARCH.PointCount}";

                if ( RequestDestination(searchPoint , context) )
                {
                    return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
                }

                CHASE_AI_SEARCH.AdvanceToNextPoint();
            }

            LastResultReason = "All search points completed";

            return CHASE_AI_BEHAVIOR_STATUS.COMPLETED;
        }

        private CHASE_AI_BEHAVIOR_STATUS AdvanceSearchPoint()
        {
            CHASE_AI_SEARCH.AdvanceToNextPoint();

            return RequestCurrentSearchPointOrComplete();
        }

        private bool RequestDestination(Vector3 position , string context)
        {
            CHASE_AI_MOVE_REQUEST_RESULT result = CHASE_AI_MOVEMENT.TrySetDestination(position , out Vector3 correctedDestination);

            if ( result == CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                return true;
            }

            Debug.LogWarning($"[ChaseAIStateMachine] {context} 목적지 요청 실패: {result}");

            return false;
        }

        private void StartWaiting(float duration)
        {
            _waitTimer = duration;
            _isWaiting = true;
        }

        private bool UpdateWaiting(float deltaTime)
        {
            _waitTimer -= deltaTime;

            if ( _waitTimer > 0f )
            {
                return true;
            }

            _isWaiting = false;

            return false;
        }

        private static Vector3 NormalizeHorizontalDirection(Vector3 direction)
        {
            direction.y = 0f;

            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
        }
    }
}
