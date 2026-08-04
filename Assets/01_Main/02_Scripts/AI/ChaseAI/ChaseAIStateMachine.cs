using System;
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
        ATTACK,
        SEARCH,
        RETREAT
    }

    public sealed class ChaseAIStateMachine
    {
        private readonly ChaseAIConfig _config;
        private readonly ChaseAIMovement _movement;

        private readonly ChaseAIInvestigationContext INVESTIGATION_CONTEXT;
        private readonly ChaseAIEvidenceSelector EVIDENCE_SELECTOR;
        private readonly ChaseAIChaseBehavior CHASE_BEHAVIOR;
        private readonly ChaseAISearchBehavior SEARCH_BEHAVIOR;
        private readonly ChaseAIPatrolRoute PATROL_ROUTE;

        private float _stateTimer;
        private bool _isWaiting;

        private Vector3 _retreatPosition;
        private bool _isRetreatPending;
        private bool _hasRetreatFailed;
        private bool _hasPlayerCaughtRequest;

        public event Action<CHASE_AI_STATE , CHASE_AI_STATE> StateChanged;

        public Vector3 SearchCenterPosition => SEARCH_BEHAVIOR.SearchCenterPosition;
        public float CurrentSearchRadius => SEARCH_BEHAVIOR.CurrentSearchRadius;
        public CHASE_AI_SEARCH_ACTION CurrentSearchAction => SEARCH_BEHAVIOR.CurrentSearchAction;
        public float SearchActionProgress => SEARCH_BEHAVIOR.SearchActionProgress;
        public float SearchActionRemainingTime => SEARCH_BEHAVIOR.SearchActionRemainingTime;
        public string ActiveInvestigationName => CurrentState == CHASE_AI_STATE.INVESTIGATE
            ? EVIDENCE_SELECTOR.ActiveInvestigationName
            : "NONE";
        public string ActiveSearchContext => CurrentState == CHASE_AI_STATE.SEARCH
            ? SEARCH_BEHAVIOR.ActiveSearchContext
            : string.Empty;
        public bool IsRetreatPending => _isRetreatPending;
        public CHASE_AI_EVIDENCE_TYPE ActiveEvidenceType => CurrentState switch
        {
            CHASE_AI_STATE.INVESTIGATE => EVIDENCE_SELECTOR.ActiveInvestigationType,
            CHASE_AI_STATE.SEARCH => SEARCH_BEHAVIOR.ActiveEvidenceType,
            _ => CHASE_AI_EVIDENCE_TYPE.NONE
        };

        public CHASE_AI_STATE CurrentState
        {
            get;
            private set;
        } = CHASE_AI_STATE.DORMANT;

        public ChaseAIStateMachine(
            ChaseAIConfig config ,
            ChaseAIMovement movement ,
            ChaseAIMemory memory ,
            ChaseAISearch search ,
            ChaseAIAnger chaseAIAnger ,
            IReadOnlyList<Transform> patrolPoints ,
            IReadOnlyList<AIWorldZone> zones)
        {
            _config = config;
            _movement = movement;

            INVESTIGATION_CONTEXT = new ChaseAIInvestigationContext(config);
            EVIDENCE_SELECTOR = new ChaseAIEvidenceSelector(config , memory , INVESTIGATION_CONTEXT);
            CHASE_BEHAVIOR = new ChaseAIChaseBehavior(config , movement , chaseAIAnger);
            SEARCH_BEHAVIOR = new ChaseAISearchBehavior(config , movement , search , chaseAIAnger);
            PATROL_ROUTE = new ChaseAIPatrolRoute(patrolPoints);
            PATROL_ROUTE.ConfigureZones(zones);
        }

        public void Initialize()
        {
            _movement.Stop();
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearAllEvidence();
            PATROL_ROUTE.Clear();

            CurrentState = CHASE_AI_STATE.DORMANT;
            _retreatPosition = Vector3.zero;
            _isWaiting = false;
            _isRetreatPending = false;
            _hasRetreatFailed = false;
            _hasPlayerCaughtRequest = false;
            _stateTimer = 0f;

            Debug.Log("[ChaseAIStateMachine] DORMANT 상태로 초기화되었습니다.");
        }

        public void ConfigurePatrolZones(IReadOnlyList<AIWorldZone> zones)
        {
            PATROL_ROUTE.ConfigureZones(zones);

            if ( CurrentState == CHASE_AI_STATE.PATROL )
            {
                _movement.Stop();
                _isWaiting = false;
                _stateTimer = 0f;
                PATROL_ROUTE.Refresh(_movement.Position);
                RequestCurrentPatrolPoint();
            }
        }

        public void RefreshAngerEffects()
        {
            if ( CurrentState == CHASE_AI_STATE.CHASE )
            {
                CHASE_BEHAVIOR.RefreshAngerEffect();
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
            _hasPlayerCaughtRequest = false;

            ChangeState(CHASE_AI_STATE.PATROL , "Director activation requested");

            return CurrentState == CHASE_AI_STATE.PATROL;
        }

        public bool RequestRetreat(Vector3 retreatPosition)
        {
            if ( CurrentState == CHASE_AI_STATE.DORMANT || CurrentState == CHASE_AI_STATE.ATTACK )
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

        public bool ConsumePlayerCaughtRequest()
        {
            if ( !_hasPlayerCaughtRequest )
            {
                return false;
            }

            _hasPlayerCaughtRequest = false;

            return true;
        }

        public void Tick(float deltaTime , ChaseAIVisualObservation visualObservation)
        {
            if ( CurrentState == CHASE_AI_STATE.DORMANT )
            {
                return;
            }

            if ( visualObservation.State == CHASE_AI_VISUAL_STATE.CONFIRMED &&
                CurrentState != CHASE_AI_STATE.CHASE &&
                CurrentState != CHASE_AI_STATE.ATTACK )
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
                    UpdateChase(deltaTime , visualObservation);
                    break;

                case CHASE_AI_STATE.ATTACK:
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

            if ( !EVIDENCE_SELECTOR.TryReceiveDirectorHint(hint , currentTime) )
            {
                return false;
            }

            ChangeState(CHASE_AI_STATE.INVESTIGATE , "Director hint accepted");

            return CurrentState == CHASE_AI_STATE.INVESTIGATE;
        }

        public bool TryReceiveAudioEvidence(ChaseAIAudioObservation observation)
        {
            if ( CurrentState == CHASE_AI_STATE.CHASE ||
                CurrentState == CHASE_AI_STATE.ATTACK ||
                CurrentState == CHASE_AI_STATE.DORMANT ||
                CurrentState == CHASE_AI_STATE.RETREAT )
            {
                return false;
            }

            if ( !EVIDENCE_SELECTOR.TryReceiveAudioEvidence(observation) )
            {
                return false;
            }

            if ( CurrentState == CHASE_AI_STATE.INVESTIGATE )
            {
                _isWaiting = false;
                _stateTimer = 0f;

                if ( !PrepareInvestigationSearch() )
                {
                    ChangeState(CHASE_AI_STATE.PATROL , "Updated audio search preparation failed");

                    return false;
                }

                bool wasDestinationAccepted = RequestInvestigationDestination();

                if ( !wasDestinationAccepted )
                {
                    ChangeState(CHASE_AI_STATE.PATROL , "Updated audio destination invalid");
                }

                return wasDestinationAccepted;
            }

            ChangeState(CHASE_AI_STATE.INVESTIGATE , "New audio evidence accepted");

            return CurrentState == CHASE_AI_STATE.INVESTIGATE;
        }

        public void Stop()
        {
            if ( CurrentState != CHASE_AI_STATE.DORMANT )
            {
                ChangeState(CHASE_AI_STATE.DORMANT , "State machine stopped");

                return;
            }

            _movement.Stop();
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearAllEvidence();
            PATROL_ROUTE.Clear();

            CurrentState = CHASE_AI_STATE.DORMANT;
            _isWaiting = false;
            _retreatPosition = Vector3.zero;
            _isRetreatPending = false;
            _hasRetreatFailed = false;
            _hasPlayerCaughtRequest = false;
            _stateTimer = 0f;
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
                    Debug.LogWarning($"[ChaseAIStateMachine] 순찰 이동 실패: {moveStatus}");

                    AdvancePatrolPoint();
                    StartWaiting(_config.PatrolWaitTime);
                    break;
            }
        }

        private void UpdateInvestigate(float deltaTime)
        {
            if ( IsInsidePreparedSearchArea() )
            {
                StartPreparedInvestigationSearch(
                    $"{EVIDENCE_SELECTOR.ActiveInvestigationName} search area entered");

                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    StartPreparedInvestigationSearch(
                        $"{EVIDENCE_SELECTOR.ActiveInvestigationName} center reached");
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    ChangeState(CHASE_AI_STATE.PATROL , $"Investigate movement failed: {moveStatus}");
                    break;
            }
        }

        private void UpdateChase(
            float deltaTime ,
            ChaseAIVisualObservation visualObservation)
        {
            if ( IsPlayerWithinAttackRange(visualObservation) )
            {
                ChangeState(CHASE_AI_STATE.ATTACK , "Player entered attack range");

                return;
            }

            CHASE_AI_CHASE_RESULT chaseResult = CHASE_BEHAVIOR.Tick(deltaTime , visualObservation);

            if ( chaseResult == CHASE_AI_CHASE_RESULT.RUNNING )
            {
                return;
            }

            string reason = CHASE_BEHAVIOR.LastResultReason;

            ChangeState(CHASE_AI_STATE.SEARCH , reason);
        }

        private void UpdateSearch(float deltaTime)
        {
            CHASE_AI_BEHAVIOR_STATUS searchStatus = SEARCH_BEHAVIOR.Tick(deltaTime);

            HandleSearchStatus(searchStatus);
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

            if ( previousState == CHASE_AI_STATE.CHASE )
            {
                CHASE_BEHAVIOR.Stop();
            }

            if ( previousState == CHASE_AI_STATE.SEARCH )
            {
                SEARCH_BEHAVIOR.Stop();
            }

            CurrentState = newState;

            Debug.Log($"[ChaseAIStateMachine] {previousState} → {newState}, Reason: {reason}");

            StateChanged?.Invoke(previousState , newState);

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

                case CHASE_AI_STATE.ATTACK:
                    EnterAttack();
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
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearAllEvidence();
            PATROL_ROUTE.Clear();

            _retreatPosition = Vector3.zero;
            _isRetreatPending = false;
            _hasRetreatFailed = false;
            _hasPlayerCaughtRequest = false;
        }

        private void EnterRetreat()
        {
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearAllEvidence();

            _movement.SetSpeed(_config.WalkSpeed);

            if ( !RequestDestination(_retreatPosition , "Retreat point") )
            {
                FailRetreat("Retreat destination invalid");
            }
        }

        private void EnterPatrol()
        {
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearAllEvidence();

            _movement.SetSpeed(_config.WalkSpeed);
            PATROL_ROUTE.Refresh(_movement.Position);

            if ( PATROL_ROUTE.IsUsingZoneRoute )
            {
                Debug.Log(
                    $"[ChaseAIStateMachine] Zone 순찰 경로 적용: " +
                    $"Zone={PATROL_ROUTE.CurrentZone.DisplayName}, " +
                    $"Count={PATROL_ROUTE.PointCount}");
            }
            else
            {
                Debug.LogWarning(
                    "[ChaseAIStateMachine] 현재 Zone의 Coverage 지점이 없어 전역 순찰 지점을 사용합니다.");
            }

            RequestCurrentPatrolPoint();
        }

        private void EnterInvestigate()
        {
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();
            _movement.SetSpeed(_config.WalkSpeed);

            if ( !EVIDENCE_SELECTOR.TryGetInvestigationDestination(
                    out Vector3 investigationPosition ,
                    out string context) )
            {
                ChangeState(CHASE_AI_STATE.PATROL , "No investigation evidence");

                return;
            }

            if ( !PrepareInvestigationSearch() )
            {
                ChangeState(CHASE_AI_STATE.PATROL , $"{context} search preparation failed");

                return;
            }

            if ( !RequestDestination(investigationPosition , context) )
            {
                ChangeState(CHASE_AI_STATE.PATROL , $"{context} destination invalid");
            }
        }

        private void EnterChase()
        {
            SEARCH_BEHAVIOR.Stop();
            EVIDENCE_SELECTOR.ClearInvestigations();
            CHASE_BEHAVIOR.Begin();
        }

        private void EnterAttack()
        {
            CHASE_BEHAVIOR.Stop();
            SEARCH_BEHAVIOR.Stop();

            _isRetreatPending = false;
            _hasPlayerCaughtRequest = true;
        }

        private void EnterSearch()
        {
            CHASE_BEHAVIOR.Stop();

            if ( SEARCH_BEHAVIOR.IsPrepared )
            {
                HandleSearchStatus(SEARCH_BEHAVIOR.BeginPrepared());

                return;
            }

            if ( !EVIDENCE_SELECTOR.TryCreateSearchRequest(Time.time , out ChaseAISearchRequest searchRequest) )
            {
                CompleteSearch("No valid evidence for search");

                return;
            }

            CHASE_AI_BEHAVIOR_STATUS searchStatus = SEARCH_BEHAVIOR.Begin(searchRequest);

            HandleSearchStatus(searchStatus);
        }

        private void HandleSearchStatus(CHASE_AI_BEHAVIOR_STATUS searchStatus)
        {
            if ( searchStatus == CHASE_AI_BEHAVIOR_STATUS.RUNNING )
            {
                return;
            }

            if ( searchStatus == CHASE_AI_BEHAVIOR_STATUS.TARGET_FOUND )
            {
                ChangeState(CHASE_AI_STATE.ATTACK , "Player found during hiding spot inspection");

                return;
            }

            string reason = SEARCH_BEHAVIOR.LastResultReason;

            if ( string.IsNullOrEmpty(reason) )
            {
                reason = searchStatus == CHASE_AI_BEHAVIOR_STATUS.FAILED
                    ? "Search behavior failed"
                    : "Search behavior completed";
            }

            CompleteSearch(reason);
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

        private bool IsPlayerWithinAttackRange(ChaseAIVisualObservation visualObservation)
        {
            return visualObservation.CanAttackTarget;
        }

        private bool PrepareInvestigationSearch()
        {
            if ( !EVIDENCE_SELECTOR.TryCreateSearchRequest(
                    Time.time ,
                    out ChaseAISearchRequest searchRequest) )
            {
                return false;
            }

            return SEARCH_BEHAVIOR.Prepare(searchRequest);
        }

        private bool IsInsidePreparedSearchArea()
        {
            if ( !SEARCH_BEHAVIOR.IsPrepared )
            {
                return false;
            }

            float searchRadius = SEARCH_BEHAVIOR.CurrentSearchRadius;

            if ( searchRadius <= 0f )
            {
                return false;
            }

            Vector3 offset = _movement.Position - SEARCH_BEHAVIOR.SearchCenterPosition;
            offset.y = 0f;

            return offset.sqrMagnitude <= searchRadius * searchRadius;
        }

        private void StartPreparedInvestigationSearch(string reason)
        {
            if ( !SEARCH_BEHAVIOR.IsPrepared )
            {
                ChangeState(CHASE_AI_STATE.PATROL , "Prepared investigation search was missing");

                return;
            }

            ChangeState(CHASE_AI_STATE.SEARCH , reason);
        }

        private bool RequestInvestigationDestination()
        {
            if ( !EVIDENCE_SELECTOR.TryGetInvestigationDestination(
                    out Vector3 investigationPosition ,
                    out string context) )
            {
                return false;
            }

            return RequestDestination(investigationPosition , context);
        }

        private void RequestCurrentPatrolPoint()
        {
            if ( !PATROL_ROUTE.TryGetCurrentPoint(
                    out Vector3 patrolPosition ,
                    out string context) )
            {
                Debug.LogError("[ChaseAIStateMachine] 사용할 수 있는 순찰 지점이 없습니다.");
                AdvancePatrolPoint();
                StartWaiting(_config.PatrolWaitTime);

                return;
            }

            bool wasAccepted = RequestDestination(
                patrolPosition ,
                context);

            if ( !wasAccepted )
            {
                AdvancePatrolPoint();
                StartWaiting(_config.PatrolWaitTime);
            }
        }

        private bool RequestDestination(Vector3 position , string context)
        {
            CHASE_AI_MOVE_REQUEST_RESULT result = _movement.TrySetDestination(position);

            if ( result == CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                return true;
            }

            Debug.LogWarning($"[ChaseAIStateMachine] {context} 목적지 요청 실패: {result}");

            return false;
        }

        private void AdvancePatrolPoint()
        {
            PATROL_ROUTE.Advance();
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
    }
}
