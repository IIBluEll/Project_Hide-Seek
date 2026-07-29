using UnityEngine;


namespace HideSeek.AI
{
    [CreateAssetMenu(fileName = "MasterAIConfig" , menuName = "HideSeek/AI/Master AI Config")]
    public sealed class MasterAIConfig : ScriptableObject
    {
        [Header("Global Stress")]
        [SerializeField, Min(1f)] private float _maximumGlobalStress = 100f;
        [SerializeField, Min(0f)] private float _chaseStressIncreaseRate = 10f;
        [SerializeField, Min(0f)] private float _nearStressIncreaseRate = 4f;
        [SerializeField, Min(0f)] private float _activeStressDecreaseRate = 2f;
        [SerializeField, Min(0f)] private float _dormantStressDecreaseRate = 5f;
        [SerializeField, Min(0.1f)] private float _safeDistance = 20f;

        [Header("Activity Cycle")]
        [SerializeField, Min(0f)] private float _minimumDormantDuration = 15f;
        [SerializeField, Range(0f, 1f)] private float _reactivationStressRatio = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _retreatStressRatio = 1f;
        [SerializeField, Min(0f)] private float _retreatRetryDelay = 3f;

        [Header("Zone Selection")]
        [SerializeField, Min(0f)] private float _lowStressPlayerZoneWeight = 0.3f;
        [SerializeField, Min(0f)] private float _lowStressAdjacentZoneWeight = 0.5f;
        [SerializeField, Min(0f)] private float _lowStressOtherZoneWeight = 0.2f;
        [SerializeField, Min(0f)] private float _highStressPlayerZoneWeight = 0.7f;
        [SerializeField, Min(0f)] private float _highStressAdjacentZoneWeight = 0.3f;
        [SerializeField, Min(0f)] private float _highStressOtherZoneWeight = 0f;

        [Header("Director Hint")]
        [SerializeField, Min(0f)] private float _minimumHintRadius = 6f;
        [SerializeField, Min(0f)] private float _maximumHintRadius = 14f;
        [SerializeField, Range(0f, 1f)] private float _minimumHintUrgency = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _maximumHintUrgency = 0.8f;
        [SerializeField, Min(0f)] private float _hintDuration = 5f;
        [SerializeField, Min(0.1f)] private float _hintNavMeshSampleRadius = 2f;
        [SerializeField, Min(1)] private int _hintPositionAttemptCount = 10;
        [SerializeField, Min(0f)] private float _minimumHintInterval = 8f;
        [SerializeField, Min(0f)] private float _maximumHintInterval = 14f;

        public float MaximumGlobalStress => _maximumGlobalStress;
        public float ChaseStressIncreaseRate => _chaseStressIncreaseRate;
        public float NearStressIncreaseRate => _nearStressIncreaseRate;
        public float ActiveStressDecreaseRate => _activeStressDecreaseRate;
        public float DormantStressDecreaseRate => _dormantStressDecreaseRate;
        public float SafeDistance => _safeDistance;

        public float MinimumDormantDuration => _minimumDormantDuration;
        public float ReactivationStressThreshold => _maximumGlobalStress * _reactivationStressRatio;
        public float RetreatStressThreshold => _maximumGlobalStress * _retreatStressRatio;
        public float RetreatRetryDelay => _retreatRetryDelay;

        public float LowStressPlayerZoneWeight => _lowStressPlayerZoneWeight;
        public float LowStressAdjacentZoneWeight => _lowStressAdjacentZoneWeight;
        public float LowStressOtherZoneWeight => _lowStressOtherZoneWeight;
        public float HighStressPlayerZoneWeight => _highStressPlayerZoneWeight;
        public float HighStressAdjacentZoneWeight => _highStressAdjacentZoneWeight;
        public float HighStressOtherZoneWeight => _highStressOtherZoneWeight;

        public float MinimumHintRadius => _minimumHintRadius;
        public float MaximumHintRadius => _maximumHintRadius;
        public float MinimumHintUrgency => _minimumHintUrgency;
        public float MaximumHintUrgency => _maximumHintUrgency;
        public float HintDuration => _hintDuration;
        public float HintNavMeshSampleRadius => _hintNavMeshSampleRadius;
        public int HintPositionAttemptCount => _hintPositionAttemptCount;
        public float MinimumHintInterval => _minimumHintInterval;
        public float MaximumHintInterval => _maximumHintInterval;

        private void OnValidate()
        {
            _retreatStressRatio = Mathf.Max(_reactivationStressRatio , _retreatStressRatio);
            _maximumHintRadius = Mathf.Max(_minimumHintRadius , _maximumHintRadius);
            _maximumHintUrgency = Mathf.Max(_minimumHintUrgency , _maximumHintUrgency);
            _hintPositionAttemptCount = Mathf.Max(1 , _hintPositionAttemptCount);
            _maximumHintInterval = Mathf.Max(_minimumHintInterval , _maximumHintInterval);
        }
    }
}
