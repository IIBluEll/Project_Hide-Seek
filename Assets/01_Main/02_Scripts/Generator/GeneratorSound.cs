using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 이벤트를 소리로 옮긴다. <see cref="Generator"/>는 오디오를 모르며 이 컴포넌트가 구독만 한다.
    ///
    /// AudioSource를 둘로 나눈 이유는 감쇠 거리와 볼륨을 따로 주기 위해서다.
    /// 기계음은 가까이서만 들리고 QTE 실패음은 더 멀리 퍼져야 한다. GDD 6.2
    ///
    /// Spatial Blend는 Awake에서 3D로 맞춘다. 인스펙터에 맡겼더니 EventSource가 2D로 남아
    /// QTE 판정음과 완료음이 맵 어디서나 같은 크기로 들렸다.
    ///
    /// 인스펙터: Output에 SFX 믹서 그룹, Volume Rolloff는 Linear나 Custom.
    /// Logarithmic은 Max Distance를 넘어도 볼륨이 0이 되지 않아 먼 발전기까지 재생 대상으로 남는다.
    ///
    /// TODO: 볼륨 설정 훅은 UI 담당 설정 시스템과 인터페이스를 합의한 뒤 연결한다. 회의 안건 B-1
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

            if (_generator == null)
            {
                Debug.LogError($"[{nameof(GeneratorSound)}] Generator 참조가 비어 있습니다.", this);
                return;
            }

            if (_loopSource != null)
            {
                _loopSource.loop = true;
                _loopSource.playOnAwake = false;
                _loopSource.spatialBlend = 1f;
            }

            if (_eventSource != null)
            {
                _eventSource.playOnAwake = false;
                _eventSource.spatialBlend = 1f;
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
