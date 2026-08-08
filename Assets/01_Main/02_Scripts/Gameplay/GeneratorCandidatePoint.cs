using HideSeek.AI;
using UnityEngine;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 발전기 1기가 놓일 수 있는 후보 지점. GDD 7.1
    ///
    /// 후보 지점마다 발전기를 미리 배치해 비활성 상태로 두고, <see cref="GeneratorProvider"/>가
    /// 필요한 수만큼 골라 활성화한다. 런타임에 생성하지 않는 이유는 라이트맵과 NavMesh 베이크에 포함시키고,
    /// 벽면마다 다른 배치 각도를 디자이너가 눈으로 맞출 수 있게 하기 위해서다.
    ///
    /// 발전기 프리팹에 붙여 둔다. 후보 지점에 놓이는 것이 발전기의 기본 사용법이고, 후보를 늘릴 때
    /// 컴포넌트 추가를 잊으면 그 발전기만 항상 켜진 채로 진행도에 집계되는 사고가 조용히 나기 때문이다.
    ///
    /// 튜토리얼처럼 고정 배치가 필요한 발전기는 그 인스턴스에서 이 컴포넌트를 제거한다(프리팹 오버라이드).
    /// 제거된 인스턴스는 Provider가 켜고 끄지 않을 뿐, Config·QTE·위치 표시·진행도 집계는 그대로 받는다.
    ///
    /// TODO: GDD 7.1의 나머지 두 규칙(시작 지점과 가까운 후보 제외, 탈출 지점 인접 후보 수 제한)은
    ///       시작·탈출 지점 시스템이 생기면 여기에 판정 근거를 추가한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorCandidatePoint : MonoBehaviour
    {
        [Tooltip("같은 Zone에 발전기가 몰리는 것을 막는 데 쓴다. 비워 두면 위치로 자동 판별한다. GDD 7.1")]
        [SerializeField] private AIWorldZone _zone;

        [Tooltip("끄면 이번 게임의 후보에서 제외한다. 오브젝트를 지우지 않고 잠시 빼둘 때 쓴다.")]
        [SerializeField] private bool _isAvailable = true;

        public AIWorldZone Zone => _zone;
        public bool IsAvailable => _isAvailable;

        [ContextMenu("Zone/Resolve From Position")]
        private void ResolveZoneFromPosition()
        {
            AIWorldZone[] tArr_zone = FindObjectsByType<AIWorldZone>(FindObjectsInactive.Include , FindObjectsSortMode.None);

            for (int i = 0; i < tArr_zone.Length; i++)
            {
                if (tArr_zone[i].Contains(transform.position))
                {
                    _zone = tArr_zone[i];

                    return;
                }
            }

            Debug.LogWarning($"[{nameof(GeneratorCandidatePoint)}] '{name}'을(를) 포함하는 Zone을 찾지 못했습니다." , this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isAvailable
                ? new Color(1f , 0.75f , 0f , 0.9f)
                : new Color(0.4f , 0.4f , 0.4f , 0.9f);

            Gizmos.DrawWireSphere(transform.position , 0.5f);
            Gizmos.DrawLine(transform.position , transform.position + Vector3.up * 1.2f);

            if (_zone != null)
            {
                Gizmos.color = new Color(1f , 0.75f , 0f , 0.25f);
                Gizmos.DrawLine(transform.position , _zone.CenterPosition);
            }
        }
    }
}
