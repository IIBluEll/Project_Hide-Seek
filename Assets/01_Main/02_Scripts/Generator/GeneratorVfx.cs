using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 이벤트를 이펙트로 옮긴다. <see cref="Generator"/>는 VFX를 모르며 이 컴포넌트가 구독만 한다.
    /// <see cref="GeneratorSound"/>와 같은 자리에 두고 같은 이벤트를 본다.
    ///
    /// 스파크는 수리가 끝나기 전까지 계속 돈다. 수리 중인지 아닌지는 보지 않는다.
    /// 고장난 발전기라는 표시이므로 플레이어가 붙어 있지 않아도 켜져 있어야 한다.
    ///
    /// 인스펙터: 두 이펙트 모두 이 발전기 프리팹 하위에 두고 넣는다.
    /// 폭발 쪽은 Looping을 끈다. 루프면 한 번 터진 뒤 멈추지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Generator))]
    public sealed class GeneratorVfx : MonoBehaviour
    {
        [SerializeField] private Generator _generator;

        [Header("이펙트")]
        [Tooltip("수리 완료 전까지 계속 재생한다. 자식 파티클도 함께 제어한다.")]
        [SerializeField] private ParticleSystem _sparkLoop;

        [Tooltip("QTE 실패마다 한 번 재생한다. Looping을 끈 파티클을 넣는다.")]
        [SerializeField] private ParticleSystem _qteFailureBurst;

#if UNITY_EDITOR
        private void Reset()
        {
            _generator = GetComponent<Generator>();
        }
#endif

        private void Awake()
        {
            // RequireComponent가 같은 오브젝트의 Generator를 보장하므로 인스펙터 연결을 잊어도 찾는다.
            if (_generator == null)
            {
                _generator = GetComponent<Generator>();
            }

            // RequireComponent 도입 이전에 만들어진 오브젝트를 위해 남겨 둔다.
            if (_generator == null)
            {
                Debug.LogError($"[{nameof(GeneratorVfx)}] Generator 참조가 비어 있습니다." , this);
                return;
            }

            // 폭발 이펙트에 Play On Awake가 켜져 있으면 발전기를 배치하는 순간 한 번 터진다.
            // Awake에서 지우므로 첫 프레임이 그려지기 전에 정리된다.
            StopBurst();
        }

        private void OnEnable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.QteFinished += OnQteFinishedActioned;
            _generator.Completed += OnGeneratorCompletedActioned;

            SetSparkLoop(_generator.State != GENERATOR_STATE.COMPLETED);
        }

        private void OnDisable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.QteFinished -= OnQteFinishedActioned;
            _generator.Completed -= OnGeneratorCompletedActioned;

            SetSparkLoop(false);
            StopBurst();
        }

        private void OnQteFinishedActioned(QTE_RESULT result)
        {
            if (result != QTE_RESULT.FAILURE)
            {
                return;
            }

            PlayBurst();
        }

        private void OnGeneratorCompletedActioned()
        {
            SetSparkLoop(false);
        }

        private void SetSparkLoop(bool isPlaying)
        {
            if (_sparkLoop == null)
            {
                return;
            }

            if (isPlaying)
            {
                // 이미 돌고 있는데 다시 Play하면 처음부터 재생돼 눈에 띈다.
                if (_sparkLoop.isPlaying == false)
                {
                    _sparkLoop.Play(true);
                }

                return;
            }

            // 이미 태어난 입자는 수명대로 사라지게 둔다. 수리 완료 순간에 화면에서 툭 끊기지 않는다.
            _sparkLoop.Stop(true , ParticleSystemStopBehavior.StopEmitting);
        }

        private void PlayBurst()
        {
            if (_qteFailureBurst == null)
            {
                return;
            }

            // 연속 실패로 이전 폭발이 남아 있을 수 있다. 지우고 다시 내야 매번 같은 세기로 보인다.
            StopBurst();
            _qteFailureBurst.Play(true);
        }

        private void StopBurst()
        {
            if (_qteFailureBurst == null)
            {
                return;
            }

            _qteFailureBurst.Stop(true , ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
