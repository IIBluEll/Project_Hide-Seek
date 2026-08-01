using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 이벤트를 소리로 옮긴다. <see cref="Generator"/>는 오디오를 모르며 이 컴포넌트가 구독만 한다.
    ///
    /// AudioSource를 둘로 나눈 이유는 감쇠 거리와 볼륨을 따로 주기 위해서다.
    /// 기계음은 가까이서만 들리고 QTE 실패음은 더 멀리 퍼져야 한다. GDD 6.2
    ///
    /// 인스펙터 설정 기준
    /// - 두 AudioSource 모두 Spatial Blend를 1로 둔다. 기본값 0이면 맵 어디서나 같은 크기로 들린다.
    /// - Volume Rolloff는 Linear나 Custom을 쓴다. 기본값인 Logarithmic은 Max Distance를 넘어도
    ///   볼륨이 0이 되지 않아, 멀리 있는 발전기까지 계속 재생 대상으로 남는다.
    /// - Output에 SFX 믹서 그룹을 직접 연결한다.
    ///
    /// TODO: 사운드 공용 구조가 만들어지면 믹서 그룹 연결과 볼륨 적용을 그쪽으로 옮긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorSound : MonoBehaviour
    {
        [SerializeField] private Generator _generator;

        [Header("소스")]
        [Tooltip("기계음과 수리음을 재생한다. Max Distance를 좁게 둔다.")]
        [SerializeField] private AudioSource _loopSource;

        [Tooltip("QTE 판정과 완료음을 재생한다. Max Distance를 넓게 둔다.")]
        [SerializeField] private AudioSource _eventSource;

        [Header("루프 클립")]
        [SerializeField] private AudioClip _idleLoopClip;
        [SerializeField] private AudioClip _repairLoopClip;

        [Tooltip("GDD 7.5의 완료 후 기계 작동음. 비워두면 완료 시 루프가 멈춘다.")]
        [SerializeField] private AudioClip _completedLoopClip;

        [Header("이벤트 클립")]
        [SerializeField] private AudioClip _qteSuccessClip;
        [SerializeField] private AudioClip _qteFailureClip;
        [SerializeField] private AudioClip _completedClip;

        private void Awake()
        {
            if (_generator == null)
            {
                Debug.LogError($"[{nameof(GeneratorSound)}] Generator 참조가 비어 있습니다.", this);
                return;
            }

            if (_loopSource != null)
            {
                _loopSource.loop = true;
                _loopSource.playOnAwake = false;
            }

            if (_eventSource != null)
            {
                _eventSource.playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.RepairStarted += OnRepairStartedActioned;
            _generator.RepairStopped += OnRepairStoppedActioned;
            _generator.QteFinished += OnQteFinishedActioned;
            _generator.Completed += OnGeneratorCompletedActioned;

            PlayLoop(GetLoopClip());
        }

        private void OnDisable()
        {
            if (_generator == null)
            {
                return;
            }

            _generator.RepairStarted -= OnRepairStartedActioned;
            _generator.RepairStopped -= OnRepairStoppedActioned;
            _generator.QteFinished -= OnQteFinishedActioned;
            _generator.Completed -= OnGeneratorCompletedActioned;

            PlayLoop(null);
        }

        private void OnRepairStartedActioned(Generator generator)
        {
            PlayLoop(_repairLoopClip);
        }

        private void OnRepairStoppedActioned(Generator generator)
        {
            // 완료로 인한 중단이면 곧바로 Completed가 이어지므로 여기서 손대지 않는다.
            if (_generator.State == GENERATOR_STATE.COMPLETED)
            {
                return;
            }

            PlayLoop(_idleLoopClip);
        }

        private void OnQteFinishedActioned(QTE_RESULT result)
        {
            PlayEvent(result == QTE_RESULT.SUCCESS ? _qteSuccessClip : _qteFailureClip);
        }

        private void OnGeneratorCompletedActioned()
        {
            PlayLoop(_completedLoopClip);
            PlayEvent(_completedClip);
        }

        private AudioClip GetLoopClip()
        {
            switch (_generator.State)
            {
                case GENERATOR_STATE.INTERACTING:
                    return _repairLoopClip;

                case GENERATOR_STATE.COMPLETED:
                    return _completedLoopClip;

                default:
                    return _idleLoopClip;
            }
        }

        // clip이 null이면 루프를 멈춘다.
        private void PlayLoop(AudioClip clip)
        {
            if (_loopSource == null || ReferenceEquals(_loopSource.clip, clip))
            {
                return;
            }

            _loopSource.Stop();
            _loopSource.clip = clip;

            if (clip == null)
            {
                return;
            }

            _loopSource.Play();
        }

        private void PlayEvent(AudioClip clip)
        {
            if (_eventSource == null || clip == null)
            {
                return;
            }

            _eventSource.PlayOneShot(clip);
        }
    }
}
