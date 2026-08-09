using HideSeek.Common;

namespace HideSeek.Loading
{
    public sealed class LoadingModel_model
    {
        public string GameplaySceneName { get; }
        public GAME_DIFFICULTY Difficulty { get; }
        public float Progress01 { get; private set; }
        public string Status { get; private set; } = "Preparing";

        public LoadingModel_model(
            string gameplaySceneName ,
            GAME_DIFFICULTY difficulty)
        {
            GameplaySceneName = gameplaySceneName;
            Difficulty = difficulty;
        }

        public void SetProgress(float progress01)
        {
            Progress01 = progress01 < 0f
                ? 0f
                : progress01 > 1f
                    ? 1f
                    : progress01;
        }

        public void SetStatus(string status)
        {
            Status = string.IsNullOrWhiteSpace(status) ? "Loading" : status;
        }
    }
}
