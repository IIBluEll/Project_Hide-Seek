using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChaseAIConfig _config;
        [SerializeField] private ChaseAIMovement _movement;
        [SerializeField] private ChaseAIPerception _perception;

        [Header("Patrol")]
        [SerializeField] private List<Transform> _patrolPoints = new();

        private ChaseAIMemory _memory;
        private ChaseAISearch _search;
        private ChaseAIStateMachine _stateMachine;

        private bool _isInitialized;

        public event Action RetreatFailed;

        public CHASE_AI_STATE CurrentState => _stateMachine != null ? _stateMachine.CurrentState : CHASE_AI_STATE.DORMANT;

        public bool IsRetreatPending => _stateMachine != null && _stateMachine.IsRetreatPending;

        private void Awake()
        {
            _memory = new ChaseAIMemory();
            _search = new ChaseAISearch();
        }

        private void OnEnable()
        {
            if ( _perception == null )
            {
                return;
            }

            _perception.NoiseDetected -= OnNoiseDetected;
            _perception.NoiseDetected += OnNoiseDetected;
        }

        private void Start()
        {
            if ( !ValidateReferences() )
            {
                return;
            }

            _stateMachine = new ChaseAIStateMachine(_config , _movement , _memory , _search , _patrolPoints);

            _stateMachine.Initialize();
            _isInitialized = true;
        }

        private void Update()
        {
            if ( !_isInitialized || _stateMachine.CurrentState == CHASE_AI_STATE.DORMANT )
            {
                return;
            }

            ChaseAIVisualObservation visualObservation = _perception.UpdatePerception(Time.deltaTime);

            _memory.RecordVisualEvidence(
                visualObservation ,
                Time.time ,
                _config.VisualEvidenceDuration);

            _memory.UpdateMemory(Time.time);

            _stateMachine.Tick(Time.deltaTime, visualObservation);
            ReportRetreatFailure();
        }

        public bool RequestActivation()
        {
            if ( !_isInitialized || _stateMachine == null )
            {
                Debug.LogWarning("[ChaseAIController] 초기화 전에 출현을 요청할 수 없습니다." , this);

                return false;
            }

            return _stateMachine.RequestActivation();
        }

        public bool RequestRetreat(Vector3 retreatPosition)
        {
            if ( !_isInitialized || _stateMachine == null )
            {
                Debug.LogWarning("[ChaseAIController] 초기화 전에 이탈을 요청할 수 없습니다." , this);

                return false;
            }

            bool wasAccepted = _stateMachine.RequestRetreat(retreatPosition);

            ReportRetreatFailure();

            return wasAccepted;
        }

        private void OnDisable()
        {
            if ( _perception != null )
            {
                _perception.NoiseDetected -= OnNoiseDetected;
            }

            _stateMachine?.Stop();
        }

        private void ReportRetreatFailure()
        {
            if ( _stateMachine == null || !_stateMachine.ConsumeRetreatFailure() )
            {
                return;
            }

            RetreatFailed?.Invoke();
        }

        private void OnNoiseDetected(ChaseAIAudioObservation observation)
        {
            if ( _config == null || _memory == null || _stateMachine == null)
            {
                return;
            }

            bool wasAccepted = _stateMachine.TryReceiveAudioEvidence(observation);

            if(!wasAccepted)
            {
                return;
            }

            float duration = observation.PerceivedIntensity >= _config.StrongNoiseThreshold ? _config.StrongNoiseEvidenceDuration : _config.WeakNoiseEvidenceDuration;

            _memory.RecordAudioEvidence(observation, duration);
        }

        public bool TryReceiveDirectorHint(MasterAIHint hint)
        {
            if ( !_isInitialized || _stateMachine == null )
            {
                Debug.LogWarning("[ChaseAIController] 초기화 전에 Director Hint를 전달할 수 없습니다." , this);

                return false;
            }

            return _stateMachine.TryReceiveDirectorHint(hint , Time.time);
        }

        private bool ValidateReferences()
        {
            if ( _config == null )
            {
                Debug.LogError("[ChaseAIController] Config가 없습니다.", this);

                return false;
            }

            if ( _movement == null )
            {
                Debug.LogError("[ChaseAIController] Movement가 없습니다.", this);

                return false;
            }

            if ( _perception == null )
            {
                Debug.LogError("[ChaseAIController] Perception이 없습니다.", this);

                return false;
            }

            if ( _patrolPoints.Count == 0 )
            {
                Debug.LogError("[ChaseAIController] 순찰 지점이 없습니다.", this);

                return false;
            }

            return true;
        }

        private void OnDrawGizmos()
        {
            DrawEvidenceGizmos();
            DrawSearchGizmos();
        }

        private void DrawEvidenceGizmos()
        {
            if ( _memory == null )
            {
                return;
            }

            if ( _memory.HasValidVisualEvidence(Time.time) )
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_memory.VisualEvidence.Position , 0.4f);
                Gizmos.DrawRay(_memory.VisualEvidence.Position , _memory.LastSeenMovementDirection * 2f);
            }

            if ( _memory.HasValidAudioEvidence(Time.time) )
            {
                Gizmos.color = new Color(1f , 0.5f , 0f);
                Gizmos.DrawWireSphere(_memory.AudioEvidence.Position , 0.5f);
            }
        }

        private void DrawSearchGizmos()
        {
            if ( _search == null || _stateMachine == null || _stateMachine.CurrentState != CHASE_AI_STATE.SEARCH )
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_stateMachine.SearchCenterPosition , _stateMachine.CurrentSearchRadius);

            Vector3 previousPosition = _stateMachine.SearchCenterPosition;

            for ( int pointIndex = 0; pointIndex < _search.SearchPoints.Count; pointIndex++ )
            {
                Vector3 searchPoint = _search.SearchPoints[pointIndex];

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(previousPosition , searchPoint);

                Gizmos.color = pointIndex == _search.CurrentPointIndex ? Color.red : Color.yellow;
                Gizmos.DrawSphere(searchPoint , 0.2f);

                previousPosition = searchPoint;
            }
        }
    }
}