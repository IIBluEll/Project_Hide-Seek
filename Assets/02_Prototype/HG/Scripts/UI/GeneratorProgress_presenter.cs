using HM.CodeBase;

namespace HideSeek.Generators
{
    public sealed class GeneratorProgress_presenter : APresenter
    {
        private readonly IGeneratorProgressModel GENERATOR_PROGRESS_MODEL;
        private readonly GeneratorProgress_view GENERATOR_PROGRESS_VIEW;

        private bool _isBound;

        public GeneratorProgress_presenter(
            IGeneratorProgressModel generatorProgressModel ,
            GeneratorProgress_view generatorProgressView)
        {
            GENERATOR_PROGRESS_MODEL = generatorProgressModel;
            GENERATOR_PROGRESS_VIEW = generatorProgressView;
        }

        public override void Open()
        {
            Bind();

            GENERATOR_PROGRESS_VIEW.Clear();

            // 이벤트는 변화만 알린다. 중단했다 재개한 발전기의 남은 진행도는 여기서 직접 읽어야 한다.
            GENERATOR_PROGRESS_VIEW.SetProgress(GENERATOR_PROGRESS_MODEL.Progress01);
            GENERATOR_PROGRESS_VIEW.Open();
        }

        public override void Close()
        {
            // 닫힌 뒤에도 구독이 남으면 중단된 발전기의 진행도 감소가 계속 View에 기록된다.
            Unbind();

            GENERATOR_PROGRESS_VIEW.Close();
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

            GENERATOR_PROGRESS_MODEL.ProgressChanged += OnProgressChangedActioned;
            _isBound = true;
        }

        private void Unbind()
        {
            if (_isBound == false)
            {
                return;
            }

            GENERATOR_PROGRESS_MODEL.ProgressChanged -= OnProgressChangedActioned;
            _isBound = false;
        }

        private void OnProgressChangedActioned(float progress01)
        {
            GENERATOR_PROGRESS_VIEW.SetProgress(progress01);
        }
    }
}
