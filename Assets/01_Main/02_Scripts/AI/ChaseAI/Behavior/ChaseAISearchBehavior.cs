using System;
using UnityEngine;

namespace HideSeek.AI
{
    public enum CHASE_AI_SEARCH_ACTION
    {
        NONE,
        CHECK_DIRECTION,
        OBSERVE_AREA,
        INSPECT_HIDING_SPOT
    }

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
        private float _searchActionDuration;
        private float _searchActionRemainingTime;
        private Vector3 _searchActionBaseDirection;
        private Vector3 _searchActionTargetDirection;
        private int _searchZoneId = ChaseAISearchRequest.NO_ZONE_ID;
        private bool _isPrepared;
        private bool _isPerformingSearchAction;
        private bool _canInspectHidingSpot;
        private float _hidingSpotInspectionChance;
        private bool _shouldRestrictToZone;

        public Vector3 SearchCenterPosition => _searchCenterPosition;
        public float CurrentSearchRadius => _currentSearchRadius;
        public bool IsPrepared => _isPrepared;
        public CHASE_AI_EVIDENCE_TYPE ActiveEvidenceType { get; private set; } = CHASE_AI_EVIDENCE_TYPE.NONE;
        public CHASE_AI_SEARCH_ACTION CurrentSearchAction { get; private set; } = CHASE_AI_SEARCH_ACTION.NONE;
        public float SearchActionRemainingTime => Mathf.Max(0f , _searchActionRemainingTime);
        public float SearchActionProgress => _searchActionDuration > Mathf.Epsilon
            ? Mathf.Clamp01(1f - SearchActionRemainingTime / _searchActionDuration)
            : 0f;
        public string ActiveSearchContext { get; private set; } = string.Empty;
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
            if ( !Prepare(searchRequest) )
            {
                return CHASE_AI_BEHAVIOR_STATUS.FAILED;
            }

