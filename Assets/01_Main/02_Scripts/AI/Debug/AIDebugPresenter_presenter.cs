using System;
using HM.CodeBase;

namespace HideSeek.AI
{
    public sealed class AIDebugPresenter_presenter : APresenter
    {
        private readonly AIDebugModel_model AI_DEBUG_MODEL;
        private readonly AIDebugPanel_view AI_DEBUG_VIEW;
        private readonly float REFRESH_INTERVAL;

        private float _refreshTimer;

        public AIDebugPresenter_presenter(
            AIDebugModel_model aiDebugModel ,
            AIDebugPanel_view aiDebugView ,
            float refreshInterval)
        {
            AI_DEBUG_MODEL = aiDebugModel ?? throw new ArgumentNullException(nameof(aiDebugModel));
            AI_DEBUG_VIEW = aiDebugView != null ? aiDebugView : throw new ArgumentNullException(nameof(aiDebugView));
            REFRESH_INTERVAL = Math.Max(0.02f , refreshInterval);
        }

        public override void Open()
        {
            _refreshTimer = 0f;
            AI_DEBUG_VIEW.Clear();
            AI_DEBUG_VIEW.Open();
        }

        public override void Close()
        {
            AI_DEBUG_VIEW.Close();
        }

        public override void Dispose()
        {
            Close();
        }

        public void Tick(float deltaTime , float currentTime)
        {
            float safeDeltaTime = Math.Max(0f , deltaTime);

            AI_DEBUG_MODEL.UpdateFrameTime(safeDeltaTime);
            _refreshTimer -= safeDeltaTime;

            if ( _refreshTimer > 0f )
            {
                return;
            }

            _refreshTimer = REFRESH_INTERVAL;
            AI_DEBUG_VIEW.SetContent(AI_DEBUG_MODEL.BuildDisplayText(currentTime));
        }
    }
}
