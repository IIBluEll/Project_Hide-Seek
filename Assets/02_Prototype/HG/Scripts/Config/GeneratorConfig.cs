using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 수리와 QTE 수치를 보관하는 설정 데이터.
    /// GAME_DESIGN_DOCUMENT.md 16.1에 따라 수치를 코드에 고정하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneratorConfig" , menuName = "HideSeek/Generator Config")]
    public sealed class GeneratorConfig : ScriptableObject
    {
        [Header("수리 진행")]
        [Tooltip("중단 없이 수리했을 때 완료까지 걸리는 시간(초).")]
        [SerializeField , Min(0.1f)] private float _repairDuration = 30f;

        [Tooltip("작업을 중단한 뒤 진행도가 감소하기 시작할 때까지의 유예 시간(초). GDD 7.3.7")]
        [SerializeField , Min(0f)] private float _decayGraceDuration = 3f;

        [Tooltip("유예 시간 이후 초당 감소하는 진행도(0~1 기준).")]
        [SerializeField , Min(0f)] private float _decayRatePerSecond = 0.04f;

        [Tooltip("수리 중 발전기 소음 이벤트를 발행하는 간격(초). GDD 6.1 GENERATOR")]
        [SerializeField , Min(0.1f)] private float _repairNoiseInterval = 2f;

        [Header("QTE 발생")]
        [Tooltip("중단 없이 완료할 때 보장되는 최소 QTE 발생 횟수. GDD 7.4")]
        [SerializeField , Min(1)] private int _minQteCount = 3;

        [Tooltip("QTE 사이의 최소 간격(초). 연속 발생을 막는다.")]
        [SerializeField , Min(0.1f)] private float _minQteInterval = 4f;

        [Tooltip("QTE 사이의 최대 간격(초). 최소 발생 횟수를 보장하도록 내부에서 추가로 제한된다.")]
        [SerializeField , Min(0.1f)] private float _maxQteInterval = 8f;

        [Header("QTE 판정")]
        [Tooltip("인디케이터가 게이지를 한 바퀴 도는 데 걸리는 시간(초).")]
        [SerializeField , Min(0.1f)] private float _qteSweepDuration = 1.6f;

        [Tooltip("성공 구간의 크기. 게이지 전체를 1로 본 비율이다. 난이도별로 조정하는 항목이다. GDD 12")]
        [SerializeField , Range(0.02f , 0.5f)] private float _qteSuccessZoneSize01 = 0.12f;

        [Tooltip("성공 구간이 배치될 수 있는 가장 이른 지점. 플레이어의 반응 시간을 확보한다.")]
        [SerializeField , Range(0f , 0.8f)] private float _qteZoneMinStart01 = 0.35f;

        [Header("QTE 실패")]
        [Tooltip("실패 시 즉시 감소하는 진행도(0~1 기준). GDD 7.4")]
        [SerializeField , Range(0f , 1f)] private float _qteFailurePenalty01 = 0.1f;

        [Tooltip("실패 후 수리가 멈춰 있는 시간(초). GDD 7.3.5")]
        [SerializeField , Min(0f)] private float _qteFailureStunDuration = 1f;

        public float RepairDuration => _repairDuration;
        public float DecayGraceDuration => _decayGraceDuration;
        public float DecayRatePerSecond => _decayRatePerSecond;
        public float RepairNoiseInterval => _repairNoiseInterval;
        public int MinQteCount => _minQteCount;
        public float MinQteInterval => _minQteInterval;
        public float QteSweepDuration => _qteSweepDuration;
        public float QteSuccessZoneSize01 => _qteSuccessZoneSize01;
        public float QteZoneMinStart01 => _qteZoneMinStart01;
        public float QteFailurePenalty01 => _qteFailurePenalty01;
        public float QteFailureStunDuration => _qteFailureStunDuration;

        /// <summary>
        /// 실제로 사용하는 QTE 최대 간격.
        /// 중단 없이 수리를 마쳤을 때 <see cref="MinQteCount"/>회 이상 발생하도록 상한을 낮춘다.
        /// 작업을 중단하고 재개하면 총 수리 시간이 늘어나므로 실제 발생 횟수는 이보다 많을 수 있다.
        /// </summary>
        public float GetEffectiveMaxQteInterval()
        {
            float tGuaranteedInterval = _repairDuration / (_minQteCount + 1);
            return Mathf.Max(_minQteInterval , Mathf.Min(_maxQteInterval , tGuaranteedInterval));
        }

        private void OnValidate()
        {
            _maxQteInterval = Mathf.Max(_minQteInterval , _maxQteInterval);
            _qteZoneMinStart01 = Mathf.Min(_qteZoneMinStart01 , 1f - _qteSuccessZoneSize01);
        }
    }
}
