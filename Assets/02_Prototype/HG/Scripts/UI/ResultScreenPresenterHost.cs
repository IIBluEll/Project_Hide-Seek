using UnityEngine;

namespace HideSeek.UI
{
    /// <summary>
    /// <see cref="ResultScreenPresenter_presenter"/>의 수명을 소유한다.
    /// APresenter는 MonoBehaviour가 아니라 씬에서 만들어 줄 컴포넌트가 필요하다.
    ///
    /// 결과 화면 루트에 붙인다. 화면이 켜질 때 구독하고 꺼질 때 해제한다.
    ///
    /// TODO: 현민님의 결과 화면 흐름이 확정되면 이 클래스를 제거하고 해당 매니저가 같은 역할을 맡는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultScreenPresenterHost : MonoBehaviour
    {
        [SerializeField] private ResultScreen_view _resultScreenView;

        [Tooltip("타이틀 씬 이름. 씬이 만들어지고 Build Settings에 등록되면 그 이름으로 맞춘다.")]
        [SerializeField] private string _titleSceneName = "Title";

        private ResultScreenPresenter_presenter _presenter;

        private void Awake()
        {
            if (_resultScreenView == null)
            {
                Debug.LogError("[ResultScreenPresenterHost] 결과 화면 View가 없습니다." , this);

                return;
            }

            _presenter = new ResultScreenPresenter_presenter(_resultScreenView , _titleSceneName);
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
