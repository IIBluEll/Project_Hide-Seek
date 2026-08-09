using UnityEngine;

namespace HideSeek.UI
{
    /// <summary>
    /// <see cref="TitlePresenter_presenter"/>의 수명을 소유한다.
    /// APresenter는 MonoBehaviour가 아니라 씬에서 만들어 줄 컴포넌트가 필요하다.
    ///
    /// 타이틀 UI 루트에 붙이고 두 View를 연결한다.
    /// 인게임에서 돌아오면 커서가 잠긴 상태이므로 같은 오브젝트에 <see cref="CursorReleaser"/>도 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitlePresenterHost : MonoBehaviour
    {
        [SerializeField] private TitleMenu_view _titleMenuView;
        [SerializeField] private DifficultySelect_view _difficultySelectView;

        private TitlePresenter_presenter _presenter;

        private void Awake()
        {
            if (_titleMenuView == null || _difficultySelectView == null)
            {
                Debug.LogError("[TitlePresenterHost] 타이틀 View가 연결되지 않았습니다." , this);

                return;
            }

            _presenter = new TitlePresenter_presenter(_titleMenuView , _difficultySelectView);
        }

        private void OnEnable()
        {
            _presenter?.Open();
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
        }
    }
}
