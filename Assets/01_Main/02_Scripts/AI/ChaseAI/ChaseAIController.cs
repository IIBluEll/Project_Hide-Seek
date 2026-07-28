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
        private ChaseAIStateMachine _stateMachine;

        private bool _isInitialized;

        public CHASE_AI_STATE CurrentState => _stateMachine != null ? _stateMachine.CurrentState : CHASE_AI_STATE.DORMANT;

        private void Awake()
        {
            _memory = new ChaseAIMemory();
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

            _stateMachine = new ChaseAIStateMachine(
                _config ,
                _movement ,
                _memory ,
                _patrolPoints);

            _stateMachine.Initialize();
            _isInitialized = true;
        }

        private void Update()
        {
            if ( !_isInitialized )
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
        }

        private void OnDisable()
        {
            if ( _perception != null )
            {
                _perception.NoiseDetected -= OnNoiseDetected;
            }

            _stateMachine?.Stop();
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
            if ( _memory == null )
            {
                return;
            }

            if ( _memory.HasValidVisualEvidence(Time.time) )
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(
                    _memory.VisualEvidence.Position ,
                    0.4f);

                Gizmos.DrawRay(
                    _memory.VisualEvidence.Position ,
                    _memory.LastSeenMovementDirection * 2f);
            }

            if ( _memory.HasValidAudioEvidence(Time.time) )
            {
                Gizmos.color = new Color(
                    1f ,
                    0.5f ,
                    0f);

                Gizmos.DrawWireSphere(
                    _memory.AudioEvidence.Position ,
                    0.5f);
            }
        }
    }
}