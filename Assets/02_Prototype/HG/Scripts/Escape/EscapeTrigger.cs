using HideSeek.Cutscene;
using UnityEngine;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 탈출 경로로 통하는 문을 통과한 순간을 감지해 탈출 컷신을 재생한다.
    ///
    /// 문 자체가 아니라 문 안쪽에 두어야 한다. 문에 붙이면 열어만 두고
    /// 들어가지 않거나 문 앞에서 서성일 때 오작동한다.
    ///
    /// 발전기 완료 여부는 검사하지 않는다. <see cref="EscapeDoorLock"/>이 문을 잠가 두므로
    /// 조건을 만족하기 전에는 여기까지 올 수 없고, 검사를 넣으면 컷신만 따로 확인할 수 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class EscapeTrigger : MonoBehaviour
    {
        [SerializeField] private CutscenePlayer _escapeCutscenePlayer;

        private bool _wasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_wasTriggered)
            {
                return;
            }

            if (!TryGetPlayer(other , out global::PlayerController tPlayerController))
            {
                return;
            }

            if (_escapeCutscenePlayer == null)
            {
                Debug.LogError("[EscapeTrigger] 탈출 컷신이 연결되지 않았습니다." , this);

                return;
            }

            if (!_escapeCutscenePlayer.TryPlay())
            {
                Debug.LogError("[EscapeTrigger] 탈출 컷신을 재생하지 못했습니다." , this);

                return;
            }

            _wasTriggered = true;

            // 플레이어 루트는 활성 상태로 유지하고, 상태 전환으로 조작과 발소리만 정지한다.
            tPlayerController.State.SetActionState(global::PLAYER_ACTION_STATE.TRANSITION);

            Debug.Log("[EscapeTrigger] 탈출을 확정하고 컷신을 재생합니다." , this);
        }

        // 태그나 레이어 대신 실제 PlayerController를 찾아 컷신용 상태 전환까지 같은 대상에 적용한다.
        private static bool TryGetPlayer(Collider other , out global::PlayerController playerController)
        {
            playerController = other.GetComponentInParent<global::PlayerController>(true);

            return playerController != null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
#endif
    }
}
