using HideSeek.AI;
using HideSeek.Generators;
using UnityEngine;

namespace HideSeek.Integration
{
    /// <summary>
    /// 발전기 이벤트를 AI 소음으로 옮긴다.
    /// 이 계층이 있어서 발전기 코드는 AI 타입을 참조하지 않는다.
    /// AI_GENERATOR_PLAYER_INTEGRATION.md 4장 기준이며, 발전기 한 대마다 하나씩 붙인다.
    ///
    /// 완료를 게임 진행도로 옮기는 일은 GeneratorDirector가 씬 단위로 맡는다.
    /// 전역 객체 참조를 발전기마다 들고 있으면 후보 지점 활성화(GDD 7.1) 구조에서 배선할 수 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorAIConnector : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Generator _generator;

        [Header("소음")]
        [Tooltip("NOISE_TYPE을 GENERATOR로 설정한다.")]
        [SerializeField] private NoiseEmitter _repairNoiseEmitter;

        [Tooltip("NOISE_TYPE을 QTE_FAILURE로 설정한다. 반경과 강도가 수리 소음보다 커야 한다. GDD 6.2")]
        [SerializeField] private NoiseEmitter _qteFailureNoiseEmitter;

#if UNITY_EDITOR
        private void Reset()
        {
            _generator = GetComponentInParent<Generator>();
        }
#endif

        private void Awake()
        {
            // 인스펙터 연결을 잊어도 같은 발전기 안에서 찾는다. 다른 발전기를 가리켜야 할 때만 인스펙터로 지정한다.
            if (_generator == null)
            {
                _generator = GetComponentInParent<Generator>();
            }
        }

        private void OnEnable()
        {
            if (_generator == null)
            {
                Debug.LogError($"[{nameof(GeneratorAIConnector)}] Generator 참조가 비어 있습니다.", this);
                return;
            }

            // 껐다 켜도 중복 구독되지 않도록 해제 후 구독한다.
            _generator.RepairNoiseOccurred -= OnRepairNoiseOccurredActioned;
            _generator.RepairNoiseOccurred += OnRepairNoiseOccurredActioned;

            _generator.QteFailureNoiseOccurred -= OnQteFailureNoiseOccurredActioned;
            _generator.QteFailureNoiseOccurred += OnQteFailureNoiseOccurredActioned;
        }

        private void OnDisable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.RepairNoiseOccurred -= OnRepairNoiseOccurredActioned;
            _generator.QteFailureNoiseOccurred -= OnQteFailureNoiseOccurredActioned;
        }

        private void OnRepairNoiseOccurredActioned(Vector3 position)
        {
            _repairNoiseEmitter?.EmitNoiseAt(position);
        }

        private void OnQteFailureNoiseOccurredActioned(Vector3 position)
        {
            _qteFailureNoiseEmitter?.EmitNoiseAt(position);
        }
    }
}
