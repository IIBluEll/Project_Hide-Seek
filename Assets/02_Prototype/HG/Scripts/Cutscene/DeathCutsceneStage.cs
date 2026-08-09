using UnityEngine;

namespace HideSeek.Cutscene
{
    /// <summary>
    /// 월드와 분리된 공간에서 재생하는 사망 컷신이다.
    /// 전용 카메라가 Cutscene 레이어만 렌더링하고 배경을 단색으로 지우므로,
    /// 플레이어가 맵 어디에서 붙잡혔는지는 결과에 영향을 주지 않는다. GDD 14.3
    ///
    /// 인스펙터 설정은 프로젝트 문서의 사망 컷신 항목을 따른다.
    /// </summary>
    public sealed class DeathCutsceneStage : ACutscenePlayer
    {
        [Header("Stage")]
        [Tooltip("카메라·모델·조명을 묶은 루트. 평소에는 비활성으로 둔다. 이 컴포넌트가 붙은 오브젝트여서는 안 된다.")]
        [SerializeField] private GameObject _stageRootObj;

        [Tooltip("선택 항목. 연결하면 컷신 종료 후 결과 화면이 뜰 때까지 검은 화면을 유지한다. " +
                 "암전 없이 마지막 프레임에서 바로 결과 화면으로 넘길 때는 비워 둔다.")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;

        private void Awake()
        {
            if (_stageRootObj != null)
            {
                _stageRootObj.SetActive(false);
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }
        }

        protected override void OnCutsceneBegin()
        {
            _stageRootObj.SetActive(true);

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }
        }

        protected override void OnCutsceneEnd()
        {
            if (_fadeCanvasGroup == null)
            {
                return;
            }

            // Timeline이 알파를 끝까지 올리지 못한 경우까지 포함해 암전을 코드로 확정한다.
            _fadeCanvasGroup.alpha = 1f;
        }

        public override void Stop()
        {
            base.Stop();

            if (_stageRootObj != null)
            {
                _stageRootObj.SetActive(false);
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }
        }

        protected override bool ValidateReferences()
        {
            if (!base.ValidateReferences())
            {
                return false;
            }

            if (_stageRootObj == null)
            {
                Debug.LogError("[DeathCutsceneStage] 스테이지 루트가 없습니다." , this);

                return false;
            }

            // 루트를 끄면 이 컴포넌트와 Director까지 함께 꺼져 완료 통보가 오지 않는다.
            if (_stageRootObj == gameObject)
            {
                Debug.LogError(
                    "[DeathCutsceneStage] 스테이지 루트는 이 컴포넌트가 붙은 오브젝트가 아니라 자식이어야 합니다." ,
                    this);

                return false;
            }

            return true;
        }
    }
}
