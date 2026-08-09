using System.Collections;
using HideSeek.AI;
using HideSeek.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HideSeek.Loading
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LoadingScreen_view))]
    public sealed class LoadingPresenterHost : MonoBehaviour
    {
        [SerializeField] private LoadingScreen_view _loadingScreenView;
        [SerializeField] private string _gameplaySceneName = "InGame_Map";
        [SerializeField, Min(0f)] private float _minimumDisplayDuration = 1.5f;

        private LoadingPresenter_presenter _presenter;

        private void Awake()
        {
            if ( _loadingScreenView == null )
            {
                _loadingScreenView = GetComponent<LoadingScreen_view>();
            }

            if ( _loadingScreenView == null )
            {
                Debug.LogError("[LoadingPresenterHost] LoadingScreen View가 없습니다." , this);

                return;
            }

            LoadingModel_model loadingModel = new(
                _gameplaySceneName ,
                DifficultyProvider.Current);

            _presenter = new LoadingPresenter_presenter(loadingModel , _loadingScreenView);
        }

        private void OnEnable()
        {
            _presenter?.Open();
        }

        private void Start()
        {
            if ( _presenter == null )
            {
                return;
            }

            StartCoroutine(LoadGameScene_cor());
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
        }

        private IEnumerator LoadGameScene_cor()
        {
            if ( string.IsNullOrWhiteSpace(_gameplaySceneName) ||
                 !Application.CanStreamedLevelBeLoaded(_gameplaySceneName) )
            {
                FailLoading($"Scene not found: {_gameplaySceneName}");

                yield break;
            }

            float loadingStartedTime = Time.realtimeSinceStartup;

            _presenter.SetStatus("Loading facility");
            _presenter.SetProgress(0f);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                _gameplaySceneName ,
                LoadSceneMode.Additive);

            if ( loadOperation == null )
            {
                FailLoading($"Could not start scene load: {_gameplaySceneName}");

                yield break;
            }

            loadOperation.allowSceneActivation = false;

            while ( loadOperation.progress < 0.9f ||
                    Time.realtimeSinceStartup - loadingStartedTime < _minimumDisplayDuration )
            {
                _presenter.SetProgress(Mathf.Clamp01(loadOperation.progress / 0.9f));

                yield return null;
            }

            _presenter.SetStatus("Initializing AI");
            _presenter.SetProgress(0.95f);

            loadOperation.allowSceneActivation = true;

            while ( !loadOperation.isDone )
            {
                yield return null;
            }

            Scene gameplayScene = SceneManager.GetSceneByName(_gameplaySceneName);

            if ( !gameplayScene.IsValid() || !gameplayScene.isLoaded )
            {
                FailLoading($"Loaded scene is invalid: {_gameplaySceneName}");

                yield break;
            }

            if ( !SceneManager.SetActiveScene(gameplayScene) )
            {
                FailLoading($"Could not activate scene: {_gameplaySceneName}");

                yield break;
            }

            MasterAIProvider masterAIProvider = FindComponentInScene<MasterAIProvider>(gameplayScene);

            if ( masterAIProvider == null )
            {
                FailLoading("MasterAIProvider not found");

                yield break;
            }

            _presenter.SetStatus("Ready");
            _presenter.SetProgress(1f);

            yield return null;

            masterAIProvider.StartGameplay();

            Scene loadingScene = gameObject.scene;
            SceneManager.UnloadSceneAsync(loadingScene);
        }

        private void FailLoading(string reason)
        {
            _presenter.SetStatus("Loading failed");

            Debug.LogError($"[LoadingPresenterHost] {reason}" , this);
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for ( int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++ )
            {
                T component = rootObjects[ rootIndex ].GetComponentInChildren<T>(true);

                if ( component != null )
                {
                    return component;
                }
            }

            return null;
        }
    }
}
