using System;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.UI
{
    /// <summary>
    /// 타이틀 메인 메뉴다. 로고는 표시만 하므로 이 View가 참조하지 않는다.
    /// 버튼 입력을 Presenter로 전달하고 버튼 활성 상태만 표시한다. AGENTS.md 3.3
    /// </summary>
    public sealed class TitleMenu_view : AView
    {
        [SerializeField] private Button _gameStartBtn;
        [SerializeField] private Button _tutorialBtn;
        [SerializeField] private Button _quitBtn;

        public event Action GameStartRequested;
        public event Action TutorialRequested;
        public event Action QuitRequested;

        public void SetGameStartInteractable(bool isInteractable)
        {
            if (_gameStartBtn == null)
            {
                return;
            }

            _gameStartBtn.interactable = isInteractable;
        }

        public void SetTutorialInteractable(bool isInteractable)
        {
            if (_tutorialBtn == null)
            {
                return;
            }

            _tutorialBtn.interactable = isInteractable;
        }

        /// <summary>
        /// 나가기를 지원하지 않는 플랫폼에서는 버튼을 숨긴다.
        /// 비활성으로 두면 누를 수 없는 이유가 드러나지 않으므로 아예 보이지 않게 한다.
        /// </summary>
        public void SetQuitVisible(bool isVisible)
        {
            if (_quitBtn == null)
            {
                return;
            }

            _quitBtn.gameObject.SetActive(isVisible);
        }

        private void Awake()
        {
            if (_gameStartBtn != null)
            {
                _gameStartBtn.onClick.AddListener(OnGameStartClickedActioned);
            }

            if (_tutorialBtn != null)
            {
                _tutorialBtn.onClick.AddListener(OnTutorialClickedActioned);
            }

            if (_quitBtn != null)
            {
                _quitBtn.onClick.AddListener(OnQuitClickedActioned);
            }
        }

        private void OnDestroy()
        {
            if (_gameStartBtn != null)
            {
                _gameStartBtn.onClick.RemoveListener(OnGameStartClickedActioned);
            }

            if (_tutorialBtn != null)
            {
                _tutorialBtn.onClick.RemoveListener(OnTutorialClickedActioned);
            }

            if (_quitBtn != null)
            {
                _quitBtn.onClick.RemoveListener(OnQuitClickedActioned);
            }
        }

        private void OnGameStartClickedActioned()
        {
            GameStartRequested?.Invoke();
        }

        private void OnTutorialClickedActioned()
        {
            TutorialRequested?.Invoke();
        }

        private void OnQuitClickedActioned()
        {
            QuitRequested?.Invoke();
        }
    }
}
