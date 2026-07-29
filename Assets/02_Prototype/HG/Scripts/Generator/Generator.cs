using System;
using UnityEngine;

namespace HideSeek.Generators
{
    public enum GENERATOR_STATE
    {
        INACTIVE,
        INTERACTING,
        COMPLETED
    }

    /// <summary>
    /// 발전기 1기의 수리 진행도와 QTE를 관리한다.
    /// GAME_DESIGN_DOCUMENT.md 7장을 기준으로 구현했으며, MVP 패턴은 UI에만 적용하므로 이 클래스는 게임플레이 로직만 가진다.
    ///
    /// 다른 담당 영역과의 연결은 아래 이벤트로만 노출한다.
    /// - 상호작용(진우): <see cref="TryBeginRepair"/>, <see cref="CancelRepair"/>를 호출한다.
    /// - 소음(현민): <see cref="RepairNoiseOccurred"/>, <see cref="QteFailureNoiseOccurred"/>를 구독해 소음 이벤트로 변환한다.
    /// - Anger(현민): <see cref="Completed"/>를 구독해 발전기 완료 수에 맞는 Anger 하한선을 적용한다.
    /// - UI: 읽기 전용 인터페이스 두 개를 구현해 표시용 값만 넘긴다. 공개 이벤트에는 UI 타입을 쓰지 않는다.
    ///
    /// 설정 데이터는 인스펙터에 두지 않는다. 난이도 하나가 모든 발전기에 같은 값을 주므로
    /// 발전기를 등록하는 쪽이 <see cref="SetConfig"/>로 넣어준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Generator : MonoBehaviour , IGeneratorProgressModel , IGeneratorQteModel
    {
        // 수리 관련 이벤트
        public event Action<Generator> RepairStarted; // 인자는 이 발전기
        public event Action<Generator> RepairStopped; // 중단과 완료 모두 발생. 인자는 이 발전기
        public event Action<float> ProgressChanged; // 인자는 진행도(0~1)
        public event Action Completed;

        // QTE 관련 이벤트
        public event Action<QteChallenge> QteStarted;
        public event Action<float> QteIndicatorChanged; // 인자는 인디케이터 위치(0~1)
        public event Action<QTE_RESULT> QteFinished;
        public event Action<Vector3> RepairNoiseOccurred; // 수리 중 주기적으로 발생. 인자는 발생 위치
        public event Action<Vector3> QteFailureNoiseOccurred; // 인자는 발생 위치

        public GENERATOR_STATE State { get; private set; } = GENERATOR_STATE.INACTIVE;
        public float Progress01 { get; private set; } // 0~1

        private GeneratorConfig _config;
        private IInputSource _qteInputSource;
        private QteRunner _qteRunner;
        private float _stopElapsed;
        private float _nextQteDelay;
        private float _failureStunRemain;
        private float _repairNoiseTimer;

        private void Awake()
        {
            _qteRunner = new QteRunner();
            _qteRunner.Finished += OnQteFinishedActioned;
        }

        private void OnEnable()
        {
            GeneratorQteProvider.RegisterGenerator(this);
        }

        private void OnDisable()
        {
            // 수리 중에 꺼지면 State가 INTERACTING으로 굳어 다시 켜도 시작할 수 없다.
            CancelRepair();

            GeneratorQteProvider.UnregisterGenerator(this);
        }

        private void OnDestroy()
        {
            if (_qteRunner != null)
                _qteRunner.Finished -= OnQteFinishedActioned;
        }

        private void Update()
        {
            if (_config == null)
            {
                return;
            }

            float tDeltaTime = Time.deltaTime;

            switch (State)
            {
                case GENERATOR_STATE.INTERACTING:
                    TickRepair(tDeltaTime);
                    break;

                case GENERATOR_STATE.INACTIVE:
                    TickDecay(tDeltaTime);
                    break;
            }
        }

        /// <summary>
        /// 발전기를 등록하는 쪽이 호출한다. 난이도가 정한 값 하나를 모든 발전기가 공유한다.
        /// </summary>
        public void SetConfig(GeneratorConfig config)
        {
            _config = config;
        }

        // 플레이어 입력 구조가 확정되면 구현만 바꿔서 여기로 넣는다.
        public void SetQteInputSource(IInputSource qteInputSource)
        {
            if (qteInputSource == null)
            {
                return;
            }

            _qteInputSource = qteInputSource;
        }

        /// <summary>
        /// 상호작용 시스템이 호출한다. 이미 작업 중이거나 완료된 발전기면 false를 반환한다.
        /// </summary>
        public bool TryBeginRepair()
        {
            if (State != GENERATOR_STATE.INACTIVE)
            {
                return false;
            }

            if (_config == null || _qteInputSource == null)
            {
                Debug.LogError($"[{nameof(Generator)}] Config 또는 입력 소스가 없어 수리를 시작할 수 없습니다. 등록할 때 SetConfig와 SetQteInputSource를 모두 호출해야 합니다." , this);
                return false;
            }

            State = GENERATOR_STATE.INTERACTING;
            _stopElapsed = 0f;
            _failureStunRemain = 0f;
            _repairNoiseTimer = 0f;
            ScheduleNextQte();

            RepairStarted?.Invoke(this);
            return true;
        }

        // 남은 진행도는 유예 시간 뒤부터 감소한다. GDD 7.3.6, 7.3.7
        public void CancelRepair()
        {
            if (State != GENERATOR_STATE.INTERACTING)
            {
                return;
            }

            State = GENERATOR_STATE.INACTIVE;
            _stopElapsed = 0f;
            _failureStunRemain = 0f;
            _qteRunner.Cancel();

            RepairStopped?.Invoke(this);
        }

        private void TickRepair(float deltaTime)
        {
            TickRepairNoise(deltaTime);

            if (_qteRunner.IsActive)
            {
                // Tick 안에서 판정이 끝나면 OnQteFinishedActioned가 먼저 실행된다.
                _qteRunner.Tick(deltaTime , _qteInputSource.IsQteKeyDown());

                if (_qteRunner.IsActive)
                {
                    QteIndicatorChanged?.Invoke(_qteRunner.Indicator01);
                }
            }

            if (_failureStunRemain > 0f)
            {
                _failureStunRemain -= deltaTime;
                return;
            }

            AddProgress(deltaTime / _config.RepairDuration);

            if (State != GENERATOR_STATE.INTERACTING || _qteRunner.IsActive)
            {
                return;
            }

            _nextQteDelay -= deltaTime;
            if (_nextQteDelay <= 0f)
            {
                BeginQte();
            }
        }

        private void TickRepairNoise(float deltaTime)
        {
            _repairNoiseTimer -= deltaTime;
            if (_repairNoiseTimer > 0f)
            {
                return;
            }

            _repairNoiseTimer = _config.RepairNoiseInterval;
            RepairNoiseOccurred?.Invoke(transform.position);
        }

        private void TickDecay(float deltaTime)
        {
            if (Progress01 <= 0f)
            {
                return;
            }

            _stopElapsed += deltaTime;
            if (_stopElapsed < _config.DecayGraceDuration)
            {
                return;
            }

            AddProgress(-_config.DecayRatePerSecond * deltaTime);
        }

        private void BeginQte()
        {
            _qteRunner.Begin(_config.QteSweepDuration, _config.QteSuccessZoneSize01, _config.QteZoneMinStart01);
            QteStarted?.Invoke(new QteChallenge(_qteRunner.ZoneStart01, _qteRunner.ZoneEnd01));
            QteIndicatorChanged?.Invoke(_qteRunner.Indicator01);
        }

        private void ScheduleNextQte()
        {
            _nextQteDelay = UnityEngine.Random.Range(
                _config.MinQteInterval,
                _config.GetEffectiveMaxQteInterval());
        }

        private void AddProgress(float delta)
        {
            float tPrevProgress = Progress01;
            Progress01 = Mathf.Clamp01(Progress01 + delta);

            if (Mathf.Approximately(tPrevProgress , Progress01) == false)
            {
                ProgressChanged?.Invoke(Progress01);
            }

            if (Progress01 >= 1f && State == GENERATOR_STATE.INTERACTING)
            {
                Complete();
            }
        }

        private void Complete()
        {
            State = GENERATOR_STATE.COMPLETED;
            Progress01 = 1f;
            _failureStunRemain = 0f;
            _qteRunner.Cancel();

            RepairStopped?.Invoke(this);

            // 완료 소음 적용 여부는 GDD 19에서 미정이므로 여기서는 소음을 발행하지 않는다.
            Completed?.Invoke();
        }

        private void OnQteFinishedActioned(QTE_RESULT result)
        {
            QteFinished?.Invoke(result);

            if (result == QTE_RESULT.FAILURE)
            {
                _failureStunRemain = _config.QteFailureStunDuration;
                AddProgress(-_config.QteFailurePenalty01);
                QteFailureNoiseOccurred?.Invoke(transform.position);
            }

            ScheduleNextQte();
        }
    }
}
