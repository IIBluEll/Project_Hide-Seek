using HideSeek.AI;
using HideSeek.Gameplay;
using HideSeek.Sound;
using UnityEngine;

namespace HideSeek.Integration
{
    // 값이 클수록 긴장이 높다. 단계 비교에 그대로 쓴다.
    public enum BGM_STATE
    {
        AMBIENT = 0,
        TENSION = 1,
        CHASE = 2,
        ENDING = 3
    }

    /// <summary>
    /// Chase AI 상태와 게임 진행도를 모아 BGM 단계를 정한다. GDD 14.1
    ///
    /// Chase AI가 직접 BGM을 바꾸지 않는다. AI가 여럿일 때 한 개체가 추격을 포기했다고
    /// 음악이 평온해지면 안 되기 때문이다. 등록된 AI 중 가장 높은 단계를 채택한다.
    ///
    /// 전환 규칙은 방향에 따라 다르다.
    /// - 상승: 단계를 건너뛴다. AMBIENT에서 CHASE로 바로 간다.
    /// - 하강: 한 단계씩 내려온다. CHASE에서 AMBIENT로 직행하지 않는다.
    /// - TENSION에는 최소 유지 시간을 둔다. 없으면 추격이 끝난 프레임에 그대로 통과한다.
    /// - ENDING은 예외다. 한 번 진입하면 AI 상태와 무관하게 고정한다.
    ///
    /// TENSION은 AI가 플레이어와 같은 Zone에 있을 때만 켠다. 무엇을 하는 중인지는 보지 않는다.
    /// Director 힌트는 부정확한 Zone을 가리키므로(GDD 9.4), 조사 중이라는 이유로 긴장을 켜면
    /// 맵 반대편을 뒤지는 동안에도 음악이 유지되어 위치 정보를 주지 못한다. GDD 3.1
    ///
    /// 기획서와 다른 점이 두 가지 있다. 2026-08-03 기획 확인을 거쳐 결정했으나 GDD는 아직 갱신하지 않았다.
    /// - GDD 14.1의 SAFE를 쓰지 않는다. AMBIENT, TENSION과 조건이 겹쳐 세 단계로 줄였다.
    /// - GDD 14.1은 ENDING 조건을 "모든 발전기 완료 + 탈출 장치 활성화"로 정의하지만, 탈출 장치가 없으므로
    ///   발전기 완료 시점에 바로 진입한다. 탈출 장치가 생기면 조건을 옮길지 다시 확인한다.
    ///
    /// TODO: 현재는 매 프레임 AI 상태를 폴링한다.
    ///       Chase AI와 Director에 상태 변경 이벤트가 생기면 구독 방식으로 바꾼다.
    ///       ENDING 축은 이미 이벤트를 받으므로 이 전환의 대상이 아니다.
    /// TODO: AI 목록을 Start에서 한 번만 수집하므로 런타임에 생성되는 AI는 놓친다.
    ///       위 이벤트 구독으로 바꿀 때 등록·해제 방식으로 같이 정리한다.
    /// TODO: 인접 Zone까지 포함할지는 회의에서 확정한다. GDD 14.1은 인접 Zone도 조건에 넣고 있다.
    /// TODO: GDD 14.1의 AMBIENT 조건에 있는 "긴장도가 낮은"을 반영하지 않았다.
    ///       MasterAIProvider.GlobalStress를 쓸지와 임계값은 회의에서 확정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BgmStateConnector : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private BgmPlayer _bgmPlayer;

        [Tooltip("플레이어가 있는 Zone을 얻는 데 쓴다.")]
        [SerializeField] private MasterAIProvider _masterAIProvider;

