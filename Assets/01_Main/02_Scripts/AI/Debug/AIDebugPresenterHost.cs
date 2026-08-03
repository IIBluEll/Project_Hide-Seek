using UnityEngine;

namespace HideSeek.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MasterAIProvider))]
    public sealed class AIDebugPresenterHost : MonoBehaviour
    {
        [SerializeField] private bool _startVisible = true;
        [SerializeField, Min(0.02f)] private float _refreshInterval = 0.1f;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

        private AIDebugPresenter_presenter _presenter;
        private bool _isVisible;

        private void Awake()
        {
            MasterAIProvider masterAIProvider = GetComponent<MasterAIProvider>();
            ChaseAIController chaseAIController = masterAIProvider.ChaseAIController;

            if ( chaseAIController == null )
            {
                Debug.LogError("[AIDebugPresenterHost] ChaseAIController가 없습니다." , this);
                enabled = false;

                return;
            }

            GameObject debugPanelObj = new("AIDebugPanel");
            debugPanelObj.transform.SetParent(transform , false);

            AIDebugPanel_view debugPanelView = debugPanelObj.AddComponent<AIDebugPanel_view>();
            AIDebugModel_model debugModel = new(masterAIProvider , chaseAIController);

            _presenter = new AIDebugPresenter_presenter(
                debugModel ,
                debugPanelView ,
                _refreshInterval);

            SetVisible(_startVisible);
        }

        private void Update()
        {
            if ( Input.GetKeyDown(_toggleKey) )
            {
                SetVisible(!_isVisible);
            }

            if ( _isVisible )
            {
                _presenter?.Tick(Time.unscaledDeltaTime , Time.time);
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if ( _isVisible )
            {
                _presenter?.Open();
            }
            else
            {
                _presenter?.Close();
            }
        }
    }
}
