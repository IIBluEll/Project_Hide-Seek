using HideSeek.Common;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HideSeek.UI
{
    /// <summary>
    /// 타이틀 씬의 화면 전환과 씬 이동을 담당한다. GDD 4.1
    ///
    /// 메인 메뉴와 난이도 선택은 한 번에 하나만 열린다. 두 View 모두 이 Presenter가 여닫는다.
    /// 난이도는 여기서 <see cref="DifficultyProvider"/>에 넣고, 값을 쓰는 쪽이 각자 읽는다.
    ///
    /// 이동 대상이 Build Settings에 없으면 버튼을 눌러도 아무 일이 없으므로 아예 비활성으로 표시한다.
    /// 등록만 되면 코드 수정 없이 동작한다.
    /// </summary>
    public sealed class TitlePresenter_presenter : APresenter
    {
        private readonly TitleMenu_view TITLE_MENU_VIEW;
        private readonly DifficultySelect_view DIFFICULTY_SELECT_VIEW;

        public TitlePresenter_presenter(
            TitleMenu_view titleMenuView ,
            DifficultySelect_view difficultySelectView)
        {
            TITLE_MENU_VIEW = titleMenuView;
            DIFFICULTY_SELECT_VIEW = difficultySelectView;
        }

        public override void Open()
        {
            TITLE_MENU_VIEW.GameStartRequested -= OnGameStartRequestedActioned;
            TITLE_MENU_VIEW.GameStartRequested += OnGameStartRequestedActioned;

            TITLE_MENU_VIEW.TutorialRequested -= OnTutorialRequestedActioned;
            TITLE_MENU_VIEW.TutorialRequested += OnTutorialRequestedActioned;

            TITLE_MENU_VIEW.QuitRequested -= OnQuitRequestedActioned;
            TITLE_MENU_VIEW.QuitRequested += OnQuitRequestedActioned;

            DIFFICULTY_SELECT_VIEW.DifficultySelected -= OnDifficultySelectedActioned;
            DIFFICULTY_SELECT_VIEW.DifficultySelected += OnDifficultySelectedActioned;

            DIFFICULTY_SELECT_VIEW.BackRequested -= OnBackRequestedActioned;
            DIFFICULTY_SELECT_VIEW.BackRequested += OnBackRequestedActioned;

            RefreshInteractable();
            ShowTitleMenu();
        }

        public override void Close()
        {
            DIFFICULTY_SELECT_VIEW.Close();
            TITLE_MENU_VIEW.Close();
        }

        public override void Dispose()
        {
            TITLE_MENU_VIEW.GameStartRequested -= OnGameStartRequestedActioned;
            TITLE_MENU_VIEW.TutorialRequested -= OnTutorialRequestedActioned;
            TITLE_MENU_VIEW.QuitRequested -= OnQuitRequestedActioned;

            DIFFICULTY_SELECT_VIEW.DifficultySelected -= OnDifficultySelectedActioned;
            DIFFICULTY_SELECT_VIEW.BackRequested -= OnBackRequestedActioned;
        }

        private void ShowTitleMenu()
        {
            DIFFICULTY_SELECT_VIEW.Close();
            TITLE_MENU_VIEW.Open();
        }

        private void ShowDifficultySelect()
        {
            TITLE_MENU_VIEW.Close();
            DIFFICULTY_SELECT_VIEW.Open();
        }

        private void RefreshInteractable()
        {
            bool tCanStartGame = CanLoad(SceneNames.LOADING);
            bool tCanStartTutorial = CanLoad(SceneNames.TUTORIAL);

            TITLE_MENU_VIEW.SetGameStartInteractable(tCanStartGame);
            TITLE_MENU_VIEW.SetTutorialInteractable(tCanStartTutorial);
            DIFFICULTY_SELECT_VIEW.SetDifficultyInteractable(tCanStartGame);

            if (!tCanStartGame)
            {
                Debug.LogWarning(
                    $"[TitlePresenter] 로딩 씬 '{SceneNames.LOADING}'을 찾을 수 없어 게임을 시작할 수 없습니다. " +
                    "File > Build Profiles에서 씬을 추가하면 동작합니다.");
            }

            if (!tCanStartTutorial)
            {
                Debug.LogWarning(
                    $"[TitlePresenter] 튜토리얼 씬 '{SceneNames.TUTORIAL}'을 찾을 수 없어 이동할 수 없습니다. " +
                    "File > Build Profiles에서 씬을 추가하면 동작합니다.");
            }
        }

        private void OnGameStartRequestedActioned()
        {
            ShowDifficultySelect();
        }

        private void OnBackRequestedActioned()
        {
            ShowTitleMenu();
        }

        private void OnDifficultySelectedActioned(GAME_DIFFICULTY difficulty)
        {
            if (!CanLoad(SceneNames.LOADING))
            {
                return;
            }

            // 난이도를 먼저 넣는다. 인게임 씬을 여는 쪽은 로딩 씬이고, 그때는 이미 값이 있어야 한다.
            DifficultyProvider.Select(difficulty);

            SceneManager.LoadScene(SceneNames.LOADING);
        }

        private void OnTutorialRequestedActioned()
        {
            if (!CanLoad(SceneNames.TUTORIAL))
            {
                return;
            }

            SceneManager.LoadScene(SceneNames.TUTORIAL);
        }

        private void OnQuitRequestedActioned()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
