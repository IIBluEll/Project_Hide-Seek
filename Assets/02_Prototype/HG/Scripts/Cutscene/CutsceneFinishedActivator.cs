using UnityEngine;

namespace HideSeek.Cutscene
{
    /// <summary>
    /// 컷신이 끝나면 지정한 오브젝트를 켠다.
    ///
    /// Timeline의 Activation Track으로 결과 화면을 켜면 Director가 마지막 프레임을
    /// 유지하는 동안에만 켜져 있고, <see cref="CutscenePlayer.Stop"/>이 호출되는 순간 다시 꺼진다.
    /// 결과 화면의 수명을 컷신 재생 상태와 분리하기 위해 활성화를 이쪽으로 옮겼다.
    ///
    /// 현민님의 결과 화면 흐름이 들어오면 이 컴포넌트 대신
    /// <see cref="CutscenePlayer.Finished"/>를 직접 구독하도록 교체한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneFinishedActivator : MonoBehaviour
    {
        [SerializeField] private CutscenePlayer _cutscenePlayer;

        [Tooltip("컷신 종료 시 켤 오브젝트. 시작 시에는 항상 꺼진 상태로 맞춘다.")]
        [SerializeField] private GameObject _targetObj;

        private void Awake()
        {
            if (_targetObj != null)
            {
                _targetObj.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_cutscenePlayer == null)
            {
                Debug.LogError("[CutsceneFinishedActivator] 컷신이 연결되지 않았습니다." , this);

                return;
            }

            _cutscenePlayer.Finished -= OnCutsceneFinishedActioned;
            _cutscenePlayer.Finished += OnCutsceneFinishedActioned;
        }

        private void OnDisable()
        {
            if (_cutscenePlayer != null)
            {
                _cutscenePlayer.Finished -= OnCutsceneFinishedActioned;
            }
        }

        private void OnCutsceneFinishedActioned()
        {
            if (_targetObj == null)
            {
                Debug.LogError("[CutsceneFinishedActivator] 켤 대상이 없습니다." , this);

                return;
            }

            _targetObj.SetActive(true);
        }
    }
}
