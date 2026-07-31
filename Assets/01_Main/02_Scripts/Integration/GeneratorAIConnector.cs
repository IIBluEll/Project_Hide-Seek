using HideSeek.AI;
using HideSeek.Gameplay;
using HideSeek.Generators;
using UnityEngine;

namespace HideSeek.Integration
{
    /// <summary>
    /// 발전기 이벤트를 AI 소음과 게임 진행도로 옮긴다.
    /// 이 계층이 있어서 발전기 코드는 AI 타입을 참조하지 않는다.
    /// AI_GENERATOR_PLAYER_INTEGRATION.md 4장 기준이며, 발전기 한 대마다 하나씩 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorAIConnector : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Generator _generator;
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Header("소음")]
        [Tooltip("NOISE_TYPE을 GENERATOR로 설정한다.")]
        [SerializeField] private NoiseEmitter _repairNoiseEmitter;

        [Tooltip("NOISE_TYPE을 QTE_FAILURE로 설정한다. 반경과 강도가 수리 소음보다 커야 한다. GDD 6.2")]
        [SerializeField] private NoiseEmitter _qteFailureNoiseEmitter;

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

            _generator.Completed -= OnGeneratorCompletedActioned;
            _generator.Completed += OnGeneratorCompletedActioned;
        }

        private void OnDisable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.RepairNoiseOccurred -= OnRepairNoiseOccurredActioned;
            _generator.QteFailureNoiseOccurred -= OnQteFailureNoiseOccurredActioned;
            _generator.Completed -= OnGeneratorCompletedActioned;
        }

        private void OnRepairNoiseOccurredActioned(Vector3 position)
        {
            _repairNoiseEmitter?.EmitNoiseAt(position);
        }

        private void OnQteFailureNoiseOccurredActioned(Vector3 position)
        {
            _qteFailureNoiseEmitter?.EmitNoiseAt(position);
        }

        private void OnGeneratorCompletedActioned()
        {
            if (_gameProgressProvider == null)
            {
                Debug.LogError($"[{nameof(GeneratorAIConnector)}] GameProgressProvider가 없습니다.", this);
                return;
            }

            _gameProgressProvider.NotifyGeneratorCompleted();
        }
    }
}
