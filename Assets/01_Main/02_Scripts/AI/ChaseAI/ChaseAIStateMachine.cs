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
        private readonly IReadOnlyList<Transform> _patrolPoints;

        private int _currentPatrolPointIndex;

        private float _stateTimer;
        private float _chaseRepathTimer;

        private bool _isWaiting;

        private Vector3 _audioSearchPosition;
        private float _audioSearchRadius;
        private float _audioSearchDuration;
        private float _currentAudioIntensity;

        private bool _hasActiveAudioInvestigation;

        public CHASE_AI_STATE CurrentState
        {
            get;
            private set;
        } = CHASE_AI_STATE.DORMANT;

        public ChaseAIStateMachine(
            ChaseAIConfig config ,
            ChaseAIMovement movement ,
            ChaseAIMemory memory ,
            IReadOnlyList<Transform> patrolPoints)
        {
            _config = config;
            _movement = movement;
            _memory = memory;
            _patrolPoints = patrolPoints;
        }

        public void Initialize()
        {
            ChangeState(CHASE_AI_STATE.PATROL, "State machine initialized");
        }

        public void Tick(float deltaTime, ChaseAIVisualObservation visualObservation)
        {
            if ( visualObservation.State == CHASE_AI_VISUAL_STATE.CONFIRMED && CurrentState != CHASE_AI_STATE.CHASE )
            {
                ChangeState(CHASE_AI_STATE.CHASE, "Player visually confirmed");
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
            }
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

            _audioSearchPosition = observation.NoiseData.Position;

            _currentAudioIntensity = newIntensity;

            _audioSearchRadius = Mathf.Lerp(_config.MaxAudioSearchRadius, _config.MinAudioSearchRadius, newIntensity);

            _audioSearchDuration = Mathf.Lerp(_config.MinAudioSearchDuration, _config.MaxAudioSearchDuration, newIntensity);

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
                    ChangeState(CHASE_AI_STATE.PATROL, "Updated audio destination invalid");
                }

                return wasDestinationAccepted;
            }

            ChangeState(CHASE_AI_STATE.INVESTIGATE, "New audio evidence accepted");

            // EnterInvestigate에서 목적지 설정에 실패하면 PATROL로 다시 변경되므로 false를 반환
            return CurrentState == CHASE_AI_STATE.INVESTIGATE;
        }

        public void Stop()
        {
            _movement.Stop();
            ClearAudioInvestigation();

            CurrentState = CHASE_AI_STATE.DORMANT;
            _isWaiting = false;
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
                    ChangeState(CHASE_AI_STATE.SEARCH, "Audio position investigation completed");
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
                    ChangeState(CHASE_AI_STATE.PATROL, $"Investigate movement failed: {moveStatus}");
                    break;
            }
        }

        private void UpdateChase(float deltaTime, ChaseAIVisualObservation visualObservation)
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
                RequestDestination(visualObservation.VisiblePosition, "Chase target");

                _chaseRepathTimer = _config.ChaseRepathInterval;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            if ( moveStatus == CHASE_AI_MOVE_STATUS.PATH_FAILED || moveStatus == CHASE_AI_MOVE_STATUS.STUCK )
            {
                ChangeState(CHASE_AI_STATE.SEARCH, $"Chase movement failed: {moveStatus}");
            }
        }

        private void UpdateSearch(float deltaTime)
        {
            if ( _isWaiting )
            {
                if ( !UpdateWaiting(deltaTime) )
                {
                    ChangeState(CHASE_AI_STATE.PATROL, "Search completed");
                }

                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus = _movement.UpdateMovement(deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.IDLE:
                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    StartWaiting(_config.SearchWaitTime);
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                case CHASE_AI_MOVE_STATUS.STUCK:
                    ChangeState(CHASE_AI_STATE.PATROL, $"Search movement failed: {moveStatus}");
                    break;
            }
        }

        private void ChangeState(CHASE_AI_STATE newState, string reason)
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
            }
        }

        private void EnterPatrol()
        {
            ClearAudioInvestigation();

            _movement.SetSpeed(_config.WalkSpeed);
            RequestCurrentPatrolPoint();
        }

        private void EnterInvestigate()
        {
            _movement.SetSpeed(_config.WalkSpeed);

            if ( !RequestAudioEvidenceDestination() )
            {
                ChangeState(CHASE_AI_STATE.PATROL , "Audio evidence destination invalid");
            }
        }

        private void EnterChase()
        {
            ClearAudioInvestigation();

            _movement.SetSpeed(_config.ChaseSpeed);
            _chaseRepathTimer = 0f;
        }

        private void EnterSearch()
        {
            _movement.SetSpeed(_config.WalkSpeed);

            // INVESTIGATE를 마치고 들어온 청각 수색
            if ( _hasActiveAudioInvestigation )
            {
                Debug.Log(
                    $"[ChaseAIStateMachine] 청각 수색 시작: " +
                    $"Position={_audioSearchPosition}, " +
                    $"Radius={_audioSearchRadius:F1}, " +
                    $"Duration={_audioSearchDuration:F1}");

                StartWaiting(_audioSearchDuration);
                return;
            }

            float currentTime = Time.time;

            // CHASE에서 시야를 놓친 경우
            if ( _memory.HasValidVisualEvidence(currentTime) )
            {
                RequestDestination(_memory.VisualEvidence.Position, "Last seen position");

                return;
            }

            if ( _memory.HasValidAudioEvidence(currentTime) )
            {
                RequestDestination(_memory.AudioEvidence.Position, "Last heard position");

                return;
            }

            StartWaiting(_config.SearchWaitTime);
        }

        private bool RequestAudioEvidenceDestination()
        {
            if ( !_hasActiveAudioInvestigation )
            {
                return false;
            }

            return RequestDestination(_audioSearchPosition, "Audio evidence");
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

        private bool RequestDestination(Vector3 position, string context)
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