using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// 씬의 모든 발전기 표시를 한 번에 켜고 끄는 프로토타입 검증용 컴포넌트.
    /// 머티리얼 1벌을 등록된 모든 <see cref="GeneratorHighlight"/>가 공유한다.
    ///
    /// 표시는 게임당 사용 횟수가 정해진 소모성 능력이다. 1회 사용하면 정해진 시간 동안만 보이고 스스로 꺼진다.
    /// 켜져 있는 동안의 재입력은 무시한다. 실수로 연타해 남은 횟수를 날리는 사고를 막는다.
    ///
    /// 완료된 발전기는 표시하지 않는다. 찾아갈 이유가 없기 때문이다.
    /// 그래서 등록 단위가 <see cref="GeneratorHighlight"/>가 아니라 <see cref="Generator"/>다.
    /// 실루엣만 모으면 그것이 어느 발전기의 것인지, 그 발전기가 완료되었는지 알 방법이 없다.
    ///
    /// TODO: 사용 횟수와 지속 시간을 GDD에 반영할지는 회의 안건이다. GDD 13.1의 인게임 HUD 항목에 위치 표시가 없어
    ///       이 기능 자체가 GDD에 근거 조항이 없다.
    /// TODO: 남은 횟수를 어디에 어떤 형태로 표시할지는 미정이다. 현민 담당 UI Manager가 확정되면
    ///       <see cref="RemainingUseCountChanged"/>를 구독하는 형태로 붙인다.
    /// TODO: 표시 키는 진우 담당 InputActions가 확정되면 그쪽으로 옮긴다. 키 값 자체는 현민 담당 키 설정 시스템으로 간다. GDD 13.2
    ///
    /// 발전기를 직접 참조하지 않고 <see cref="Generator.Enabled"/>, <see cref="Generator.Disabled"/>를
    /// 구독해 등록한다. 그래서 01_Main의 발전기 코드는 프로토타입인 이 클래스를 모른다.
    /// 싱글톤이 아니다. 게임 1판 동안만 의미 있는 상태를 들고 있어 씬과 수명을 같이해야 한다.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public sealed class GeneratorHighlightProvider : MonoBehaviour
    {
        [Tooltip("등록된 모든 발전기가 이 머티리얼을 공유한다. HideSeek/GeneratorHighlight 셰이더를 쓰는 머티리얼을 할당한다.")]
        [SerializeField] private Material _highlightMaterial;

        [Header("표시 입력")]
        [Tooltip("표시를 켜는 키. 켜져 있는 동안의 재입력은 무시한다.")]
        [SerializeField] private Key _activateKey = Key.H;

        [Header("표시 지속")]
        [Tooltip("한 번 사용했을 때 표시가 유지되는 시간(초). 시간이 지나면 자동으로 꺼진다.")]
        [SerializeField , Min(0.1f)] private float _visibleDuration = 5f;

        [Tooltip("한 게임에서 쓸 수 있는 총 횟수. 0이면 사용할 수 없다.")]
        [SerializeField , Min(0)] private int _maxUseCount = 3;

        private readonly GeneratorRegistry REGISTRY = new();

        // Registry는 "아는 발전기"를, 이 목록은 "실루엣이 있는 발전기"를 담는다.
        // 실루엣이 없는 발전기는 Registry에는 남지만 여기에는 들어오지 않는다.
        private readonly List<GeneratorEntry> LIST_ENTRY = new();

        private bool _isVisible;
        private float _visibleRemain;
        private int _remainingUseCount;

        public event Action<int> RemainingUseCountChanged;

        public bool IsVisible => _isVisible;
        public int RemainingUseCount => _remainingUseCount;
        public int MaxUseCount => _maxUseCount;
        public bool CanActivate => _isVisible == false && _remainingUseCount > 0;

        private void Awake()
        {
            _isVisible = false;
            _remainingUseCount = _maxUseCount;

            REGISTRY.Attach(OnGeneratorRegisteredActioned , OnGeneratorUnregisteredActioned);

            if (_highlightMaterial == null)
            {
                Debug.LogError($"[{nameof(GeneratorHighlightProvider)}] 머티리얼이 비어 있어 발전기 표시가 동작하지 않습니다." , this);
            }
        }

        private void OnDestroy()
        {
            REGISTRY.Detach();

            LIST_ENTRY.Clear();
        }

        private void Update()
        {
            TickVisibleDuration(Time.deltaTime);

            Keyboard tKeyboard = Keyboard.current;
            if (tKeyboard == null)
            {
                return;
            }

            if (tKeyboard[ _activateKey ].wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        /// <summary>
        /// 남은 횟수를 1 소모하고 지속 시간 동안 표시를 켠다.
        /// 이미 켜져 있거나 남은 횟수가 없으면 아무것도 하지 않고 false를 돌려준다.
        /// </summary>
        public bool TryActivate()
        {
            if (CanActivate == false)
            {
                return false;
            }

            _remainingUseCount--;
            RemainingUseCountChanged?.Invoke(_remainingUseCount);

            _visibleRemain = _visibleDuration;
            SetVisible(true);

            return true;
        }

        public void ResetUseCount()
        {
            _remainingUseCount = _maxUseCount;
            RemainingUseCountChanged?.Invoke(_remainingUseCount);
        }

        /// <summary>
        /// 표시 상태를 직접 지정한다. <see cref="TryActivate"/>와 달리 횟수를 소모하지 않고,
        /// true로 켜면 지속 시간 제한 없이 유지된다. 튜토리얼처럼 횟수와 무관하게 보여줘야 할 때 쓴다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (isVisible == false)
            {
                // 남은 시간을 지워야 다음 프레임에 타이머가 한 번 더 끄지 않는다.
                _visibleRemain = 0f;
            }

            _isVisible = isVisible;

            for (int i = 0; i < LIST_ENTRY.Count; i++)
            {
                ApplyVisible(LIST_ENTRY[i]);
            }
        }

        // 잔여 시간이 0 이하가 되면 스스로 끈다. 0이면 SetVisible로 직접 켜 둔 상태라 건드리지 않는다.
        private void TickVisibleDuration(float deltaTime)
        {
            if (_visibleRemain <= 0f)
            {
                return;
            }

            _visibleRemain -= deltaTime;

            if (_visibleRemain > 0f)
            {
                return;
            }

            SetVisible(false);
        }

        [ContextMenu("Debug/Activate Highlight")]
        private void ActivateForDebug()
        {
            TryActivate();
        }

        [ContextMenu("Debug/Reset Use Count")]
        private void ResetUseCountForDebug()
        {
            ResetUseCount();
        }

        // 완료 신호는 RepairStopped로 받는다. Completed는 인자가 없어 어느 발전기인지 알 수 없다.
        private void OnRepairStoppedActioned(Generator generator)
        {
            if (generator == null || generator.State != GENERATOR_STATE.COMPLETED)
            {
                return;
            }

            int tIndex = IndexOf(generator);
            if (tIndex >= 0)
            {
                ApplyVisible(LIST_ENTRY[tIndex]);
            }
        }

        // 나중에 켜진 발전기도 현재 표시 상태를 그대로 따라간다.
        private void OnGeneratorRegisteredActioned(Generator generator)
        {
            GeneratorHighlight tHighlight = generator.GetComponentInChildren<GeneratorHighlight>(true);
            if (tHighlight == null)
            {
                Debug.LogWarning($"[{nameof(GeneratorHighlightProvider)}] '{generator.name}'에 {nameof(GeneratorHighlight)}가 없어 위치 표시 대상에서 제외됩니다." , generator);
                return;
            }

            LIST_ENTRY.Add(new GeneratorEntry(generator , tHighlight));
            generator.RepairStopped += OnRepairStoppedActioned;

            tHighlight.SetMaterial(_highlightMaterial);
            ApplyVisible(LIST_ENTRY[LIST_ENTRY.Count - 1]);
        }

        private void OnGeneratorUnregisteredActioned(Generator generator)
        {
            int tIndex = IndexOf(generator);
            if (tIndex < 0)
            {
                return;
            }

            generator.RepairStopped -= OnRepairStoppedActioned;

            LIST_ENTRY.RemoveAt(tIndex);
        }

        // 완료된 발전기는 찾아갈 이유가 없으므로 표시에서 뺀다.
        private void ApplyVisible(GeneratorEntry entry)
        {
            if (entry.HIGHLIGHT == null)
            {
                return;
            }

            bool tIsRepairNeeded = entry.GENERATOR != null && entry.GENERATOR.State != GENERATOR_STATE.COMPLETED;

            entry.HIGHLIGHT.SetVisible(_isVisible && tIsRepairNeeded);
        }

        private int IndexOf(Generator generator)
        {
            for (int i = 0; i < LIST_ENTRY.Count; i++)
            {
                if (ReferenceEquals(LIST_ENTRY[i].GENERATOR , generator))
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct GeneratorEntry
        {
            public readonly Generator GENERATOR;
            public readonly GeneratorHighlight HIGHLIGHT;

            public GeneratorEntry(Generator generator , GeneratorHighlight highlight)
            {
                GENERATOR = generator;
                HIGHLIGHT = highlight;
            }
        }
    }
}
