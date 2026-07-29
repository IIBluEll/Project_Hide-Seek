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
    /// TODO: 현민 담당 UI Manager가 확정되면 이 클래스를 제거하고 해당 매니저가 같은 역할을 맡는다.
    ///       옮겨야 할 책임은 View 보유, Presenter 수명 관리, 발전기 등록 세 가지다.
    /// TODO: Config 배포는 UI 책임이 아니다. 후보 지점 중 일부를 활성화하는 시스템(GDD 7.1)이 생기면
    ///       그쪽으로 옮긴다. UI Manager로 따라가면 안 된다.
    /// TODO: QTE 키는 진우 담당 InputActions가 확정되면 KeyboardInputSource 대신 그 구현을 주입한다.
    ///       키 값 자체는 현민 담당 키 설정 시스템으로 옮긴다. GDD 13.2
    /// TODO: 발전기 이벤트를 EventProvider로 발행할지는 추후 회의에서 결정한다. 전환 지점은 이 클래스다.
    ///
    /// 등록은 반드시 아래 정적 메서드로 한다. <see cref="ASingletone{T}.Instance"/>는 인스턴스가 없으면
    /// GameObject를 새로 만들기 때문에, 종료 중에 호출하면 파괴된 Provider가 되살아난다.
    /// <code>
    /// GeneratorQteProvider.RegisterGenerator(generator);
    /// GeneratorQteProvider.UnregisterGenerator(generator);
    /// </code>
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
        [Tooltip("모든 발전기가 같은 키를 쓴다.")]
        [SerializeField] private Key _qteKey = Key.Space;

        // Instance를 거치지 않고 등록을 처리하기 위한 자체 참조. 파괴 시 스스로 비운다.
        private static GeneratorQteProvider s_provider;

        private readonly List<Generator> LIST_GENERATOR = new();

        private IInputSource _qteInputSource; // 상태가 없어 모든 발전기가 하나를 공유해도 된다

        private GeneratorProgress_presenter _progressPresenter;
        private GeneratorQte_presenter _qtePresenter;
        private Generator _currentGenerator;

        public Generator CurrentGenerator => _currentGenerator; // 아무도 수리 중이 아니면 null

        public override void Awake()
        {
            base.Awake();

            s_provider = this;

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
            // 먼저 비워야 남은 발전기의 OnDisable이 이 인스턴스를 다시 건드리지 않는다.
            if (ReferenceEquals(s_provider , this))
            {
                s_provider = null;
            }

            for (int i = LIST_GENERATOR.Count - 1; i >= 0; i--)
            {
                Unregister(LIST_GENERATOR[i]);
            }

            ReleasePresenters();
        }

        /// <summary>
        /// 발전기가 활성화될 때 호출한다. Provider가 없으면 경고만 남기고 넘어간다.
        /// </summary>
        public static void RegisterGenerator(Generator generator)
        {
            if (s_provider == null)
            {
                Debug.LogWarning($"[{nameof(GeneratorQteProvider)}] 씬에 Provider가 없어 발전기를 등록하지 못했습니다." , generator);
                return;
            }

            s_provider.Register(generator);
        }

        /// <summary>
        /// 발전기가 비활성화될 때 호출한다. 종료 중이라 Provider가 이미 사라졌으면 조용히 넘어간다.
        /// </summary>
        public static void UnregisterGenerator(Generator generator)
        {
            if (s_provider == null)
            {
                return;
            }

            s_provider.Unregister(generator);
        }

        private void Register(Generator generator)
        {
            if (generator == null || LIST_GENERATOR.Contains(generator))
            {
                return;
            }

            // Awake 순서가 보장되지 않아 다른 오브젝트가 먼저 Register를 부를 수 있다.
            _qteInputSource ??= new KeyboardInputSource(_qteKey);

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
