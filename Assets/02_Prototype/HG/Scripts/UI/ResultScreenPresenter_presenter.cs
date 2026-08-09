using HideSeek.Common;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HideSeek.UI
{
    /// <summary>
    /// 결과 화면의 재시작과 타이틀 이동을 처리한다.
    /// 재시작은 현재 씬을 다시 여는 것이 아니라 로딩 씬으로 보낸다.
    ///
    /// 두 대상 모두 Build Settings에 등록되어 있어야 이동할 수 있다.
    /// 등록되지 않은 대상은 버튼을 눌러도 아무 일이 없으므로 아예 비활성으로 표시한다.
    /// 등록만 되면 코드 수정 없이 동작한다.
    /// </summary>
    public sealed class ResultScreenPresenter_presenter : APresenter
    {
        private readonly ResultScreen_view RESULT_SCREEN_VIEW;

        public ResultScreenPresenter_presenter(ResultScreen_view resultScreenView)
        {
            RESULT_SCREEN_VIEW = resultScreenView;
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
            bool tCanRestart = CanLoad(SceneNames.LOADING);
            bool tCanGoTitle = CanLoad(SceneNames.TITLE);

            RESULT_SCREEN_VIEW.SetRestartInteractable(tCanRestart);
            RESULT_SCREEN_VIEW.SetTitleInteractable(tCanGoTitle);

            if (!tCanRestart)
            {
                Debug.LogWarning(
                    $"[ResultScreenPresenter] 로딩 씬 '{SceneNames.LOADING}'을 찾을 수 없어 재시작할 수 없습니다. " +
                    "File > Build Profiles에서 씬을 추가하면 동작합니다.");
            }

            if (!tCanGoTitle)
            {
                Debug.LogWarning(
                    $"[ResultScreenPresenter] 타이틀 씬 '{SceneNames.TITLE}'을 찾을 수 없어 이동할 수 없습니다. " +
                    "File > Build Profiles에서 씬을 추가하면 동작합니다.");
            }
        }

        private void OnRestartRequestedActioned()
        {
            if (!CanLoad(SceneNames.LOADING))
            {
                return;
            }

            // 현재 씬을 다시 여는 대신 로딩 씬을 거친다. 난이도는 그대로 유지된다.
            // 컷신이 Time.timeScale과 무관하게 동작하므로 여기서 되돌릴 상태는 없다.
            SceneManager.LoadScene(SceneNames.LOADING);
        }

        private void OnTitleRequestedActioned()
        {
            if (!CanLoad(SceneNames.TITLE))
            {
                return;
            }

            SceneManager.LoadScene(SceneNames.TITLE);
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
