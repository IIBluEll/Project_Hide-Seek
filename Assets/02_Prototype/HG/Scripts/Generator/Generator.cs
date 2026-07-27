using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 1기의 수리 진행도와 QTE를 관리한다.
    /// GAME_DESIGN_DOCUMENT.md 7장을 기준으로 구현했으며, MVP 패턴은 UI에만 적용하므로 이 클래스는 게임플레이 로직만 가진다.
    ///
    /// 다른 담당 영역과의 연결은 아래 이벤트로만 노출한다.
    /// - 상호작용(진우): <see cref="TryBeginRepair"/>, <see cref="CancelRepair"/>를 호출한다.
    /// - 소음(현민): <see cref="RepairNoiseOccurred"/>, <see cref="QteFailureNoiseOccurred"/>를 구독해 소음 이벤트로 변환한다.
    /// - Anger(현민): <see cref="Completed"/>를 구독해 발전기 완료 수에 맞는 Anger 하한선을 적용한다.
    /// - UI: <see cref="IGeneratorQteModel"/>을 구현해 표시용 데이터만 읽기 전용으로 제공한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Generator : MonoBehaviour , IGeneratorQteModel
    {
        [SerializeField] private GeneratorConfig _config;

        [Header("임시 입력 설정")]
        [Tooltip("플레이어 입력 담당자의 InputActions가 확정되면 SetQteInputSource로 교체한다.")]
        [SerializeField] private Key _qteKey = Key.Space;
        [SerializeField] private string _qteKeyLabel = "SPACE";

        // 수리 관련 이벤트
        public event Action<IGeneratorQteModel> RepairStarted; // 수리를 시작했다. 인자는 이 발전기
        public event Action<IGeneratorQteModel> RepairStopped; // 수리가 중단되거나 완료됐다. 인자는 이 발전기
        public event Action<float> ProgressChanged; // 수리 진행도(0~1)가 변경됐다.
        public event Action Completed; // 수리가 완료됐다.

        // QTE 입력 관련 이벤트
        public event Action<QteChallenge> QteStarted; // QTE가 시작됐다.
        public event Action<float> QteIndicatorChanged; // QTE 인디케이터 위치(0~1)가 갱신됐다.
        public event Action<QTE_RESULT> QteFinished; // QTE 판정이 끝났다.
        public event Action<Vector3> RepairNoiseOccurred; // 수리 중 주기적으로 발생하는 발전기 소음. 인자는 발생 위치
        public event Action<Vector3> QteFailureNoiseOccurred; // QTE 실패 소음. 인자는 발생 위치

        public GENERATOR_STATE State { get; private set; } = GENERATOR_STATE.INACTIVE;

        /// <summary>현재 수리 진행도. 0에서 1 사이다.</summary>
        public float Progress01 { get; private set; }

        private QteRunner _qteRunner;
        private IQteInputSource _qteInputSource;
        private float _stopElapsed;
        private float _nextQteDelay;
        private float _failureStunRemain;
        private float _repairNoiseTimer;

        private void Awake()
        {
            _qteRunner = new QteRunner();
            _qteRunner.Finished += OnQteFinishedActioned;

            _qteInputSource = new KeyboardQteInputSource(_qteKey , _qteKeyLabel);

            if (_config == null)
            {
                Debug.LogError($"[{nameof(Generator)}] GeneratorConfig가 비어 있어 비활성화합니다." , this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_qteRunner != null)
                _qteRunner.Finished -= OnQteFinishedActioned;
        }

        private void Update()
        {
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
        /// QTE 입력 경로를 교체한다. 플레이어 입력 구조가 확정되면 이 메서드로 주입한다.
        /// </summary>
        public void SetQteInputSource(IQteInputSource qteInputSource)
        {
            if (qteInputSource == null)
            {
                return;
            }

            _qteInputSource = qteInputSource;
        }

        /// <summary>
        /// 수리를 시작한다. 이미 작업 중이거나 완료된 발전기면 false를 반환한다.
        /// 상호작용 시스템이 호출한다.
        /// </summary>
        public bool TryBeginRepair()
        {
            if (State != GENERATOR_STATE.INACTIVE)
            {
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

        /// <summary>
        /// 수리를 중단한다. 남은 진행도는 유예 시간 뒤부터 감소한다. GDD 7.3.6, 7.3.7
        /// </summary>
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
            _qteRunner.Begin(
                _config.QteSweepDuration ,
                _config.QteSuccessZoneSize01 ,
                _config.QteZoneMinStart01);

            QteStarted?.Invoke(new QteChallenge(
                _qteRunner.ZoneStart01 ,
                _qteRunner.ZoneEnd01 ,
                _qteInputSource.GetQteKeyLabel()));

            QteIndicatorChanged?.Invoke(_qteRunner.Indicator01);
        }

        private void ScheduleNextQte()
        {
            _nextQteDelay = UnityEngine.Random.Range(
                _config.MinQteInterval ,
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
