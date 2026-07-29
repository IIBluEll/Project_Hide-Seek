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

        private void OnValidate()
        {
            _retreatStressRatio = Mathf.Max(_reactivationStressRatio , _retreatStressRatio);
        }
    }
}
