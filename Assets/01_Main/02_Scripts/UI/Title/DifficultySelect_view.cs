using System;
using HideSeek.Common;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.UI
{
    /// <summary>
    /// 난이도 선택 화면이다. 메인 메뉴에서 게임 시작을 누르면 열린다. GDD 4.1
    /// 어느 버튼을 눌렀는지만 Presenter로 전달한다. 난이도를 보관하지 않는다.
    ///
    /// 인스펙터 설정 기준
    /// - 세 버튼은 각각 Easy, Normal, Hard에 연결한다. 버튼과 난이도의 연결은 코드가 정한다.
    /// - GDD 13.1의 난이도별 설명 문구는 아직 정해지지 않아 이 View에 넣지 않았다.
    /// </summary>
    public sealed class DifficultySelect_view : AView
    {
        [SerializeField] private Button _easyBtn;
        [SerializeField] private Button _normalBtn;
        [SerializeField] private Button _hardBtn;

        [Tooltip("메인 메뉴로 돌아가는 버튼. 없어도 동작한다.")]
        [SerializeField] private Button _backBtn;

        public event Action<GAME_DIFFICULTY> DifficultySelected;
        public event Action BackRequested;

        public void SetDifficultyInteractable(bool isInteractable)
        {
            SetInteractable(_easyBtn , isInteractable);
            SetInteractable(_normalBtn , isInteractable);
            SetInteractable(_hardBtn , isInteractable);
        }

        private void Awake()
        {
            if (_easyBtn != null)
            {
                _easyBtn.onClick.AddListener(OnEasyClickedActioned);
            }

            if (_normalBtn != null)
            {
                _normalBtn.onClick.AddListener(OnNormalClickedActioned);
            }

            if (_hardBtn != null)
            {
                _hardBtn.onClick.AddListener(OnHardClickedActioned);
            }

            if (_backBtn != null)
            {
                _backBtn.onClick.AddListener(OnBackClickedActioned);
            }
        }

        private void OnDestroy()
        {
            if (_easyBtn != null)
            {
                _easyBtn.onClick.RemoveListener(OnEasyClickedActioned);
            }

            if (_normalBtn != null)
            {
                _normalBtn.onClick.RemoveListener(OnNormalClickedActioned);
            }

            if (_hardBtn != null)
            {
                _hardBtn.onClick.RemoveListener(OnHardClickedActioned);
            }

            if (_backBtn != null)
            {
                _backBtn.onClick.RemoveListener(OnBackClickedActioned);
            }
        }

        private void OnEasyClickedActioned()
        {
            DifficultySelected?.Invoke(GAME_DIFFICULTY.EASY);
        }

        private void OnNormalClickedActioned()
        {
            DifficultySelected?.Invoke(GAME_DIFFICULTY.NORMAL);
        }

        private void OnHardClickedActioned()
        {
            DifficultySelected?.Invoke(GAME_DIFFICULTY.HARD);
        }

        private void OnBackClickedActioned()
        {
            BackRequested?.Invoke();
        }

        private static void SetInteractable(Button button , bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
        }
    }
}
