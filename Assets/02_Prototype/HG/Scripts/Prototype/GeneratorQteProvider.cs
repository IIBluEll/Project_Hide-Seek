using System.Collections.Generic;
using HM.CodeBase;
using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// QTE UI 1개를 모든 발전기가 돌려쓰도록 중개하는 프로토타입 검증용 싱글톤.
    ///
    /// 수리를 시작한 발전기에게만 View 소유권을 넘기고, 대상이 바뀌면 이전 Presenter를
    /// 반드시 Dispose해 이벤트 구독을 정리한다. 발전기가 여러 기여도 표시가 섞이지 않는다.
    ///
    /// TODO: 현민 담당 UI Manager가 확정되면 이 클래스를 제거하고 해당 매니저가 같은 역할을 맡는다.
    ///       옮겨야 할 책임은 View 보유, Presenter 수명 관리, 발전기 등록 세 가지다.
    /// TODO: 발전기 이벤트를 EventProvider로 발행할지는 추후 회의에서 결정한다. 전환 지점은 이 클래스다.
    ///
    /// 사용 예시
    /// <code>
    /// // 발전기를 런타임에 활성화하는 시스템에서
    /// GeneratorQteProvider.Instance.Register(generator);
    ///
    /// // 발전기를 비활성화하거나 파괴하기 전에
    /// GeneratorQteProvider.Instance.Unregister(generator);
    /// </code>
    ///
    /// 프로토타입 씬에서는 <see cref="_arr_startupGenerator"/>에 발전기를 넣어두면 Start에서 자동 등록한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorQteProvider : ASingletone<GeneratorQteProvider>
    {
        [Tooltip("모든 발전기가 공유하는 QTE UI. 씬에 배치한 오브젝트에 직접 할당한다.")]
        [SerializeField] private GeneratorQte_view _generatorQteView;

        [Header("프로토타입 전용")]
        [Tooltip("여기에 넣은 발전기는 Start에서 자동으로 등록된다. 정식 구조에서는 Register를 직접 호출한다.")]
        [SerializeField] private Generator[] _arr_startupGenerator;

        private readonly List<IGeneratorQteModel> LIST_MODEL = new();

        private GeneratorQte_presenter _presenter;
        private IGeneratorQteModel _currentModel;

        /// <summary>현재 UI를 점유 중인 발전기. 아무도 수리 중이 아니면 null이다.</summary>
        public IGeneratorQteModel CurrentModel => _currentModel;

        public override void Awake()
        {
            base.Awake();

            if (_generatorQteView == null)
            {
                Debug.LogError($"[{nameof(GeneratorQteProvider)}] View 참조가 비어 있습니다. 씬에 배치한 오브젝트에 직접 할당해야 합니다." , this);
            }
        }

        private void Start()
        {
            if (_generatorQteView != null)
            {
                _generatorQteView.Clear();
                _generatorQteView.Close();
            }

            if (_arr_startupGenerator == null)
            {
                return;
            }

            for (int i = 0; i < _arr_startupGenerator.Length; i++)
            {
                Register(_arr_startupGenerator[i]);
            }
        }

        private void OnDestroy()
        {
            for (int i = LIST_MODEL.Count - 1; i >= 0; i--)
            {
                Unregister(LIST_MODEL[i]);
            }

            ReleasePresenter();
        }

        /// <summary>
        /// 발전기를 UI 중개 대상으로 등록한다. 같은 발전기를 여러 번 등록해도 한 번만 반영된다.
        /// </summary>
        public void Register(IGeneratorQteModel generatorQteModel)
        {
            if (generatorQteModel == null || LIST_MODEL.Contains(generatorQteModel))
            {
                return;
            }

            LIST_MODEL.Add(generatorQteModel);
            generatorQteModel.RepairStarted += OnRepairStartedActioned;
            generatorQteModel.RepairStopped += OnRepairStoppedActioned;
        }

        /// <summary>
        /// 등록을 해제한다. 해당 발전기가 UI를 점유 중이었다면 UI도 함께 닫는다.
        /// 발전기를 파괴하기 전에 반드시 호출한다.
        /// </summary>
        public void Unregister(IGeneratorQteModel generatorQteModel)
        {
            if (generatorQteModel == null || LIST_MODEL.Remove(generatorQteModel) == false)
            {
                return;
            }

            generatorQteModel.RepairStarted -= OnRepairStartedActioned;
            generatorQteModel.RepairStopped -= OnRepairStoppedActioned;

            if (ReferenceEquals(_currentModel , generatorQteModel))
            {
                ReleasePresenter();
            }
        }

        private void OnRepairStartedActioned(IGeneratorQteModel generatorQteModel)
        {
            if (_generatorQteView == null)
            {
                return;
            }

            // 이전 대상이 남아 있으면 여기서 구독까지 정리된다.
            ReleasePresenter();

            _currentModel = generatorQteModel;
            _presenter = new GeneratorQte_presenter(generatorQteModel , _generatorQteView);
            _presenter.Open();
        }

        private void OnRepairStoppedActioned(IGeneratorQteModel generatorQteModel)
        {
            // 다른 발전기가 이미 UI를 넘겨받았다면 뒤늦게 도착한 종료 신호는 무시한다.
            if (ReferenceEquals(_currentModel , generatorQteModel) == false)
            {
                return;
            }

            ReleasePresenter();
        }

        private void ReleasePresenter()
        {
            _currentModel = null;

            if (_presenter == null)
            {
                return;
            }

            _presenter.Close();
            _presenter.Dispose();
            _presenter = null;
        }
    }
}
