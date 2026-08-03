using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIMemoryTester : MonoBehaviour
    {
        [SerializeField] private ChaseAIConfig _config;
        [SerializeField] private ChaseAIPerception _perception;

        private ChaseAIMemory _memory;

        private bool _hadVisualEvidence;
        private bool _hadAudioEvidence;
        private bool _isInitialized;

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
            if ( _config == null )
            {
                Debug.LogError(
                    "[ChaseAIMemoryTester] Config가 할당되지 않았습니다." ,
                    this);

                return;
            }

            if ( _perception == null )
            {
                Debug.LogError(
                    "[ChaseAIMemoryTester] Perception이 할당되지 않았습니다." ,
                    this);

                return;
            }

            _isInitialized = true;
        }

        private void Update()
        {
            if ( !_isInitialized )
            {
                return;
            }

            ChaseAIVisualObservation visualObservation =
                _perception.UpdatePerception(Time.deltaTime);

            _memory.RecordVisualEvidence(
                visualObservation ,
                Time.time ,
                _config.VisualEvidenceDuration);

            _memory.UpdateMemory(Time.time);

            ReportEvidenceStateChanges();
        }

        private void OnDisable()
        {
            if ( _perception == null )
            {
                return;
            }

            _perception.NoiseDetected -= OnNoiseDetected;
        }

        private void OnNoiseDetected(
            ChaseAIAudioObservation observation)
        {
            if ( _config == null || _memory == null )
            {
                return;
            }

            float duration =
                observation.PerceivedIntensity >=
                _config.StrongNoiseThreshold
                    ? _config.StrongNoiseEvidenceDuration
                    : _config.WeakNoiseEvidenceDuration;

            _memory.RecordAudioEvidence(
                observation ,
                duration);

            Debug.Log(
                $"[MemoryTester] 청각 증거 기록: " +
                $"Type={observation.NoiseData.NoiseType}, " +
                $"Position={observation.NoiseData.Position}, " +
                $"Duration={duration:F1}" ,
                this);
        }

        private void ReportEvidenceStateChanges()
        {
            bool hasVisualEvidence =
                _memory.HasValidVisualEvidence(Time.time);

            bool hasAudioEvidence =
                _memory.HasValidAudioEvidence(Time.time);

            if ( hasVisualEvidence != _hadVisualEvidence )
            {
                if ( hasVisualEvidence )
                {
                    Debug.Log(
                        $"[MemoryTester] 시각 증거 생성: " +
                        $"{_memory.VisualEvidence.Position}" ,
                        this);
                }
                else
                {
                    Debug.Log(
                        "[MemoryTester] 시각 증거 만료" ,
                        this);
                }

                _hadVisualEvidence = hasVisualEvidence;
            }

            if ( hasAudioEvidence != _hadAudioEvidence )
            {
                if ( hasAudioEvidence )
                {
                    Debug.Log(
                        $"[MemoryTester] 청각 증거 활성: " +
                        $"{_memory.AudioEvidence.Position}" ,
                        this);
                }
                else
                {
                    Debug.Log(
                        "[MemoryTester] 청각 증거 만료" ,
                        this);
                }

                _hadAudioEvidence = hasAudioEvidence;
            }
        }

        private void OnDrawGizmosSelected()
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