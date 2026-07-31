using System;
using System.Collections.Generic;
using HideSeek.Gameplay;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChaseAIConfig _config;
        [SerializeField] private ChaseAIMovement _movement;
        [SerializeField] private ChaseAIPerception _perception;
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Header("Patrol")]
        [SerializeField] private List<Transform> _patrolPoints = new();

        private ChaseAIMemory _memory;
        private ChaseAISearch _search;
        private ChaseAIAnger _anger;
        private ChaseAIStateMachine _stateMachine;

        private bool _isInitialized;

        public event Action RetreatFailed;

        public CHASE_AI_STATE CurrentState => _stateMachine != null ? _stateMachine.CurrentState : CHASE_AI_STATE.DORMANT;

        public bool IsRetreatPending => _stateMachine != null && _stateMachine.IsRetreatPending;
        public bool IsInitialized => _isInitialized;
        public float NavMeshSampleRadius => _config != null ? _config.SampleRadius : 0.1f;
        public int AreaMask => _movement != null ? _movement.AreaMask : UnityEngine.AI.NavMesh.AllAreas;
        public float CurrentAnger => _anger != null ? _anger.CurrentAnger : 0f;
        public float AngerFloor => _anger != null ? _anger.AngerFloor : 0f;
        public int CompletedGeneratorCount => _anger != null ? _anger.CompletedGeneratorCount : 0;

        private void Awake()
        {
            _memory = new ChaseAIMemory();
            _search = new ChaseAISearch();
        }

        private void OnEnable()
        {
            if ( _perception != null )
            {
                _perception.NoiseDetected -= OnNoiseDetected;
                _perception.NoiseDetected += OnNoiseDetected;
            }

            if ( _gameProgressProvider != null )
            {
                _gameProgressProvider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
                _gameProgressProvider.CompletedGeneratorCountChanged += OnCompletedGeneratorCountChangedActioned;

                SynchronizeGameProgress();
            }
        }

        private void Start()
        {
            if ( !ValidateReferences() )
            {
                return;
            }

            _anger = new ChaseAIAnger(_config);
            SynchronizeGameProgress();

            _stateMachine = new ChaseAIStateMachine(_config , _movement , _memory , _search , _anger , _patrolPoints);

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

        public bool RequestActivation(Vector3 activationPosition)
        {
            if ( !_isInitialized || _stateMachine == null )
            {
                Debug.LogWarning("[ChaseAIController] 초기화 전에 출현을 요청할 수 없습니다." , this);

                return false;
            }

            if ( !_movement.TryWarp(activationPosition , out Vector3 correctedPosition) )
            {
                Debug.LogWarning($"[ChaseAIController] Vent 출현 위치로 이동할 수 없습니다: {activationPosition}" , this);

                return false;
            }

            Debug.Log($"[ChaseAIController] Vent 출현 위치 적용: {correctedPosition}" , this);

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

        public void ConfigureSearchZones(IReadOnlyList<AIWorldZone> zones)
        {
            if ( _search == null )
            {
                _search = new ChaseAISearch();
            }

            _search.ConfigureZones(zones);
        }

        private void OnDisable()
        {
            if ( _perception != null )
            {
                _perception.NoiseDetected -= OnNoiseDetected;
            }

            if ( _gameProgressProvider != null )
            {
                _gameProgressProvider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
            }

            _stateMachine?.Stop();
        }

        private void OnCompletedGeneratorCountChangedActioned(int completedGeneratorCount)
        {
            if ( _anger == null )
            {
                return;
            }

            _anger.SetCompletedGeneratorCount(completedGeneratorCount);
            _stateMachine?.RefreshAngerEffects();

            LogAngerState("발전기 완료 이벤트");
        }

        private void SynchronizeGameProgress()
        {
            if ( _gameProgressProvider == null || _anger == null )
            {
                return;
            }

            _anger.SetCompletedGeneratorCount(_gameProgressProvider.CompletedGeneratorCount);
            _stateMachine?.RefreshAngerEffects();

            LogAngerState("게임 진행도 동기화");
        }

        private void LogAngerState(string reason)
        {
            Debug.Log(
                $"[ChaseAIController] Anger 갱신: " +
                $"Reason={reason}, " +
                $"Completed={_anger.CompletedGeneratorCount}, " +
                $"Anger={_anger.CurrentAnger:F1}, " +
                $"Floor={_anger.AngerFloor:F1}, " +
                $"ChaseSpeedMultiplier={_anger.ChaseSpeedMultiplier:F2}, " +
                $"SearchRadiusMultiplier={_anger.SearchRadiusMultiplier:F2}, " +
                $"SearchPointCount={_anger.SearchPointCount}" ,
                this);
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

            if ( _gameProgressProvider == null )
            {
                Debug.LogWarning("[ChaseAIController] GameProgressProvider가 없어 Anger가 발전기 진행도와 연결되지 않습니다." , this);
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

                Gizmos.color = GetSearchPointColor(pointIndex);
                Gizmos.DrawLine(previousPosition , searchPoint);

                Gizmos.color = pointIndex == _search.CurrentPointIndex
                    ? Color.red
                    : GetSearchPointColor(pointIndex);

                Gizmos.DrawSphere(searchPoint , 0.2f);

                previousPosition = searchPoint;
            }
        }

        private Color GetSearchPointColor(int pointIndex)
        {
            if ( !_search.TryGetSearchPointSource(pointIndex , out CHASE_AI_SEARCH_POINT_SOURCE searchPointSource) )
            {
                return Color.yellow;
            }

            return searchPointSource switch
            {
                CHASE_AI_SEARCH_POINT_SOURCE.PREDICTED_DIRECTION => Color.green,
                CHASE_AI_SEARCH_POINT_SOURCE.DIRECTIONAL => new Color(1f , 0.6f , 0f),
                CHASE_AI_SEARCH_POINT_SOURCE.ZONE_COVERAGE => Color.cyan,
                CHASE_AI_SEARCH_POINT_SOURCE.HIDING_SPOT => new Color(1f , 0.25f , 0.7f),
                _ => Color.yellow
            };
        }
    }
}