            return BeginPrepared();
        }

        public bool Prepare(ChaseAISearchRequest searchRequest)
        {
            Stop();

            _searchCenterPosition = searchRequest.CenterPosition;
            _currentSearchDirection = NormalizeHorizontalDirection(searchRequest.PreferredDirection);
            ActiveSearchContext = searchRequest.Context;
            ActiveEvidenceType = searchRequest.EvidenceType;
            _currentSearchRadius = Mathf.Max(
                0f ,
                searchRequest.SearchRadius * CHASE_AI_ANGER.GetSearchRadiusMultiplier(searchRequest.AngerInfluence));
            _currentSearchPointCount = CHASE_AI_ANGER.GetSearchPointCount(searchRequest.AngerInfluence);

            float pointCountRatio = _currentSearchPointCount / (float)CHASE_AI_CONFIG.MinimumAngerSearchPointCount;

            _currentSearchDuration = Mathf.Max(0f , searchRequest.SearchDuration * pointCountRatio);
            _searchZoneId = searchRequest.SearchZoneId;
            _canInspectHidingSpot = searchRequest.CanInspectHidingSpot;
            _hidingSpotInspectionChance = searchRequest.HidingSpotInspectionChance;
            _shouldRestrictToZone = searchRequest.ShouldRestrictToZone;

            if ( !BuildSearchPoints() )
            {
                LastResultReason = "Search point generation failed";

                return false;
            }

            _isPrepared = true;

            return true;
        }

        public CHASE_AI_BEHAVIOR_STATUS BeginPrepared()
        {
            if ( !_isPrepared )
            {
                LastResultReason = "Search behavior was not prepared";

                return CHASE_AI_BEHAVIOR_STATUS.FAILED;
            }

            _isPrepared = false;
            CHASE_AI_MOVEMENT.SetSpeed(CHASE_AI_CONFIG.WalkSpeed);

            Debug.Log(
                $"[ChaseAIStateMachine] 준비된 수색 실행 시작: " +
                $"Center={_searchCenterPosition}, " +
                $"Radius={_currentSearchRadius:F1}, " +
                $"FirstPointIndex={CHASE_AI_SEARCH.CurrentPointIndex + 1}/{CHASE_AI_SEARCH.PointCount}");

            return RequestCurrentSearchPointOrComplete();
        }

        public CHASE_AI_BEHAVIOR_STATUS Tick(float deltaTime)
        {
            if ( _isPerformingSearchAction )
            {
                if ( UpdateSearchAction(deltaTime) )
                {
                    return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
                }

                return CompleteCurrentSearchAction();
            }

            CHASE_AI_MOVE_STATUS moveStatus = CHASE_AI_MOVEMENT.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                    return RequestCurrentSearchPointOrComplete();

                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    return BeginCurrentSearchAction();

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    Debug.LogWarning($"[ChaseAIStateMachine] 수색 이동 실패: {moveStatus}");

                    return AdvanceSearchPoint();

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
            _searchZoneId = ChaseAISearchRequest.NO_ZONE_ID;
            _isPrepared = false;
            _canInspectHidingSpot = false;
            _hidingSpotInspectionChance = 0f;
            _shouldRestrictToZone = false;
            StopSearchAction();
            ActiveSearchContext = string.Empty;
            ActiveEvidenceType = CHASE_AI_EVIDENCE_TYPE.NONE;
            LastResultReason = string.Empty;
        }

        private bool BuildSearchPoints()
        {
            bool hasSearchPoints = CHASE_AI_SEARCH.BuildSearchPoints(
                CHASE_AI_MOVEMENT.Position ,
                _searchCenterPosition ,
                _currentSearchRadius ,
                _currentSearchPointCount ,
                _currentSearchDirection ,
                CHASE_AI_CONFIG.LastSeenPredictionDistance ,
                CHASE_AI_CONFIG.DirectionalSearchPointRatio ,
                CHASE_AI_CONFIG.ZoneCoverageSearchPointRatio ,
                CHASE_AI_CONFIG.DirectionalSearchAngle ,
                CHASE_AI_CONFIG.MinimumSearchPointDistance ,
                CHASE_AI_CONFIG.HidingSpotEvidenceDistance ,
                CHASE_AI_CONFIG.SampleRadius ,
                CHASE_AI_MOVEMENT.AreaMask ,
                CHASE_AI_CONFIG.SearchPointGenerationAttemptCountPerPoint ,
                _searchZoneId ,
                _canInspectHidingSpot ,
                _hidingSpotInspectionChance ,
                _shouldRestrictToZone);

            if ( !hasSearchPoints )
            {
                return false;
            }

            _searchWaitDurationPerPoint = _currentSearchDuration / CHASE_AI_SEARCH.PointCount;

            Debug.Log(
                $"[ChaseAIStateMachine] 수색 계획 준비 완료: " +
                $"Count={CHASE_AI_SEARCH.PointCount}, " +
                $"Center={_searchCenterPosition}, " +
                $"Direction={_currentSearchDirection}, " +
                $"Radius={_currentSearchRadius:F1}, " +
                $"Zone={CHASE_AI_SEARCH.ActiveSearchZoneName}, " +
                $"ZoneRestricted={CHASE_AI_SEARCH.IsZoneRestricted}, " +
                $"CoveragePoints={CHASE_AI_SEARCH.ZoneCoveragePointCount}, " +
                $"HidingSpotPoints={CHASE_AI_SEARCH.HidingSpotPointCount}, " +
                $"WaitPerPoint={_searchWaitDurationPerPoint:F1}");

            return true;
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
            CHASE_AI_MOVE_REQUEST_RESULT result = CHASE_AI_MOVEMENT.TrySetDestination(position);

            if ( result == CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                return true;
            }

            Debug.LogWarning($"[ChaseAIStateMachine] {context} 목적지 요청 실패: {result}");

            return false;
        }

        private CHASE_AI_BEHAVIOR_STATUS BeginCurrentSearchAction()
        {
            CHASE_AI_SEARCH_POINT_SOURCE searchPointSource = CHASE_AI_SEARCH_POINT_SOURCE.RANDOM;

            if ( !CHASE_AI_SEARCH.TryGetSearchPointSource(
                    CHASE_AI_SEARCH.CurrentPointIndex ,
                    out searchPointSource) )
            {
                searchPointSource = CHASE_AI_SEARCH_POINT_SOURCE.RANDOM;
            }

            CurrentSearchAction = ResolveSearchAction(searchPointSource);
            _searchActionDuration = _searchWaitDurationPerPoint * GetSearchActionTimeMultiplier(CurrentSearchAction);
            _searchActionRemainingTime = _searchActionDuration;
            _isPerformingSearchAction = _searchActionDuration > 0f;
            PrepareSearchActionFacing();

            Debug.Log(
                $"[ChaseAISearchBehavior] 수색 행동 시작: " +
                $"Action={CurrentSearchAction}, " +
                $"Source={searchPointSource}, " +
                $"Point={CHASE_AI_SEARCH.CurrentPointIndex + 1}/{CHASE_AI_SEARCH.PointCount}, " +
                $"Duration={_searchActionDuration:F1}, " +
                $"LookDirection={_searchActionTargetDirection}");

            if ( !_isPerformingSearchAction )
            {
                return CompleteCurrentSearchAction();
            }

            return CHASE_AI_BEHAVIOR_STATUS.RUNNING;
        }

        private bool UpdateSearchAction(float deltaTime)
        {
            float validDeltaTime = Mathf.Max(0f , deltaTime);

            _searchActionRemainingTime -= validDeltaTime;
            UpdateSearchActionFacing(validDeltaTime);

            return _searchActionRemainingTime > 0f;
        }

        private void PrepareSearchActionFacing()
        {
            _searchActionBaseDirection = CHASE_AI_MOVEMENT.Forward;

            if ( _searchActionBaseDirection == Vector3.zero )
            {
                _searchActionBaseDirection = Vector3.forward;
            }

            _searchActionTargetDirection = _searchActionBaseDirection;

            switch ( CurrentSearchAction )
            {
                case CHASE_AI_SEARCH_ACTION.CHECK_DIRECTION:
                    if ( _currentSearchDirection != Vector3.zero )
                    {
                        _searchActionTargetDirection = _currentSearchDirection;
                    }
                    break;

                case CHASE_AI_SEARCH_ACTION.INSPECT_HIDING_SPOT:
                    if ( CHASE_AI_SEARCH.TryGetCurrentHidingSpot(out AIHidingSpot hidingSpot) )
                    {
                        Vector3 hidingSpotDirection = NormalizeHorizontalDirection(
                            hidingSpot.transform.position - CHASE_AI_MOVEMENT.Position);

                        if ( hidingSpotDirection != Vector3.zero )
                        {
                            _searchActionTargetDirection = hidingSpotDirection;
                        }
                    }
                    break;
            }
        }

        private void UpdateSearchActionFacing(float deltaTime)
        {
            Vector3 targetDirection = CurrentSearchAction == CHASE_AI_SEARCH_ACTION.OBSERVE_AREA
                ? GetAreaSearchSweepDirection()
                : _searchActionTargetDirection;

            CHASE_AI_MOVEMENT.RotateTowardsDirection(
                targetDirection ,
                CHASE_AI_CONFIG.SearchRotationSpeed ,
                deltaTime);
        }

        private Vector3 GetAreaSearchSweepDirection()
        {
            float halfSweepAngle = CHASE_AI_CONFIG.AreaSearchSweepAngle * 0.5f;
            float progress = SearchActionProgress;
            float currentAngle;

            if ( progress < 0.25f )
            {
                currentAngle = Mathf.Lerp(0f , -halfSweepAngle , progress / 0.25f);
            }
            else if ( progress < 0.75f )
            {
                currentAngle = Mathf.Lerp(
                    -halfSweepAngle ,
                    halfSweepAngle ,
                    (progress - 0.25f) / 0.5f);
            }
            else
            {
                currentAngle = Mathf.Lerp(
                    halfSweepAngle ,
                    0f ,
                    (progress - 0.75f) / 0.25f);
            }

            return Quaternion.AngleAxis(currentAngle , Vector3.up) * _searchActionBaseDirection;
        }

        private CHASE_AI_BEHAVIOR_STATUS CompleteCurrentSearchAction()
        {
            bool wasPlayerFound = CurrentSearchAction == CHASE_AI_SEARCH_ACTION.INSPECT_HIDING_SPOT &&
                TryFindPlayerInCurrentHidingSpot();

            StopSearchAction();

            if ( wasPlayerFound )
            {
                LastResultReason = "Player found in hiding spot";

                return CHASE_AI_BEHAVIOR_STATUS.TARGET_FOUND;
            }

            return AdvanceSearchPoint();
        }

        private bool TryFindPlayerInCurrentHidingSpot()
        {
            if ( !CHASE_AI_SEARCH.TryGetCurrentHidingSpot(out AIHidingSpot hidingSpot) )
            {
                Debug.LogWarning("[ChaseAISearchBehavior] 은신처 조사 대상 정보가 없습니다.");

                return false;
            }

            bool containsPlayer = hidingSpot.ContainsPlayer();

            Debug.Log(
                $"[ChaseAISearchBehavior] 은신처 조사 완료: " +
                $"Spot={hidingSpot.name}, " +
                $"Type={hidingSpot.HidingSpotType}, " +
                $"PlayerFound={containsPlayer}");

            return containsPlayer;
        }

        private void StopSearchAction()
        {
            CurrentSearchAction = CHASE_AI_SEARCH_ACTION.NONE;
            _searchActionDuration = 0f;
            _searchActionRemainingTime = 0f;
            _searchActionBaseDirection = Vector3.zero;
            _searchActionTargetDirection = Vector3.zero;
            _isPerformingSearchAction = false;
        }

        private float GetSearchActionTimeMultiplier(CHASE_AI_SEARCH_ACTION searchAction)
        {
            return searchAction switch
            {
                CHASE_AI_SEARCH_ACTION.CHECK_DIRECTION => CHASE_AI_CONFIG.DirectionalSearchActionTimeMultiplier,
                CHASE_AI_SEARCH_ACTION.INSPECT_HIDING_SPOT => CHASE_AI_CONFIG.HidingSpotSearchActionTimeMultiplier,
                CHASE_AI_SEARCH_ACTION.OBSERVE_AREA => CHASE_AI_CONFIG.AreaSearchActionTimeMultiplier,
                _ => 0f
            };
        }

        private static CHASE_AI_SEARCH_ACTION ResolveSearchAction(
            CHASE_AI_SEARCH_POINT_SOURCE searchPointSource)
        {
            return searchPointSource switch
            {
                CHASE_AI_SEARCH_POINT_SOURCE.PREDICTED_DIRECTION => CHASE_AI_SEARCH_ACTION.CHECK_DIRECTION,
                CHASE_AI_SEARCH_POINT_SOURCE.DIRECTIONAL => CHASE_AI_SEARCH_ACTION.CHECK_DIRECTION,
                CHASE_AI_SEARCH_POINT_SOURCE.HIDING_SPOT => CHASE_AI_SEARCH_ACTION.INSPECT_HIDING_SPOT,
                CHASE_AI_SEARCH_POINT_SOURCE.ZONE_COVERAGE => CHASE_AI_SEARCH_ACTION.OBSERVE_AREA,
                CHASE_AI_SEARCH_POINT_SOURCE.RANDOM => CHASE_AI_SEARCH_ACTION.OBSERVE_AREA,
                _ => CHASE_AI_SEARCH_ACTION.OBSERVE_AREA
            };
        }

        private static Vector3 NormalizeHorizontalDirection(Vector3 direction)
        {
            direction.y = 0f;

            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
        }
    }
}
