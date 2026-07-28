using HM.CodeBase;

namespace HideSeek.Generators
{
    /// <summary>
    /// 수리가 시작돼도 게이지는 닫아둔 채로, QTE가 실제로 발생할 때만 View를 연다.
    /// 닫는 시점은 결과 피드백이 끝난 뒤라서 View가 스스로 판단한다.
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

            GENERATOR_QTE_VIEW.Clear();
            GENERATOR_QTE_VIEW.Close();
        }

        public override void Close()
        {
            // 닫힌 뒤에도 구독이 남으면 다른 발전기의 QTE가 이 View에 그려진다.
            Unbind();

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

            GENERATOR_QTE_MODEL.QteStarted -= OnQteStartedActioned;
            GENERATOR_QTE_MODEL.QteIndicatorChanged -= OnQteIndicatorChangedActioned;
            GENERATOR_QTE_MODEL.QteFinished -= OnQteFinishedActioned;
            _isBound = false;
        }

        private void OnQteStartedActioned(QteChallenge challenge)
        {
            GENERATOR_QTE_VIEW.Open();
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
            GENERATOR_QTE_VIEW.PlayResultFeedback(result);
        }
    }
}
