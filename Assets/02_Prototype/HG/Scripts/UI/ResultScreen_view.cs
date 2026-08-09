using System;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.UI
{
    /// <summary>
    /// 게임 오버와 게임 클리어 화면이 공유하는 결과 화면이다.
    /// 버튼 입력을 Presenter로 전달하고 버튼 활성 상태만 표시한다. AGENTS.md 3.3
    /// </summary>
    public sealed class ResultScreen_view : AView
    {
        [SerializeField] private Button _restartBtn;
        [SerializeField] private Button _titleBtn;

        public event Action RestartRequested;
        public event Action TitleRequested;

        public void SetRestartInteractable(bool isInteractable)
        {
            if (_restartBtn == null)
            {
                return;
            }

            _restartBtn.interactable = isInteractable;
        }

        public void SetTitleInteractable(bool isInteractable)
        {
            if (_titleBtn == null)
            {
                return;
            }

            _titleBtn.interactable = isInteractable;
        }

        private void Awake()
        {
            if (_restartBtn != null)
            {
                _restartBtn.onClick.AddListener(OnRestartClickedActioned);
            }

            if (_titleBtn != null)
            {
                _titleBtn.onClick.AddListener(OnTitleClickedActioned);
            }
        }

        private void OnDestroy()
        {
            if (_restartBtn != null)
            {
                _restartBtn.onClick.RemoveListener(OnRestartClickedActioned);
            }

            if (_titleBtn != null)
            {
                _titleBtn.onClick.RemoveListener(OnTitleClickedActioned);
            }
        }

        private void OnRestartClickedActioned()
        {
            RestartRequested?.Invoke();
        }

        private void OnTitleClickedActioned()
        {
            TitleRequested?.Invoke();
        }
    }
}
