using HM.CodeBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HideSeek.UI
{
    /// <summary>
    /// 결과 화면의 재시작과 타이틀 이동을 처리한다.
    ///
    /// 두 대상 모두 Build Settings에 등록되어 있어야 이동할 수 있다.
    /// 등록되지 않은 대상은 버튼을 눌러도 아무 일이 없으므로 아예 비활성으로 표시한다.
    /// 타이틀 씬은 아직 만들어지지 않았고, 인게임 씬도 현재 등록되어 있지 않다.
    /// 등록만 되면 코드 수정 없이 동작한다.
    /// </summary>
    public sealed class ResultScreenPresenter_presenter : APresenter
    {
        private readonly ResultScreen_view RESULT_SCREEN_VIEW;
        private readonly string TITLE_SCENE_NAME;

        public ResultScreenPresenter_presenter(
            ResultScreen_view resultScreenView ,
            string titleSceneName)
        {
            RESULT_SCREEN_VIEW = resultScreenView;
            TITLE_SCENE_NAME = titleSceneName;
        }

        public override void Open()
        {
            RESULT_SCREEN_VIEW.RestartRequested -= OnRestartRequestedActioned;
            RESULT_SCREEN_VIEW.RestartRequested += OnRestartRequestedActioned;

            RESULT_SCREEN_VIEW.TitleRequested -= OnTitleRequestedActioned;
            RESULT_SCREEN_VIEW.TitleRequested += OnTitleRequestedActioned;

            RefreshInteractable();
        }

        public override void Close()
        {
            RESULT_SCREEN_VIEW.Close();
        }

        public override void Dispose()
        {
            RESULT_SCREEN_VIEW.RestartRequested -= OnRestartRequestedActioned;
            RESULT_SCREEN_VIEW.TitleRequested -= OnTitleRequestedActioned;
        }

        private void RefreshInteractable()
        {
            string tActiveSceneName = SceneManager.GetActiveScene().name;

            bool tCanRestart = CanLoad(tActiveSceneName);
            bool tCanGoTitle = CanLoad(TITLE_SCENE_NAME);

            RESULT_SCREEN_VIEW.SetRestartInteractable(tCanRestart);
            RESULT_SCREEN_VIEW.SetTitleInteractable(tCanGoTitle);

            if (!tCanRestart)
            {
                Debug.LogWarning(
                    $"[ResultScreenPresenter] 현재 씬 '{tActiveSceneName}'이 Build Settings에 없어 재시작할 수 없습니다. " +
                    "File > Build Profiles에서 씬을 추가하면 동작합니다.");
            }

            if (!tCanGoTitle)
            {
                Debug.LogWarning(
                    $"[ResultScreenPresenter] 타이틀 씬 '{TITLE_SCENE_NAME}'을 찾을 수 없어 이동할 수 없습니다. " +
                    "씬을 만들고 Build Settings에 추가한 뒤 이름을 맞추면 동작합니다.");
            }
        }

        private void OnRestartRequestedActioned()
        {
            Scene tActiveScene = SceneManager.GetActiveScene();

            if (!CanLoad(tActiveScene.name))
            {
                return;
            }

            // 컷신이 Time.timeScale과 무관하게 동작하므로 여기서 되돌릴 상태는 없다.
            SceneManager.LoadScene(tActiveScene.name);
        }

        private void OnTitleRequestedActioned()
        {
            if (!CanLoad(TITLE_SCENE_NAME))
            {
                return;
            }

            SceneManager.LoadScene(TITLE_SCENE_NAME);
        }

        private static bool CanLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            return Application.CanStreamedLevelBeLoaded(sceneName);
        }
    }
}
