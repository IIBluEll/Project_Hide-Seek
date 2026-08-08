using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// UI 1벌을 모든 발전기가 돌려쓰도록 중개하는 프로토타입 검증용 컴포넌트.
    /// 수리를 시작한 발전기에게만 UI 소유권을 넘기고, 대상이 바뀌면 이전 Presenter를 Dispose해 구독을 정리한다.
    ///
    /// Presenter끼리는 서로를 참조하지 않는다. 열고 닫는 조율은 이 클래스만 한다. AGENTS.md 3.3
    ///
    /// QTE 입력을 등록된 모든 발전기에 똑같이 넣어준다. 발전기별 오버라이드는 아직 없다.
    /// Config 배포는 UI 책임이 아니라서 <see cref="GeneratorProvider"/>로 옮겼다.
    ///
    /// 발전기를 직접 참조하지 않고 <see cref="Generator.Enabled"/>, <see cref="Generator.Disabled"/>를 구독해
    /// 등록한다. 그래서 01_Main의 발전기 코드는 프로토타입인 이 클래스를 모른다.
    /// 싱글톤이 아니다. 게임 1판 동안만 의미 있는 상태를 들고 있어 씬과 수명을 같이해야 한다.
    ///
    /// QTE 입력은 진우 담당 <see cref="PlayerQTEInputSource"/>를 인스펙터로 주입받는다. MonoBehaviour라
    /// 코드로 만들 수 없어 씬 인스턴스에서만 할당된다. 프리팹은 씬 오브젝트를 참조할 수 없다.
    ///
    /// TODO: 현민 담당 UI Manager가 확정되면 이 클래스를 제거하고 해당 매니저가 같은 역할을 맡는다.
    ///       옮겨야 할 책임은 View 보유, Presenter 수명 관리, 발전기 등록 세 가지다.
    /// TODO: 발전기 이벤트를 EventProvider로 발행할지는 추후 회의에서 결정한다. 전환 지점은 이 클래스다.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public sealed class GeneratorQteProvider : MonoBehaviour
    {
        [Header("View")]
        [Tooltip("모든 발전기가 공유한다. 씬에 배치한 오브젝트에 직접 할당한다.")]
        [SerializeField] private GeneratorProgress_view _generatorProgressView;
        [SerializeField] private GeneratorQte_view _generatorQteView;

        [Header("QTE 입력")]
        [Tooltip("아래 PlayerQTEInputSource가 비어 있을 때만 쓰는 폴백 키다. 플레이어가 없는 씬에서 QTE를 검증하기 위한 것이다.")]
        [SerializeField] private Key _qteKey = Key.Space;

        [Tooltip("씬에 배치한 플레이어의 컴포넌트를 직접 할당한다. 프리팹 에셋에서는 비워둘 수밖에 없다.")]
        [SerializeField] private PlayerQTEInputSource _qteInputSource;

        private readonly GeneratorRegistry REGISTRY = new();

        private GeneratorProgress_presenter _progressPresenter;
        private GeneratorQte_presenter _qtePresenter;
        private Generator _currentGenerator;

        public Generator CurrentGenerator => _currentGenerator; // 아무도 수리 중이 아니면 null

        private void Awake()
        {
            REGISTRY.Attach(OnGeneratorRegisteredActioned , OnGeneratorUnregisteredActioned);

            if (_generatorProgressView == null || _generatorQteView == null)
            {
                Debug.LogError($"[{nameof(GeneratorQteProvider)}] View 참조가 비어 있습니다. 씬에 배치한 오브젝트에 직접 할당해야 합니다." , this);
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
            REGISTRY.Detach();

            ReleasePresenters();
        }

        private void OnGeneratorRegisteredActioned(Generator generator)
        {
            generator.SetQteInputSource(_qteInputSource != null ? _qteInputSource : new KeyboardInputSource(_qteKey));
            generator.RepairStarted += OnRepairStartedActioned;
            generator.RepairStopped += OnRepairStoppedActioned;
        }

        // 해당 발전기가 UI를 점유 중이었다면 UI도 함께 닫는다.
        private void OnGeneratorUnregisteredActioned(Generator generator)
        {
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
