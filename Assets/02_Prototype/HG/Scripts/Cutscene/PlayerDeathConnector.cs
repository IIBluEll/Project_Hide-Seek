using HideSeek.AI;
using HideSeek.Cutscene;
using HideSeek.Sound;
using UnityEngine;

namespace HideSeek.Integration
{
    /// <summary>
    /// AI의 포획 확정과 사망 컷신을 잇는다.
    ///
    /// 외부 시스템은 <see cref="ChaseAIController"/>를 직접 잡지 않고
    /// <see cref="MasterAIProvider"/>를 단일 창구로 사용한다. 연동 명세서 3절
    ///
    /// 플레이어 조작 정지는 이쪽 책임이 아니다. 플레이어 담당자가
    /// <see cref="MasterAIProvider.PlayerCaught"/>를 직접 구독해 처리한다.
    /// 결과 화면도 마찬가지로 <see cref="DeathCutsceneStage.Finished"/>를 구독한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeathConnector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MasterAIProvider _masterAIProvider;
        [SerializeField] private DeathCutsceneStage _deathCutsceneStage;

        [Header("Sound")]
        [SerializeField] private BgmPlayer _bgmPlayer;
        [SerializeField , Min(0f)] private float _bgmFadeOutDuration = 0.2f;

        private bool _hasHandledCapture;

        private void OnEnable()
        {
            if (_masterAIProvider == null)
            {
                Debug.LogError("[PlayerDeathConnector] MasterAIProvider가 없습니다." , this);

                return;
            }

            _masterAIProvider.PlayerCaught -= OnPlayerCaughtActioned;
            _masterAIProvider.PlayerCaught += OnPlayerCaughtActioned;
        }

        private void OnDisable()
        {
            if (_masterAIProvider != null)
            {
                _masterAIProvider.PlayerCaught -= OnPlayerCaughtActioned;
            }
        }

        private void OnPlayerCaughtActioned()
        {
            // PlayerCaught는 한 번만 발행되지만, Connector가 꺼졌다 켜지는 경우까지 막는다.
            if (_hasHandledCapture)
            {
                return;
            }

            if (_deathCutsceneStage == null)
            {
                Debug.LogError("[PlayerDeathConnector] DeathCutsceneStage가 없습니다." , this);

                return;
            }

            _hasHandledCapture = true;

            StopBgm();

            if (!_deathCutsceneStage.TryPlay())
            {
                Debug.LogError("[PlayerDeathConnector] 사망 컷신을 재생하지 못했습니다." , this);

                return;
            }

            Debug.Log("[PlayerDeathConnector] 사망 컷신을 재생합니다." , this);
        }

        private void StopBgm()
        {
            if (_bgmPlayer == null)
            {
                return;
            }

            // 점프스케어 사운드가 BGM에 묻히지 않게 먼저 비운다.
            _bgmPlayer.Stop(_bgmFadeOutDuration);
        }
    }
}
