using HideSeek.AI;
using HideSeek.Cutscene;
using HideSeek.Sound;
using UnityEngine;

namespace HideSeek.Integration
{
    /// <summary>
    /// AI의 포획 확정과 사망 컷신을 잇는다. AI와 플레이어, 컷신은 서로를 직접 참조하지 않는다.
    /// 결과 화면은 이 컴포넌트가 아니라 <see cref="DeathCutsceneStage.Finished"/>를 구독한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDeathConnector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private DeathCutsceneStage _deathCutsceneStage;

        [Tooltip("컷신 중 꺼 둘 플레이어 카메라.")]
        [SerializeField] private Camera _playerCamera;

        [Header("Player")]
        [Tooltip("포획 시 조작을 멈추기 위해 비활성화한다. 진우님의 TryCapture()가 들어오면 그 호출로 교체한다.")]
        [SerializeField] private PlayerController _playerController;

        [Header("Sound")]
        [SerializeField] private BgmPlayer _bgmPlayer;
        [SerializeField , Min(0f)] private float _bgmFadeOutDuration = 0.2f;

        private bool _hasHandledCapture;

        private void OnEnable()
        {
            if (_chaseAIController == null)
            {
                Debug.LogError("[PlayerDeathConnector] ChaseAIController가 없습니다." , this);

                return;
            }

            _chaseAIController.PlayerCaught -= OnPlayerCaughtActioned;
            _chaseAIController.PlayerCaught += OnPlayerCaughtActioned;
        }

        private void OnDisable()
        {
            if (_chaseAIController != null)
            {
                _chaseAIController.PlayerCaught -= OnPlayerCaughtActioned;
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

            LockPlayer();
            StopBgm();

            if (!_deathCutsceneStage.TryPlay(_playerCamera))
            {
                Debug.LogError("[PlayerDeathConnector] 사망 컷신을 재생하지 못했습니다." , this);

                return;
            }

            Debug.Log("[PlayerDeathConnector] 사망 컷신을 재생합니다." , this);
        }

        private void LockPlayer()
        {
            if (_playerController == null)
            {
                Debug.LogWarning("[PlayerDeathConnector] PlayerController가 없어 조작을 멈추지 못했습니다." , this);

                return;
            }

            // 임시 처리다. PlayerController.OnDisable이 입력 구독을 해제하므로 조작 입력은 끊기지만,
            // MoveController에 남아 있던 이동 입력까지 정리되지는 않는다.
            // 화면이 스테이지로 덮이는 동안에는 보이지 않으므로 마감 전까지는 이대로 둔다.
            // 진우님의 TryCapture()가 들어오면 이 줄을 그 호출로 교체한다. 연동 명세서 5.1
            _playerController.enabled = false;
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
