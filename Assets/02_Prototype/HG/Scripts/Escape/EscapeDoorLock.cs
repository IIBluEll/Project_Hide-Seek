using UnityEngine;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 탈출용 슬라이딩 문을 발전기 수리가 모두 끝날 때까지 잠가 둔다.
    ///
    /// 문과 자동 개폐 센서는 현민 담당이라 파일을 고치지 않는다. 컴포넌트를 껐다 켜는 것으로만 제어한다.
    /// <see cref="SlidingDoor"/>는 enabled가 꺼져 있으면 CanInteract와 SetOpen이 모두 false를 돌려주므로
    /// 상호작용과 센서 양쪽이 한 번에 막힌다. 문 오브젝트 자체를 끄면 모델까지 사라지므로 그렇게 하지 않는다.
    ///
    /// 열어 주기만 하고 직접 열지는 않는다. 잠금이 풀리면 평소 문과 똑같이 센서가 플레이어를 감지해 연다.
    ///
    /// 맵에 같은 문 프리팹이 여럿 있으므로 대상은 인스펙터로 지정한다. 탈출 문에 직접 붙이면 비워 둬도 된다.
    ///
    /// <see cref="EscapeTrigger"/>는 건드리지 않는다. 그쪽은 발전기 조건을 검사하지 않는 대신 문 안쪽에 있어
    /// 잠겨 있는 동안 닿을 수 없다.
    ///
    /// 발전기 완료가 곧 문 활성화다. 중간에 탈출 장치를 켜는 단계는 회의에서 없애기로 했다.
    /// GDD 4.3과 5.7은 아직 그 단계를 포함하고 있어 이 구현과 어긋난다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EscapeDoorLock : MonoBehaviour
    {
        [Tooltip("비워두면 Awake에서 씬을 검색한다.")]
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Header("탈출 문")]
        [Tooltip("비워두면 이 오브젝트와 자식에서 찾는다.")]
        [SerializeField] private SlidingDoor _door;

        [Tooltip("비워두면 이 오브젝트와 자식에서 찾는다. 없으면 상호작용만 잠근다.")]
        [SerializeField] private AutomaticDoorSensor _doorSensor;

        public bool IsUnlocked { get; private set; }

#if UNITY_EDITOR
        private void Reset()
        {
            _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            _door = GetComponentInChildren<SlidingDoor>(true);
            _doorSensor = GetComponentInChildren<AutomaticDoorSensor>(true);
        }
#endif

        private void Awake()
        {
            // 프리팹을 씬에 끌어다 놓으면 Reset이 돌지 않으므로 여기서 한 번 더 받쳐준다.
            if (_gameProgressProvider == null)
            {
                _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            }

            if (_door == null)
            {
                _door = GetComponentInChildren<SlidingDoor>(true);
            }

            if (_doorSensor == null)
            {
                _doorSensor = GetComponentInChildren<AutomaticDoorSensor>(true);
            }

            // 아래 두 경우는 문을 열 방법이 없어진다. 잠그지 않고 배치 그대로 두는 편이
            // 진행이 막히는 것보다 낫고, 오류 로그로 잘못된 배선을 바로 알 수 있다.
            if (_door == null)
            {
                Debug.LogError($"[{nameof(EscapeDoorLock)}] 잠글 문이 없습니다. 탈출 문의 {nameof(SlidingDoor)}를 지정해야 합니다." , this);
                enabled = false;

                return;
            }

            if (_gameProgressProvider == null)
            {
                Debug.LogError($"[{nameof(EscapeDoorLock)}] {nameof(GameProgressProvider)}를 찾지 못했습니다. 문을 열 수 없게 되므로 잠그지 않습니다." , this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // Awake가 배선 실패로 이 컴포넌트를 껐다면 인스펙터에서 다시 켜도 참조는 비어 있다.
            if (_gameProgressProvider == null || _door == null)
            {
                return;
            }

            _gameProgressProvider.AllGeneratorsCompleted += OnAllGeneratorsCompletedActioned;
            _gameProgressProvider.CompletedGeneratorCountChanged += OnCompletedGeneratorCountChangedActioned;

            SetLocked(_gameProgressProvider.AreAllGeneratorsCompleted == false);
        }

        private void OnDisable()
        {
            if (_gameProgressProvider == null)
            {
                return;
            }

            _gameProgressProvider.AllGeneratorsCompleted -= OnAllGeneratorsCompletedActioned;
            _gameProgressProvider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
        }

        private void OnAllGeneratorsCompletedActioned()
        {
            SetLocked(false);

            Debug.Log($"[{nameof(EscapeDoorLock)}] 발전기 수리가 모두 끝나 탈출 문을 활성화했습니다." , this);
        }

        // ResetProgress로 진행도가 되돌아가면 문도 다시 잠근다. 잠금이 한 방향으로만 걸리면
        // 같은 씬에서 다시 시작했을 때 문이 열린 채로 남는다.
        private void OnCompletedGeneratorCountChangedActioned(int completedGeneratorCount)
        {
            if (_gameProgressProvider.AreAllGeneratorsCompleted)
            {
                return;
            }

            SetLocked(true);
        }

        private void SetLocked(bool isLocked)
        {
            IsUnlocked = !isLocked;

            // 열려 있던 문은 먼저 닫는다. SetOpen이 enabled를 보므로 끄기 전에 호출해야 한다.
            // 이동 중이었다면 SlidingDoor.OnDisable이 닫힌 위치로 스냅한다.
            if (isLocked)
            {
                _door.Close();
            }

            _door.enabled = IsUnlocked;

            // 센서를 남겨 두면 잠긴 문 앞에서 0.1초마다 OverlapBox를 계속 돌린다.
            if (_doorSensor != null)
            {
                _doorSensor.enabled = IsUnlocked;
            }
        }
    }
}
