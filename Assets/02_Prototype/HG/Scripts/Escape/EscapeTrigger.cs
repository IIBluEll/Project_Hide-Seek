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

            if (!IsPlayer(other))
            {
                return;
            }

            if (_escapeCutscenePlayer == null)
            {
                Debug.LogError("[EscapeTrigger] 탈출 컷신이 연결되지 않았습니다." , this);

                return;
            }

            _wasTriggered = true;

            _escapeCutscenePlayer.TryPlay();

            Debug.Log("[EscapeTrigger] 탈출을 확정하고 컷신을 재생합니다." , this);
        }

        // 태그나 레이어 대신 IPlayerVisibilityState로 식별한다.
        // Chase AI가 쓰는 것과 같은 방식이라 플레이어 오브젝트에 아무 설정도 추가하지 않아도 된다.
        private static bool IsPlayer(Collider other)
        {
            MonoBehaviour[] arr_component = other.GetComponentsInParent<MonoBehaviour>(true);

            for (int componentIndex = 0; componentIndex < arr_component.Length; componentIndex++)
            {
                if (arr_component[componentIndex] is global::IPlayerVisibilityState)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
#endif
    }
}
