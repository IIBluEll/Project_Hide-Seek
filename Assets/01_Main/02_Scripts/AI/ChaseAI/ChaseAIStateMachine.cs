using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public enum CHASE_AI_STATE
    {
        DORMANT,
        PATROL,
        INVESTIGATE,
        CHASE,
        SEARCH,
        RETREAT
    }

    public sealed class ChaseAIStateMachine
    {
        private const float AUDIO_REPLACEMENT_TOLERANCE = 0.05f;

        private readonly ChaseAIConfig _config;
        private readonly ChaseAIMovement _movement;
        private readonly ChaseAIMemory _memory;
        private readonly ChaseAISearch _search;
        private readonly ChaseAIAnger CHASE_AI_ANGER;
        private readonly IReadOnlyList<Transform> _patrolPoints;

        private MasterAIHint _activeDirectorHint;
        private bool _hasActiveDirectorInvestigation;

        private int _currentPatrolPointIndex;

        private float _stateTimer;
        private float _chaseRepathTimer;

        private bool _isWaiting;

        private Vector3 _audioSearchPosition;
        private float _audioSearchRadius;
        private float _audioSearchDuration;
        private float _currentAudioIntensity;

        private Vector3 _searchCenterPosition;
        private float _currentSearchRadius;
        private float _currentSearchDuration;
        private int _currentSearchPointCount;
        private float _searchWaitDurationPerPoint;
        private bool _isMovingToSearchCenter;

        private bool _hasActiveAudioInvestigation;

        private Vector3 _retreatPosition;
        private bool _isRetreatPending;
        private bool _hasRetreatFailed;

        public Vector3 SearchCenterPosition => _searchCenterPosition;
        public float CurrentSearchRadius => _currentSearchRadius;

        public bool IsRetreatPending => _isRetreatPending;

        public CHASE_AI_STATE CurrentState
        {
            get;
            private set;
        } = CHASE_AI_STATE.DORMANT;

        public ChaseAIStateMachine(ChaseAIConfig config , ChaseAIMovement movement , ChaseAIMemory memory , ChaseAISearch search , ChaseAIAnger chaseAIAnger , IReadOnlyList<Transform> patrolPoints)
        {
            _config = config;
            _movement = movement;
            _memory = memory;
            _search = search;
            CHASE_AI_ANGER = chaseAIAnger;
            _patrolPoints = patrolPoints;
        }

        public void Initialize()
        {
            _movement.Stop();
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            CurrentState = CHASE_AI_STATE.DORMANT;
            _retreatPosition = Vector3.zero;
            _isWaiting = false;
            _isRetreatPending = false;
            _hasRetreatFailed = false;
            _stateTimer = 0f;

            Debug.Log("[ChaseAIStateMachine] DORMANT 상태로 초기화되었습니다.");
        }

        public void RefreshAngerEffects()
        {
            if ( CurrentState == CHASE_AI_STATE.CHASE )
            {
                _movement.SetSpeed(_config.ChaseSpeed * CHASE_AI_ANGER.ChaseSpeedMultiplier);
            }
        }

        public bool RequestActivation()
        {
            if ( CurrentState != CHASE_AI_STATE.DORMANT )
            {
                return false;
            }

            _retreatPosition = Vector3.zero;
            _isRetreatPending = false;
            _hasRetreatFailed = false;

            ChangeState(CHASE_AI_STATE.PATROL , "Director activation requested");

            return CurrentState == CHASE_AI_STATE.PATROL;
        }

        public bool RequestRetreat(Vector3 retreatPosition)
        {
            if ( CurrentState == CHASE_AI_STATE.DORMANT )
            {
                return false;
            }

            _retreatPosition = retreatPosition;

            if ( _isRetreatPending || CurrentState == CHASE_AI_STATE.RETREAT )
            {
                return true;
            }

            _isRetreatPending = true;

            if ( CurrentState == CHASE_AI_STATE.CHASE )
            {
                Debug.Log("[ChaseAIStateMachine] 추격 중 이탈 요청 예약: 직접 시야와 수색을 우선합니다.");

                return true;
            }

            if ( CurrentState == CHASE_AI_STATE.SEARCH )
            {
                Debug.Log("[ChaseAIStateMachine] 수색 중 이탈 요청 예약: 현재 수색 완료 후 처리합니다.");

                return true;
            }

            ChangeState(CHASE_AI_STATE.RETREAT , $"Director retreat requested from {CurrentState}");

            return CurrentState == CHASE_AI_STATE.RETREAT;
        }

        public bool ConsumeRetreatFailure()
        {
            if ( !_hasRetreatFailed )
            {
                return false;
            }

            _hasRetreatFailed = false;

            return true;
        }

        public void Tick(float deltaTime , ChaseAIVisualObservation visualObservation)
        {
            if ( CurrentState == CHASE_AI_STATE.DORMANT )
            {
                return;
            }

            if ( visualObservation.State == CHASE_AI_VISUAL_STATE.CONFIRMED && CurrentState != CHASE_AI_STATE.CHASE )
            {
                ChangeState(CHASE_AI_STATE.CHASE , "Player visually confirmed");
            }

            switch ( CurrentState )
            {
                case CHASE_AI_STATE.PATROL:
                    UpdatePatrol(deltaTime);
                    break;

                case CHASE_AI_STATE.INVESTIGATE:
                    UpdateInvestigate(deltaTime);
                    break;

                case CHASE_AI_STATE.CHASE:
                    UpdateChase(
                        deltaTime ,
                        visualObservation);
                    break;

                case CHASE_AI_STATE.SEARCH:
                    UpdateSearch(deltaTime);
                    break;

                case CHASE_AI_STATE.RETREAT:
                    UpdateRetreat(deltaTime);
                    break;
            }
        }

        public bool TryReceiveDirectorHint(MasterAIHint hint , float currentTime)
        {
            if ( CurrentState != CHASE_AI_STATE.PATROL )
            {
                return false;
            }

            if ( !hint.IsValid(currentTime) )
            {
                return false;
            }

            if ( _hasActiveAudioInvestigation || _memory.HasValidVisualEvidence(currentTime) || _memory.HasValidAudioEvidence(currentTime) )
            {
                return false;
            }

            _activeDirectorHint = hint;
            _hasActiveDirectorInvestigation = true;

            ChangeState(CHASE_AI_STATE.INVESTIGATE , "Director hint accepted");

            return CurrentState == CHASE_AI_STATE.INVESTIGATE;
        }

        public bool TryReceiveAudioEvidence(ChaseAIAudioObservation observation)
        {
            if ( CurrentState == CHASE_AI_STATE.CHASE || CurrentState == CHASE_AI_STATE.DORMANT || CurrentState == CHASE_AI_STATE.RETREAT )
            {
                return false;
            }

            float newIntensity = Mathf.Clamp01(observation.PerceivedIntensity);

            bool canReplaceEvidence =!_hasActiveAudioInvestigation || newIntensity + AUDIO_REPLACEMENT_TOLERANCE >= _currentAudioIntensity;

            if ( !canReplaceEvidence )
            {
                Debug.Log(
                    $"[ChaseAIStateMachine] 약한 소음 무시: " +
                    $"Current={_currentAudioIntensity:F2}, " +
                    $"New={newIntensity:F2}");

                return false;
            }

            ClearDirectorInvestigation();

            _audioSearchPosition = observation.NoiseData.Position;

            _currentAudioIntensity = newIntensity;

            _audioSearchRadius = Mathf.Lerp(_config.MaxAudioSearchRadius , _config.MinAudioSearchRadius , newIntensity);

            _audioSearchDuration = Mathf.Lerp(_config.MinAudioSearchDuration , _config.MaxAudioSearchDuration , newIntensity);

            _hasActiveAudioInvestigation = true;

            Debug.Log(
                $"[ChaseAIStateMachine] 청각 증거 적용: " +
                $"Intensity={newIntensity:F2}, " +
                $"Radius={_audioSearchRadius:F1}, " +
                $"Duration={_audioSearchDuration:F1}");

            if ( CurrentState == CHASE_AI_STATE.INVESTIGATE )
            {
                // 기존 소음 위치에서 대기 중이었다면 즉시 중단
                _isWaiting = false;
                _stateTimer = 0f;

                bool wasDestinationAccepted = RequestAudioEvidenceDestination();

                if ( !wasDestinationAccepted )
                {
                    ChangeState(CHASE_AI_STATE.PATROL , "Updated audio destination invalid");
                }

                return wasDestinationAccepted;
            }

            ChangeState(CHASE_AI_STATE.INVESTIGATE , "New audio evidence accepted");

            // EnterInvestigate에서 목적지 설정에 실패하면 PATROL로 다시 변경되므로 false를 반환
            return CurrentState == CHASE_AI_STATE.INVESTIGATE;
        }

        public void Stop()
        {
            _movement.Stop();
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            CurrentState = CHASE_AI_STATE.DORMANT;
            _isWaiting = false;
            _retreatPosition = Vector3.zero;
            _isRetreatPending = false;
            _hasRetreatFailed = false;

            _stateTimer = 0f;
        }

        private void ClearDirectorInvestigation()
        {
            _activeDirectorHint = default;
            _hasActiveDirectorInvestigation = false;
        }

        private void UpdatePatrol(float deltaTime)
        {
            if ( UpdateWaiting(deltaTime) )
            {
                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                    RequestCurrentPatrolPoint();
                    break;

                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    AdvancePatrolPoint();
                    StartWaiting(_config.PatrolWaitTime);
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    Debug.LogWarning(
                        $"[ChaseAIStateMachine] " +
                        $"순찰 이동 실패: {moveStatus}");

                    AdvancePatrolPoint();
                    StartWaiting(_config.PatrolWaitTime);
                    break;
            }
        }

        private void UpdateInvestigate(float deltaTime)
        {
            if ( _isWaiting )
            {
                if ( !UpdateWaiting(deltaTime) )
                {
                    string completedEvidence = _hasActiveAudioInvestigation ? "Audio evidence" : "Director hint";

                    ChangeState(CHASE_AI_STATE.SEARCH , $"{completedEvidence} investigation completed");
                }

                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    StartWaiting(_config.InvestigateWaitTime);
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    ChangeState(CHASE_AI_STATE.PATROL , $"Investigate movement failed: {moveStatus}");
                    break;
            }
        }

        private void UpdateChase(float deltaTime , ChaseAIVisualObservation visualObservation)
        {
            if ( visualObservation.State != CHASE_AI_VISUAL_STATE.CONFIRMED || !visualObservation.HasLineOfSight )
            {
                ChangeState(CHASE_AI_STATE.SEARCH , "Line of sight lost");

                return;
            }

            _chaseRepathTimer -= deltaTime;

            float updateDistance = _config.ChaseDestinationUpdateDistance;

            float squaredUpdateDistance = updateDistance * updateDistance;

            bool hasMovedFromDestination = !_movement.HasDestination || ( visualObservation.VisiblePosition - _movement.CurrentDestination).sqrMagnitude >= squaredUpdateDistance;

            if ( _chaseRepathTimer <= 0f && hasMovedFromDestination )
            {
                RequestDestination(visualObservation.VisiblePosition , "Chase target");

                _chaseRepathTimer = _config.ChaseRepathInterval;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            if ( moveStatus == CHASE_AI_MOVE_STATUS.PATH_FAILED || moveStatus == CHASE_AI_MOVE_STATUS.STUCK )
            {
                ChangeState(CHASE_AI_STATE.SEARCH , $"Chase movement failed: {moveStatus}");
            }
        }

        private void UpdateSearch(float deltaTime)
        {
            if ( _isWaiting )
            {
                if ( !UpdateWaiting(deltaTime) )
                {
                    AdvanceSearchPoint();
                }

                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                    if ( _isMovingToSearchCenter )
                    {
                        BeginAreaSearch();
                    }
                    else
                    {
                        RequestCurrentSearchPointOrComplete();
                    }
                    break;

                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    if ( _isMovingToSearchCenter )
                    {
                        BeginAreaSearch();
                    }
                    else
                    {
                        StartWaiting(_searchWaitDurationPerPoint);
                    }
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    Debug.LogWarning($"[ChaseAIStateMachine] 수색 이동 실패: {moveStatus}");

                    if ( _isMovingToSearchCenter )
                    {
                        BeginAreaSearch();
                    }
                    else
                    {
                        AdvanceSearchPoint();
                    }
                    break;
            }
        }

        private void UpdateRetreat(float deltaTime)
        {
            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                    if ( !RequestDestination(_retreatPosition , "Retreat point") )
                    {
                        _isRetreatPending = false;
                        ChangeState(CHASE_AI_STATE.PATROL , "Retreat destination invalid");
                    }
                    break;

                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    CompleteRetreat();
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    FailRetreat($"Retreat movement failed: {moveStatus}");
                    break;
            }
        }

        private void FailRetreat(string reason)
        {
            _isRetreatPending = false;
            _hasRetreatFailed = true;

            Debug.LogWarning($"[ChaseAIStateMachine] 이탈 실패: {reason}");

            ChangeState(CHASE_AI_STATE.PATROL , reason);
        }

        private void ChangeState(CHASE_AI_STATE newState , string reason)
        {
            if ( CurrentState == newState )
            {
                return;
            }

            CHASE_AI_STATE previousState = CurrentState;

            _movement.Stop();
            _isWaiting = false;
            _stateTimer = 0f;

            CurrentState = newState;

            Debug.Log(
                $"[ChaseAIStateMachine] " +
                $"{previousState} → {newState}, " +
                $"Reason: {reason}");

            switch ( newState )
            {
                case CHASE_AI_STATE.DORMANT:
                    EnterDormant();
                    break;

                case CHASE_AI_STATE.PATROL:
                    EnterPatrol();
                    break;

                case CHASE_AI_STATE.INVESTIGATE:
                    EnterInvestigate();
                    break;

                case CHASE_AI_STATE.CHASE:
                    EnterChase();
                    break;

                case CHASE_AI_STATE.SEARCH:
                    EnterSearch();
                    break;

                case CHASE_AI_STATE.RETREAT:
                    EnterRetreat();
                    break;
            }
        }

        private void EnterDormant()
        {
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            _retreatPosition = Vector3.zero;
            _isRetreatPending = false;
            _hasRetreatFailed = false;
        }

        private void EnterRetreat()
        {
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            _movement.SetSpeed(_config.WalkSpeed);

            if ( !RequestDestination(_retreatPosition , "Retreat point") )
            {
                FailRetreat("Retreat destination invalid");
            }
        }

        private void EnterPatrol()
        {
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            _movement.SetSpeed(_config.WalkSpeed);
            RequestCurrentPatrolPoint();
        }

        private void EnterInvestigate()
        {
            ClearSearch();
            _movement.SetSpeed(_config.WalkSpeed);

            if ( _hasActiveAudioInvestigation )
            {
                if ( !RequestAudioEvidenceDestination() )
                {
                    ChangeState(CHASE_AI_STATE.PATROL , "Audio evidence destination invalid");
                }

                return;
            }

            if ( _hasActiveDirectorInvestigation )
            {
                if ( !RequestDirectorHintDestination() )
                {
                    ChangeState(CHASE_AI_STATE.PATROL , "Director hint destination invalid");
                }

                return;
            }

            ChangeState(CHASE_AI_STATE.PATROL , "No investigation evidence");
        }

        private bool RequestDirectorHintDestination()
        {
            if ( !_hasActiveDirectorInvestigation )
            {
                return false;
            }

            return RequestDestination(_activeDirectorHint.SearchAnchorPosition , "Director hint");
        }

        private void EnterChase()
        {
            ClearSearch();
            ClearAudioInvestigation();
            ClearDirectorInvestigation();

            _movement.SetSpeed(_config.ChaseSpeed * CHASE_AI_ANGER.ChaseSpeedMultiplier);
            _chaseRepathTimer = 0f;
        }

        private void EnterSearch()
        {
            ClearSearch();
            _movement.SetSpeed(_config.WalkSpeed);

            if ( _hasActiveAudioInvestigation )
            {
                Debug.Log($"[ChaseAIStateMachine] 청각 수색 시작: Position={_audioSearchPosition}, Radius={_audioSearchRadius:F1}, Duration={_audioSearchDuration:F1}");

                PrepareSearch(
                    _audioSearchPosition ,
                    _audioSearchRadius ,
                    _audioSearchDuration ,
                    false ,
                    1f ,
                    "Audio search center");

                return;
            }

            float currentTime = Time.time;

            if ( _memory.HasValidVisualEvidence(currentTime) )
            {
                PrepareSearch(
                    _memory.VisualEvidence.Position ,
                    _config.VisualSearchRadius ,
                    _config.SearchWaitTime ,
                    true ,
                    1f ,
                    "Last seen position");

                return;
            }

            if ( _memory.HasValidAudioEvidence(currentTime) )
            {
                float intensity = Mathf.Clamp01(_memory.AudioEvidence.Strength);
                float searchRadius = Mathf.Lerp(_config.MaxAudioSearchRadius , _config.MinAudioSearchRadius , intensity);
                float searchDuration = Mathf.Lerp(_config.MinAudioSearchDuration , _config.MaxAudioSearchDuration , intensity);

                PrepareSearch(
                    _memory.AudioEvidence.Position ,
                    searchRadius ,
                    searchDuration ,
                    true ,
                    1f ,
                    "Last heard position");

                return;
            }

            if ( _hasActiveDirectorInvestigation )
            {
                Debug.Log($"[ChaseAIStateMachine] Director Hint 수색 시작: Zone={_activeDirectorHint.TargetZoneId}, Position={_activeDirectorHint.SearchAnchorPosition}, Radius={_activeDirectorHint.SearchRadius:F1}, Urgency={_activeDirectorHint.Urgency:F2}");

                PrepareSearch(
                    _activeDirectorHint.SearchAnchorPosition ,
                    _activeDirectorHint.SearchRadius ,
                    _config.SearchWaitTime ,
                    false ,
                    _config.DirectorHintAngerInfluence ,
                    "Director hint search center");

                return;
            }

            CompleteSearch("No valid evidence for search");
        }

        private void CompleteSearch(string reason)
        {
            if ( _isRetreatPending )
            {
                ChangeState(CHASE_AI_STATE.RETREAT , $"{reason}, retreat pending");

                return;
            }

            ChangeState(CHASE_AI_STATE.PATROL , reason);
        }

        private void CompleteRetreat()
        {
            ChangeState(CHASE_AI_STATE.DORMANT , "Retreat point reached");
        }

        private void PrepareSearch(
            Vector3 centerPosition ,
            float searchRadius ,
            float searchDuration ,
            bool shouldMoveToCenter ,
            float angerInfluence ,
            string context)
        {
            _searchCenterPosition = centerPosition;
            _currentSearchRadius = Mathf.Max(0f , searchRadius * CHASE_AI_ANGER.GetSearchRadiusMultiplier(angerInfluence));
            _currentSearchPointCount = CHASE_AI_ANGER.GetSearchPointCount(angerInfluence);

            float pointCountRatio = _currentSearchPointCount / (float)_config.MinimumAngerSearchPointCount;

            _currentSearchDuration = Mathf.Max(0f , searchDuration * pointCountRatio);
            _isMovingToSearchCenter = shouldMoveToCenter;

            if ( shouldMoveToCenter && RequestDestination(centerPosition , context) )
            {
                return;
            }

            BeginAreaSearch();
        }

        private void BeginAreaSearch()
        {
            _isMovingToSearchCenter = false;

            bool hasSearchPoints = _search.BuildSearchPoints(_movement.Position, _searchCenterPosition, _currentSearchRadius, _currentSearchPointCount, _config.MinimumSearchPointDistance, _config.SampleRadius, _movement.AreaMask, _config.SearchPointGenerationAttemptCountPerPoint);

            if ( !hasSearchPoints )
            {
                CompleteSearch("Search point generation failed");
                return;
            }

            _searchWaitDurationPerPoint = _currentSearchDuration / _search.PointCount;

            Debug.Log($"[ChaseAIStateMachine] 수색 지점 생성 완료: Count={_search.PointCount}, Center={_searchCenterPosition}, Radius={_currentSearchRadius:F1}, WaitPerPoint={_searchWaitDurationPerPoint:F1}");

            RequestCurrentSearchPointOrComplete();
        }

        private void RequestCurrentSearchPointOrComplete()
        {
            while ( _search.TryGetCurrentPoint(out Vector3 searchPoint) )
            {
                string context = $"Search point {_search.CurrentPointIndex + 1}/{_search.PointCount}";

                if ( RequestDestination(searchPoint , context) )
                {
                    return;
                }

                _search.AdvanceToNextPoint();
            }

            CompleteSearch("All search points completed");
        }

        private void AdvanceSearchPoint()
        {
            _search.AdvanceToNextPoint();
            RequestCurrentSearchPointOrComplete();
        }

        private void ClearSearch()
        {
            _search.Clear();
            _searchCenterPosition = Vector3.zero;
            _currentSearchRadius = 0f;
            _currentSearchDuration = 0f;
            _currentSearchPointCount = 0;
            _searchWaitDurationPerPoint = 0f;
            _isMovingToSearchCenter = false;
        }

        private bool RequestAudioEvidenceDestination()
        {
            if ( !_hasActiveAudioInvestigation )
            {
                return false;
            }

            return RequestDestination(_audioSearchPosition , "Audio evidence");
        }

        private void RequestCurrentPatrolPoint()
        {
            if ( _patrolPoints == null || _patrolPoints.Count == 0 )
            {
                Debug.LogError("[ChaseAIStateMachine] 순찰 지점이 없습니다.");

                return;
            }

            Transform patrolPoint = _patrolPoints[_currentPatrolPointIndex];

            if ( patrolPoint == null )
            {
                AdvancePatrolPoint();
                StartWaiting(_config.PatrolWaitTime);
                return;
            }

            bool wasAccepted = RequestDestination(patrolPoint.position, $"Patrol point {_currentPatrolPointIndex}");

            if ( !wasAccepted )
            {
                AdvancePatrolPoint();
                StartWaiting(_config.PatrolWaitTime);
            }
        }

        private bool RequestDestination(Vector3 position , string context)
        {
            CHASE_AI_MOVE_REQUEST_RESULT result = _movement.TrySetDestination(position, out Vector3 correctedDestination);

            if ( result == CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                return true;
            }

            Debug.LogWarning(
                $"[ChaseAIStateMachine] " +
                $"{context} 목적지 요청 실패: {result}");

            return false;
        }

        private void AdvancePatrolPoint()
        {
            if ( _patrolPoints == null || _patrolPoints.Count == 0 )
            {
                return;
            }

            _currentPatrolPointIndex = ( _currentPatrolPointIndex + 1 ) % _patrolPoints.Count;
        }

        private void StartWaiting(float duration)
        {
            _stateTimer = duration;
            _isWaiting = true;
        }

        private bool UpdateWaiting(float deltaTime)
        {
            if ( !_isWaiting )
            {
                return false;
            }

            _stateTimer -= deltaTime;

            if ( _stateTimer > 0f )
            {
                return true;
            }

            _isWaiting = false;
            return false;
        }

        private void ClearAudioInvestigation()
        {
            _audioSearchPosition = Vector3.zero;
            _audioSearchRadius = 0f;
            _audioSearchDuration = 0f;
            _currentAudioIntensity = 0f;
            _hasActiveAudioInvestigation = false;
        }
    }
}
