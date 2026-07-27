using HM.CodeBase;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기의 진행도와 QTE 이벤트를 View에 표시한다.
    /// Model은 <see cref="IGeneratorQteModel"/>로 주입받으며, 실제 구현은 <see cref="Generator"/>다.
    ///
    /// 한 프레젠터는 발전기 한 기의 한 번의 수리 구간만 담당한다.
    /// 대상이 바뀔 때는 소유자가 <see cref="Dispose"/> 후 새로 생성한다.
    /// </summary>
    public sealed class GeneratorQte_presenter : APresenter
    {
        private readonly IGeneratorQteModel GENERATOR_QTE_MODEL;
        private readonly GeneratorQte_view GENERATOR_QTE_VIEW;

        private bool _isBound;

        public GeneratorQte_presenter(
            IGeneratorQteModel generatorQteModel ,
            GeneratorQte_view generatorQteView)
        {
            GENERATOR_QTE_MODEL = generatorQteModel;
            GENERATOR_QTE_VIEW = generatorQteView;
        }

        public override void Open()
        {
            Bind();

            // View는 여러 발전기가 공유하므로 넘겨받을 때마다 이전 표시 상태를 지운다.
            GENERATOR_QTE_VIEW.Clear();
            GENERATOR_QTE_VIEW.SetProgress(GENERATOR_QTE_MODEL.Progress01);
            GENERATOR_QTE_VIEW.Open();
        }

        public override void Close()
        {
            // 닫힌 뒤에도 구독이 남아 있으면 중단된 발전기의 진행도 감소가 계속 View에 기록된다.
            Unbind();

            GENERATOR_QTE_VIEW.HideQte();
            GENERATOR_QTE_VIEW.Close();
        }

        public override void Dispose()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_isBound)
            {
                return;
            }

            GENERATOR_QTE_MODEL.ProgressChanged += OnProgressChangedActioned;
            GENERATOR_QTE_MODEL.QteStarted += OnQteStartedActioned;
            GENERATOR_QTE_MODEL.QteIndicatorChanged += OnQteIndicatorChangedActioned;
            GENERATOR_QTE_MODEL.QteFinished += OnQteFinishedActioned;
            _isBound = true;
        }

        private void Unbind()
        {
            if (_isBound == false)
            {
                return;
            }

            GENERATOR_QTE_MODEL.ProgressChanged -= OnProgressChangedActioned;
            GENERATOR_QTE_MODEL.QteStarted -= OnQteStartedActioned;
            GENERATOR_QTE_MODEL.QteIndicatorChanged -= OnQteIndicatorChangedActioned;
            GENERATOR_QTE_MODEL.QteFinished -= OnQteFinishedActioned;
            _isBound = false;
        }

        private void OnProgressChangedActioned(float progress01)
        {
            GENERATOR_QTE_VIEW.SetProgress(progress01);
        }

        private void OnQteStartedActioned(QteChallenge challenge)
        {
            GENERATOR_QTE_VIEW.ShowQte(
                challenge.ZONE_START_01 ,
                challenge.ZONE_END_01 ,
                challenge.KEY_LABEL);
        }

        private void OnQteIndicatorChangedActioned(float indicator01)
        {
            GENERATOR_QTE_VIEW.SetIndicator(indicator01);
        }

        private void OnQteFinishedActioned(QTE_RESULT result)
        {
            // 게이지를 즉시 숨기지 않고 View가 결과 색을 표시한 뒤 닫는다. GDD 7.4
            GENERATOR_QTE_VIEW.PlayResultFeedback(result);
        }
    }
}
