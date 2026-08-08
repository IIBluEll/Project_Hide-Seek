using System.Collections.Generic;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// UI 1벌을 모든 발전기가 돌려쓰도록 중개하는 프로토타입 검증용 싱글톤.
    /// 수리를 시작한 발전기에게만 UI 소유권을 넘기고, 대상이 바뀌면 이전 Presenter를 Dispose해 구독을 정리한다.
    ///
    /// Presenter끼리는 서로를 참조하지 않는다. 열고 닫는 조율은 이 클래스만 한다. AGENTS.md 3.3
    ///
    /// Config와 QTE 입력을 등록된 모든 발전기에 똑같이 넣어준다. 발전기별 오버라이드는 아직 없다.
    ///
    /// 발전기를 직접 참조하지 않고 <see cref="Generator.Enabled"/>, <see cref="Generator.Disabled"/>를 구독해
    /// 등록한다. 그래서 01_Main의 발전기 코드는 프로토타입인 이 클래스를 모른다.
    /// <see cref="ASingletone{T}.Instance"/>는 인스턴스가 없으면 GameObject를 새로 만들므로 쓰지 않는다.
    ///
    /// QTE 입력은 진우 담당 <see cref="PlayerQTEInputSource"/>를 인스펙터로 주입받는다. MonoBehaviour라
    /// 코드로 만들 수 없어 씬 인스턴스에서만 할당된다. 프리팹은 씬 오브젝트를 참조할 수 없다.
    ///
    /// TODO: 현민 담당 UI Manager가 확정되면 이 클래스를 제거하고 해당 매니저가 같은 역할을 맡는다.
    ///       옮겨야 할 책임은 View 보유, Presenter 수명 관리, 발전기 등록 세 가지다.
    /// TODO: Config 배포는 UI 책임이 아니다. 후보 지점 활성화 시스템(GDD 7.1)이 생기면 그쪽으로 옮긴다.
    /// TODO: 발전기 이벤트를 EventProvider로 발행할지는 추후 회의에서 결정한다. 전환 지점은 이 클래스다.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public sealed class GeneratorQteProvider : ASingletone<GeneratorQteProvider>
    {
        [Tooltip("등록된 모든 발전기가 이 설정을 공유한다. 난이도 선택이 생기면 그쪽에서 지정한다.")]
        [SerializeField] private GeneratorConfig _generatorConfig;

        [Header("View")]
        [Tooltip("모든 발전기가 공유한다. 씬에 배치한 오브젝트에 직접 할당한다.")]
        [SerializeField] private GeneratorProgress_view _generatorProgressView;
        [SerializeField] private GeneratorQte_view _generatorQteView;

        [Header("QTE 입력")]
        [Tooltip("현재 동작하지 않는다. 실제 키는 PlayerQTEInputSource가 구독하는 Jump 액션이고 라벨도 그쪽이 고정 반환한다.")]
        [SerializeField] private Key _qteKey = Key.Space;

        [Tooltip("씬에 배치한 플레이어의 컴포넌트를 직접 할당한다. 프리팹 에셋에서는 비워둘 수밖에 없다.")]
        [SerializeField] private PlayerQTEInputSource _qteInputSource;

        private readonly List<Generator> LIST_GENERATOR = new();

        private GeneratorProgress_presenter _progressPresenter;
        private GeneratorQte_presenter _qtePresenter;
        private Generator _currentGenerator;

        public Generator CurrentGenerator => _currentGenerator; // 아무도 수리 중이 아니면 null

        public override void Awake()
        {
            base.Awake();

            Generator.Enabled += OnGeneratorEnabledActioned;
            Generator.Disabled += OnGeneratorDisabledActioned;

            // 이 Provider보다 먼저 활성화된 발전기는 이벤트를 놓쳤으므로 여기서 훑는다.
            Generator[] tArr_generator = FindObjectsByType<Generator>(FindObjectsSortMode.None);
            for (int i = 0; i < tArr_generator.Length; i++)
            {
                Register(tArr_generator[i]);
            }

            if (_generatorProgressView == null || _generatorQteView == null)
            {
                Debug.LogError($"[{nameof(GeneratorQteProvider)}] View 참조가 비어 있습니다. 씬에 배치한 오브젝트에 직접 할당해야 합니다." , this);
            }

            if (_generatorConfig == null)
            {
                Debug.LogError($"[{nameof(GeneratorQteProvider)}] GeneratorConfig가 비어 있어 발전기가 동작하지 않습니다." , this);
            }
        }

        private void Start()
        {
            if (_generatorProgressView != null)
            {
                _generatorProgressView.Clear();
                _generatorProgressView.Close();
            }

            if (_generatorQteView != null)
            {
                _generatorQteView.Clear();
                _generatorQteView.Close();
            }
        }

        private void OnDestroy()
        {
            // 먼저 끊어야 남은 발전기의 OnDisable이 이 인스턴스를 다시 건드리지 않는다.
            Generator.Enabled -= OnGeneratorEnabledActioned;
            Generator.Disabled -= OnGeneratorDisabledActioned;

            for (int i = LIST_GENERATOR.Count - 1; i >= 0; i--)
            {
                Unregister(LIST_GENERATOR[i]);
            }

            ReleasePresenters();
        }

        private void OnGeneratorEnabledActioned(Generator generator)
        {
            Register(generator);
        }

        private void OnGeneratorDisabledActioned(Generator generator)
        {
            Unregister(generator);
        }

        private void Register(Generator generator)
        {
            if (generator == null || LIST_GENERATOR.Contains(generator))
            {
                return;
            }

            LIST_GENERATOR.Add(generator);
            generator.SetConfig(_generatorConfig);
            generator.SetQteInputSource(_qteInputSource);
            generator.RepairStarted += OnRepairStartedActioned;
            generator.RepairStopped += OnRepairStoppedActioned;
        }

        // 해당 발전기가 UI를 점유 중이었다면 UI도 함께 닫는다.
        private void Unregister(Generator generator)
        {
            if (generator == null || LIST_GENERATOR.Remove(generator) == false)
            {
                return;
            }

            generator.RepairStarted -= OnRepairStartedActioned;
            generator.RepairStopped -= OnRepairStoppedActioned;

            if (ReferenceEquals(_currentGenerator , generator))
            {
                ReleasePresenters();
            }
        }

        private void OnRepairStartedActioned(Generator generator)
        {
            if (_generatorProgressView == null || _generatorQteView == null)
            {
                return;
            }

            // 이전 대상이 남아 있으면 여기서 구독까지 정리된다.
            ReleasePresenters();

            _currentGenerator = generator;

            _progressPresenter = new GeneratorProgress_presenter(generator , _generatorProgressView);
            _qtePresenter = new GeneratorQte_presenter(generator , _generatorQteView);

            _progressPresenter.Open();
            _qtePresenter.Open();
        }

        private void OnRepairStoppedActioned(Generator generator)
        {
            // 다른 발전기가 이미 UI를 넘겨받았다면 뒤늦게 도착한 종료 신호는 무시한다.
            if (ReferenceEquals(_currentGenerator , generator) == false)
            {
                return;
            }

            ReleasePresenters();
        }

        private void ReleasePresenters()
        {
            _currentGenerator = null;

            if (_progressPresenter != null)
            {
                _progressPresenter.Close();
                _progressPresenter.Dispose();
                _progressPresenter = null;
            }

            if (_qtePresenter != null)
            {
                _qtePresenter.Close();
                _qtePresenter.Dispose();
                _qtePresenter = null;
            }
        }
    }
}
