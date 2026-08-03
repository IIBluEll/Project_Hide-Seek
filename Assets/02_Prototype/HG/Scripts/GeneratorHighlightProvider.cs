using System.Collections.Generic;
using HM.CodeBase;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// 씬의 모든 발전기 표시를 한 번에 켜고 끄는 프로토타입 검증용 싱글톤.
    /// 머티리얼 1벌을 등록된 모든 <see cref="GeneratorHighlight"/>가 공유한다.
    ///
    /// TODO: 표시 On/Off를 무엇에 연결할지는 기획 미정이다. GDD 13.1의 인게임 HUD 항목에 위치 표시가 없다.
    ///       고정 키, 소모품, 능력 중 무엇으로 확정되든 이 클래스의 키 입력을 지우고
    ///       해당 시스템이 <see cref="SetVisible"/>을 호출하는 형태로 바꾼다.
    /// TODO: 표시 키는 진우 담당 InputActions가 확정되면 그쪽으로 옮긴다. 키 값 자체는 현민 담당 키 설정 시스템으로 간다. GDD 13.2
    ///
    /// 발전기를 직접 참조하지 않고 <see cref="GeneratorHighlight.Enabled"/>, <see cref="GeneratorHighlight.Disabled"/>를
    /// 구독해 등록한다. 그래서 01_Main의 발전기 코드는 프로토타입인 이 클래스를 모른다.
    /// <see cref="ASingletone{T}.Instance"/>는 인스턴스가 없으면 GameObject를 새로 만들므로 쓰지 않는다.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public sealed class GeneratorHighlightProvider : ASingletone<GeneratorHighlightProvider>
    {
        [Tooltip("등록된 모든 발전기가 이 머티리얼을 공유한다. HideSeek/GeneratorHighlight 셰이더를 쓰는 머티리얼을 할당한다.")]
        [SerializeField] private Material _highlightMaterial;

        [Header("표시 전환 입력")]
        [SerializeField] private Key _toggleKey = Key.H;
        [SerializeField] private bool _isVisibleOnStart;

        private readonly List<GeneratorHighlight> LIST_HIGHLIGHT = new();

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public override void Awake()
        {
            base.Awake();

            _isVisible = _isVisibleOnStart;

            GeneratorHighlight.Enabled += OnHighlightEnabledActioned;
            GeneratorHighlight.Disabled += OnHighlightDisabledActioned;

            // 이 Provider보다 먼저 활성화된 발전기는 이벤트를 놓쳤으므로 여기서 훑는다.
            GeneratorHighlight[] tArr_highlight = FindObjectsByType<GeneratorHighlight>(FindObjectsSortMode.None);
            for (int i = 0; i < tArr_highlight.Length; i++)
            {
                Register(tArr_highlight[i]);
            }

            if (_highlightMaterial == null)
            {
                Debug.LogError($"[{nameof(GeneratorHighlightProvider)}] 머티리얼이 비어 있어 발전기 표시가 동작하지 않습니다." , this);
            }
        }

        private void OnDestroy()
        {
            // 먼저 끊어야 남은 발전기의 OnDisable이 이 인스턴스를 다시 건드리지 않는다.
            GeneratorHighlight.Enabled -= OnHighlightEnabledActioned;
            GeneratorHighlight.Disabled -= OnHighlightDisabledActioned;

            LIST_HIGHLIGHT.Clear();
        }

        private void Update()
        {
            Keyboard tKeyboard = Keyboard.current;
            if (tKeyboard == null)
            {
                return;
            }

            if (tKeyboard[ _toggleKey ].wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            SetVisible(_isVisible == false);
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            for (int i = 0; i < LIST_HIGHLIGHT.Count; i++)
            {
                if (LIST_HIGHLIGHT[i] != null)
                {
                    LIST_HIGHLIGHT[i].SetVisible(isVisible);
                }
            }
        }

        private void OnHighlightEnabledActioned(GeneratorHighlight highlight)
        {
            Register(highlight);
        }

        private void OnHighlightDisabledActioned(GeneratorHighlight highlight)
        {
            if (highlight != null)
            {
                LIST_HIGHLIGHT.Remove(highlight);
            }
        }

        // 나중에 켜진 발전기도 현재 표시 상태를 그대로 따라간다.
        private void Register(GeneratorHighlight highlight)
        {
            if (highlight == null || LIST_HIGHLIGHT.Contains(highlight))
            {
                return;
            }

            LIST_HIGHLIGHT.Add(highlight);
            highlight.SetMaterial(_highlightMaterial);
            highlight.SetVisible(_isVisible);
        }
    }
}
