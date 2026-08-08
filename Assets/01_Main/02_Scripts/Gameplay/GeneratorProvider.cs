using System.Collections.Generic;
using HideSeek.AI;
using HideSeek.Generators;
using UnityEngine;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 후보 지점 중 이번 게임에 쓸 발전기를 골라 활성화한다. GDD 7.1
    ///
    /// 생성하지 않고 <see cref="GameObject.SetActive"/>만 한다. <see cref="Generator.Enabled"/>와
    /// <see cref="Generator.Disabled"/>가 OnEnable/OnDisable에서 발행되므로, 켜는 것만으로
    /// QTE·위치 표시·진행도 쪽 등록이 전부 따라온다.
    ///
    /// Config 배포와 완료 집계도 여기서 한다. 셋 다 <see cref="Generator.Enabled"/> 구독 하나로 해결되므로
    /// 컴포넌트를 나누면 구독과 씬 배선만 늘어난다. 발전기마다 <see cref="GameProgressProvider"/> 참조를
    /// 들고 있으면 후보 지점 활성화 구조에서 배선할 방법이 없어, 발전기 목록을 아는 이쪽이 대신 집계한다.
    ///
    /// 후보가 요청 수보다 적으면 <see cref="GameProgressProvider.SetRequiredGeneratorCount"/>로 실제 수를 알린다.
    /// 그러지 않으면 남은 발전기를 다 고쳐도 클리어 조건이 영원히 만족되지 않는다.
    ///
    /// 켜고 끄는 대상은 <see cref="GeneratorCandidatePoint"/>가 붙은 발전기뿐이다. 마커를 뗀 인스턴스는
    /// 배치 그대로 두되 Config는 똑같이 받는다. 그 수는 <see cref="ReportUnmanagedGenerators"/>로 알린다.
    ///
    /// TODO: 난이도 선택이 생기면 Config를 인스펙터 고정값이 아니라 그쪽에서 받는다. GDD 12
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public sealed class GeneratorProvider : MonoBehaviour
    {
        [Tooltip("활성화할 발전기 수를 여기서 받는다. 비워두면 Awake에서 씬을 검색한다.")]
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Tooltip("활성 여부와 무관하게 등록된 모든 발전기가 이 설정을 공유한다.")]
        [SerializeField] private GeneratorConfig _generatorConfig;

        [Tooltip("끄면 Zone 중복을 신경 쓰지 않고 무작위로만 고른다. 디버그용이다.")]
        [SerializeField] private bool _isZoneSpreadEnabled = true;

        private readonly GeneratorRegistry REGISTRY = new();
        private readonly List<GeneratorCandidatePoint> LIST_SELECTED = new();

        public IReadOnlyList<GeneratorCandidatePoint> SelectedCandidates => LIST_SELECTED;

        private void Awake()
        {
            if (_gameProgressProvider == null)
            {
                _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            }

            REGISTRY.Attach(OnGeneratorRegisteredActioned , OnGeneratorUnregisteredActioned);

            if (_generatorConfig == null)
            {
                Debug.LogError($"[{nameof(GeneratorProvider)}] GeneratorConfig가 비어 있어 발전기가 동작하지 않습니다." , this);
            }
        }

        // 다른 Provider들이 Awake에서 구독을 마친 뒤에 켜야 등록을 놓치지 않는다.
        private void Start()
        {
            ActivateCandidates();
        }

        private void OnDestroy()
        {
            REGISTRY.Detach();

            LIST_SELECTED.Clear();
        }

        /// <summary>
        /// 후보를 다시 골라 활성화한다. 같은 씬에서 게임을 다시 시작할 때 호출한다.
        /// </summary>
        [ContextMenu("Debug/Activate Candidates")]
        public void ActivateCandidates()
        {
            LIST_SELECTED.Clear();

            // 후보는 비활성 상태로 배치되므로 Include로 찾아야 한다.
            GeneratorCandidatePoint[] tArr_candidate = FindObjectsByType<GeneratorCandidatePoint>(FindObjectsInactive.Include , FindObjectsSortMode.None);

            List<GeneratorCandidatePoint> tList_available = CollectAvailableCandidates(tArr_candidate);
            int tRequestedCount = _gameProgressProvider != null ? _gameProgressProvider.RequiredGeneratorCount : 0;

            SelectCandidates(tList_available , tRequestedCount);
            ApplyActivation(tArr_candidate);
            NotifyActualCount(tRequestedCount);
            ReportUnmanagedGenerators();
        }

        private List<GeneratorCandidatePoint> CollectAvailableCandidates(GeneratorCandidatePoint[] arr_candidate)
        {
            List<GeneratorCandidatePoint> tList_available = new();

            for (int i = 0; i < arr_candidate.Length; i++)
            {
                if (arr_candidate[i].IsAvailable && arr_candidate[i].GetComponent<Generator>() != null)
                {
                    tList_available.Add(arr_candidate[i]);
                }
            }

            Shuffle(tList_available);

            return tList_available;
        }

        // 1차로 Zone이 겹치지 않게 채우고, 모자라면 2차로 남은 후보에서 채운다. GDD 7.1
        private void SelectCandidates(List<GeneratorCandidatePoint> availableCandidates , int requestedCount)
        {
            if (_isZoneSpreadEnabled)
            {
                AIWorldZone[] tArr_zone = FindObjectsByType<AIWorldZone>(FindObjectsInactive.Include , FindObjectsSortMode.None);
                HashSet<int> tSet_usedZoneId = new();

                for (int i = 0; i < availableCandidates.Count && LIST_SELECTED.Count < requestedCount; i++)
                {
                    AIWorldZone tZone = ResolveZone(availableCandidates[i] , tArr_zone);

                    // Zone을 못 찾은 후보는 중복 판정을 할 수 없으니 2차로 미룬다.
                    if (tZone == null || tSet_usedZoneId.Add(tZone.ZoneId) == false)
                    {
                        continue;
                    }

                    LIST_SELECTED.Add(availableCandidates[i]);
                }
            }

            for (int i = 0; i < availableCandidates.Count && LIST_SELECTED.Count < requestedCount; i++)
            {
                if (LIST_SELECTED.Contains(availableCandidates[i]) == false)
                {
                    LIST_SELECTED.Add(availableCandidates[i]);
                }
            }
        }

        // 선택되지 않은 후보는 이전 게임에서 켜져 있었을 수 있으므로 명시적으로 끈다.
        private void ApplyActivation(GeneratorCandidatePoint[] arr_candidate)
        {
            for (int i = 0; i < arr_candidate.Length; i++)
            {
                arr_candidate[i].gameObject.SetActive(LIST_SELECTED.Contains(arr_candidate[i]));
            }
        }

        private void NotifyActualCount(int requestedCount)
        {
            if (LIST_SELECTED.Count >= requestedCount)
            {
                return;
            }

            Debug.LogWarning($"[{nameof(GeneratorProvider)}] 후보가 모자라 {requestedCount}개 중 {LIST_SELECTED.Count}개만 활성화했습니다. 클리어 조건을 실제 수에 맞춥니다. GDD 7.1은 후보를 5개 이상 두라고 정합니다." , this);

            if (_gameProgressProvider != null)
            {
                _gameProgressProvider.SetRequiredGeneratorCount(LIST_SELECTED.Count);
            }
        }

        /// <summary>
        /// 마커가 없어 이 Provider의 관리 밖에 있는 발전기를 알린다.
        /// 튜토리얼처럼 일부러 제거한 경우도 있어 경고가 아니라 정보로 남긴다.
        /// 관리 밖 발전기도 완료하면 진행도에 집계되므로, 실수로 제거했다면 클리어 조건이 헐거워진다.
        /// </summary>
        private void ReportUnmanagedGenerators()
        {
            int tUnmanagedCount = 0;

            // 등록된 발전기가 곧 활성 발전기다. 이 시점에는 켜진 것은 등록되고 꺼진 것은 해제된 뒤다.
            IReadOnlyList<Generator> tList_generator = REGISTRY.Generators;
            for (int i = 0; i < tList_generator.Count; i++)
            {
                if (tList_generator[i].GetComponent<GeneratorCandidatePoint>() == null)
                {
                    tUnmanagedCount++;
                }
            }

            if (tUnmanagedCount == 0)
            {
                return;
            }

            Debug.Log($"[{nameof(GeneratorProvider)}] 후보 관리 밖에서 켜져 있는 발전기 {tUnmanagedCount}개를 찾았습니다. 의도한 예외가 아니면 {nameof(GeneratorCandidatePoint)}가 제거되지 않았는지 확인하세요." , this);
        }

        private AIWorldZone ResolveZone(GeneratorCandidatePoint candidate , AIWorldZone[] arr_zone)
        {
            if (candidate.Zone != null)
            {
                return candidate.Zone;
            }

            for (int i = 0; i < arr_zone.Length; i++)
            {
                if (arr_zone[i].Contains(candidate.transform.position))
                {
                    return arr_zone[i];
                }
            }

            return null;
        }

        private void Shuffle(List<GeneratorCandidatePoint> candidates)
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int tSwapIndex = Random.Range(0 , i + 1);

                (candidates[i], candidates[tSwapIndex]) = (candidates[tSwapIndex], candidates[i]);
            }
        }

        // 나중에 켜진 발전기도 같은 Config를 받는다.
        private void OnGeneratorRegisteredActioned(Generator generator)
        {
            generator.SetConfig(_generatorConfig);
            generator.Completed += OnGeneratorCompletedActioned;
        }

        private void OnGeneratorUnregisteredActioned(Generator generator)
        {
            generator.Completed -= OnGeneratorCompletedActioned;
        }

        // Completed는 인자가 없지만 완료 수만 세면 되므로 문제되지 않는다.
        // 완료는 발전기 1기당 한 번만 발생하므로 중복 집계도 없다.
        private void OnGeneratorCompletedActioned()
        {
            if (_gameProgressProvider == null)
            {
                return;
            }

            _gameProgressProvider.NotifyGeneratorCompleted();
        }
    }
}
