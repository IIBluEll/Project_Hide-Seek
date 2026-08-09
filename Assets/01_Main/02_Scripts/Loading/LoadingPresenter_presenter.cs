using HM.CodeBase;

namespace HideSeek.Loading
{
    public sealed class LoadingPresenter_presenter : APresenter
    {
        private readonly LoadingModel_model LOADING_MODEL;
        private readonly LoadingScreen_view LOADING_VIEW;

        public LoadingPresenter_presenter(
            LoadingModel_model loadingModel ,
            LoadingScreen_view loadingView)
        {
            LOADING_MODEL = loadingModel;
            LOADING_VIEW = loadingView;
        }

        public override void Open()
        {
            LOADING_VIEW.Clear();
            LOADING_VIEW.SetDifficulty(LOADING_MODEL.Difficulty);
            LOADING_VIEW.SetStatus(LOADING_MODEL.Status);
            LOADING_VIEW.SetProgress(LOADING_MODEL.Progress01);
            LOADING_VIEW.Open();
        }

        public override void Close()
        {
            LOADING_VIEW.Close();
        }

        public override void Dispose()
        {
        }

        public void SetProgress(float progress01)
        {
            LOADING_MODEL.SetProgress(progress01);
            LOADING_VIEW.SetProgress(LOADING_MODEL.Progress01);
        }

        public void SetStatus(string status)
        {
            LOADING_MODEL.SetStatus(status);
            LOADING_VIEW.SetStatus(LOADING_MODEL.Status);
        }
    }
}