        [Tooltip("비워두면 Start에서 씬의 GameProgressProvider를 찾는다. 프리팹은 씬 오브젝트를 참조할 수 없어 폴백이 필요하다.")]
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Tooltip("비워두면 Start에서 씬의 Chase AI를 모두 찾는다.")]
        [SerializeField] private ChaseAIController[] _arr_chaseAI;

        [Header("클립")]
        [SerializeField] private AudioClip _ambientClip;
        [SerializeField] private AudioClip _tensionClip;
        [SerializeField] private AudioClip _chaseClip;
        [SerializeField] private AudioClip _endingClip;

        [Header("전환")]
        [SerializeField, Min(0f)] private float _fadeDuration = 2f;

        [Tooltip("TENSION에서 AMBIENT로 내려가기까지 최소로 머무는 시간(초).")]
        [SerializeField, Min(0f)] private float _tensionMinDuration = 6f;

        private BGM_STATE _currentState = BGM_STATE.AMBIENT;
        private float _stateElapsed;
        private bool _isEndingLatched;

        public BGM_STATE CurrentState => _currentState;

        private void Start()
        {
            if (_bgmPlayer == null)
            {
                Debug.LogError($"[{nameof(BgmStateConnector)}] BgmPlayer 참조가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            if (HasAnyChaseAI() == false)
            {
                _arr_chaseAI = FindObjectsByType<ChaseAIController>(FindObjectsSortMode.None);
            }

            if (_gameProgressProvider == null)
            {
                _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            }

            SubscribeProgress();

            _currentState = BGM_STATE.AMBIENT;
            _stateElapsed = 0f;
            _isEndingLatched = false;
            _bgmPlayer.Play(_ambientClip, 0f);
        }

        private void OnDestroy()
        {
            UnsubscribeProgress();
        }

        private void Update()
        {
            // ENDING은 되돌리지 않는다. 탈출 구간에서 추격이 시작돼도 음악은 그대로 둔다.
            if (_isEndingLatched)
            {
                return;
            }

            _stateElapsed += Time.deltaTime;

            BGM_STATE tTargetState = ResolveTargetState();

            if (tTargetState > _currentState)
            {
                ChangeState(tTargetState);
                return;
            }

            if (tTargetState == _currentState)
            {
                return;
            }

            // 여기부터는 하강이다. 한 단계만 내린다.
            if (_currentState == BGM_STATE.CHASE)
            {
                ChangeState(BGM_STATE.TENSION);
                return;
            }

            if (_currentState == BGM_STATE.TENSION && _stateElapsed >= _tensionMinDuration)
            {
                ChangeState(BGM_STATE.AMBIENT);
            }
        }

        private void SubscribeProgress()
        {
            if (_gameProgressProvider == null)
            {
                Debug.LogError($"[{nameof(BgmStateConnector)}] GameProgressProvider가 없어 ENDING으로 전환되지 않습니다.", this);
                return;
            }

            _gameProgressProvider.AllGeneratorsCompleted -= OnAllGeneratorsCompletedActioned;
            _gameProgressProvider.AllGeneratorsCompleted += OnAllGeneratorsCompletedActioned;

            _gameProgressProvider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
            _gameProgressProvider.CompletedGeneratorCountChanged += OnCompletedGeneratorCountChangedActioned;

            // 이 컴포넌트보다 먼저 완료될 일은 없지만, 확인해 두면 실행 순서에 기대지 않는다.
            if (_gameProgressProvider.AreAllGeneratorsCompleted)
            {
                OnAllGeneratorsCompletedActioned();
            }
        }

        private void UnsubscribeProgress()
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
            _isEndingLatched = true;

            ChangeState(BGM_STATE.ENDING);
        }

        // 같은 씬에서 재시작하면 GameProgressProvider.ResetProgress()가 완료 수 0을 발행한다. 그때 ENDING도 푼다.
        private void OnCompletedGeneratorCountChangedActioned(int completedGeneratorCount)
        {
            if (_isEndingLatched == false || completedGeneratorCount > 0)
            {
                return;
            }

            _isEndingLatched = false;
            _stateElapsed = 0f;

            // ENDING에는 하강 규칙이 없으므로 여기서 직접 내린다. 이후 단계는 Update가 다시 올린다.
            ChangeState(BGM_STATE.AMBIENT);
        }

        // 인스펙터에서 슬롯만 만들고 비워 두는 경우가 있어 길이만으로 판단하지 않는다.
        private bool HasAnyChaseAI()
        {
            if (_arr_chaseAI == null)
            {
                return false;
            }

            for (int i = 0; i < _arr_chaseAI.Length; i++)
            {
                if (_arr_chaseAI[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 등록된 AI 중 가장 높은 단계를 고른다. 한 개체가 추격을 포기해도 다른 개체가
        /// 추격 중이면 CHASE가 유지된다.
        /// </summary>
        private BGM_STATE ResolveTargetState()
        {
            BGM_STATE tHighestState = BGM_STATE.AMBIENT;

            for (int i = 0; i < _arr_chaseAI.Length; i++)
            {
                ChaseAIController tChaseAI = _arr_chaseAI[i];
                if (tChaseAI == null || tChaseAI.isActiveAndEnabled == false)
                {
                    continue;
                }

                BGM_STATE tState = ResolveStateFor(tChaseAI);
                if (tState > tHighestState)
                {
                    tHighestState = tState;
                }

                if (tHighestState == BGM_STATE.CHASE)
                {
                    break;
                }
            }

            return tHighestState;
        }

        private BGM_STATE ResolveStateFor(ChaseAIController chaseAI)
        {
            switch (chaseAI.CurrentState)
            {
                case CHASE_AI_STATE.CHASE:
                case CHASE_AI_STATE.ATTACK:
                    return BGM_STATE.CHASE;

                case CHASE_AI_STATE.DORMANT:
                case CHASE_AI_STATE.RETREAT:
                    // 맵에서 빠지는 중이므로 거리와 무관하게 긴장을 만들지 않는다.
                    return BGM_STATE.AMBIENT;

                default:
                    // PATROL, INVESTIGATE, SEARCH. 무엇을 하든 위치로만 판단한다.
                    return IsInPlayerZone(chaseAI.transform.position) ? BGM_STATE.TENSION : BGM_STATE.AMBIENT;
            }
        }

        private bool IsInPlayerZone(Vector3 worldPosition)
        {
            if (_masterAIProvider == null)
            {
                return false;
            }

            AIWorldZone tPlayerZone = _masterAIProvider.CurrentPlayerZone;

            return tPlayerZone != null && tPlayerZone.Contains(worldPosition);
        }

        private void ChangeState(BGM_STATE state)
        {
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;
            _stateElapsed = 0f;
            _bgmPlayer.Play(GetClip(state), _fadeDuration);

            Debug.Log($"[{nameof(BgmStateConnector)}] BGM 상태 전환: {state}", this);
        }

        private AudioClip GetClip(BGM_STATE state)
        {
            switch (state)
            {
                case BGM_STATE.ENDING:
                    return _endingClip;

                case BGM_STATE.CHASE:
                    return _chaseClip;

                case BGM_STATE.TENSION:
                    return _tensionClip;

                default:
                    return _ambientClip;
            }
        }
    }
}
