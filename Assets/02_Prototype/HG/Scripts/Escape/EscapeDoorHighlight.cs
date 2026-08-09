using HideSeek.Generators;
using UnityEngine;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 발전기 수리가 모두 끝난 시점부터 탈출 문을 실루엣으로 계속 표시한다.
    ///
    /// 발전기 표시와 달리 소모성이 아니다. 키 입력도 지속 시간도 없다.
    /// 조건이 충족되면 켜지고, <see cref="GameProgressProvider.ResetProgress"/>로 진행도가 되돌아가면 꺼진다.
    /// <see cref="EscapeDoorLock"/>이 문을 여는 조건과 같은 조건이므로 두 컴포넌트가 같은 시점에 반응한다.
    ///
    /// 실루엣을 만드는 일은 <see cref="GeneratorHighlight"/>가 한다. 이 컴포넌트는 언제 켤지만 정한다.
    /// 문 패널의 Renderer에 붙여 두면 문이 열리며 움직여도 실루엣이 따라간다.
    ///
    /// <see cref="EscapeDoorLock"/>이 아니라 <see cref="GameProgressProvider"/>를 직접 본다.
    /// 표시 조건은 "문이 열렸는가"가 아니라 "발전기를 모두 고쳤는가"이고, 잠금 컴포넌트가 없는 배치에서도 표시는 되어야 한다.
    ///
    /// TODO: 문에 붙는 컴포넌트 이름이 <see cref="GeneratorHighlight"/>인 것은 이름이 맞지 않는다.
    ///       표시 대상이 발전기 밖으로 늘어났으므로 범용 이름으로 바꾸는 것을 팀에 안건으로 올린다.
    ///       발전기 프리팹이 이미 참조하고 있어 이번 작업 범위에서는 건드리지 않았다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EscapeDoorHighlight : MonoBehaviour
    {
        [Tooltip("비워두면 Awake에서 씬을 검색한다.")]
        [SerializeField] private GameProgressProvider _gameProgressProvider;

        [Tooltip("비워두면 이 오브젝트와 자식에서 찾는다. 문 모델의 Renderer보다 상위에 있어야 한다.")]
        [SerializeField] private GeneratorHighlight _doorHighlight;

        [Tooltip("발전기와 같은 머티리얼을 넣으면 같은 색으로 보인다. 색을 구분하려면 별도 머티리얼을 만든다.")]
        [SerializeField] private Material _highlightMaterial;

        public bool IsVisible => _doorHighlight != null && _doorHighlight.IsVisible;

#if UNITY_EDITOR
        private void Reset()
        {
            _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            _doorHighlight = GetComponentInChildren<GeneratorHighlight>(true);
        }
#endif

        private void Awake()
        {
            // 프리팹을 씬에 끌어다 놓으면 Reset이 돌지 않으므로 여기서 한 번 더 받쳐준다.
            if (_gameProgressProvider == null)
            {
                _gameProgressProvider = FindFirstObjectByType<GameProgressProvider>();
            }

            if (_doorHighlight == null)
            {
                _doorHighlight = GetComponentInChildren<GeneratorHighlight>(true);
            }

            // 아래 셋 중 하나라도 없으면 표시할 방법이 없다. 표시가 안 되어도 탈출은 막히지 않으므로
            // 진행을 멈추지 않고 컴포넌트만 끈다. 오류 로그로 잘못된 배선을 바로 알 수 있다.
            if (_doorHighlight == null)
            {
                Debug.LogError($"[{nameof(EscapeDoorHighlight)}] 표시할 {nameof(GeneratorHighlight)}가 없습니다. 문 모델보다 상위에 붙여야 합니다." , this);
                enabled = false;

                return;
            }

            if (_highlightMaterial == null)
            {
                Debug.LogError($"[{nameof(EscapeDoorHighlight)}] 머티리얼이 비어 있어 문을 표시할 수 없습니다." , this);
                enabled = false;

                return;
            }

            if (_gameProgressProvider == null)
            {
                Debug.LogError($"[{nameof(EscapeDoorHighlight)}] {nameof(GameProgressProvider)}를 찾지 못해 표시 시점을 알 수 없습니다." , this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // Awake가 배선 실패로 이 컴포넌트를 껐다면 인스펙터에서 다시 켜도 참조는 비어 있다.
            if (_gameProgressProvider == null || _doorHighlight == null || _highlightMaterial == null)
            {
                return;
            }

            _gameProgressProvider.AllGeneratorsCompleted += OnAllGeneratorsCompletedActioned;
            _gameProgressProvider.CompletedGeneratorCountChanged += OnCompletedGeneratorCountChangedActioned;

            _doorHighlight.SetMaterial(_highlightMaterial);

            ApplyVisible();
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
            ApplyVisible();

            Debug.Log($"[{nameof(EscapeDoorHighlight)}] 발전기 수리가 모두 끝나 탈출 문 표시를 켰습니다." , this);
        }

        // ResetProgress로 진행도가 되돌아가면 표시도 끈다. 한 방향으로만 켜지면
        // 같은 씬에서 다시 시작했을 때 문이 표시된 채로 남는다.
        private void OnCompletedGeneratorCountChangedActioned(int completedGeneratorCount)
        {
            ApplyVisible();
        }

        private void ApplyVisible()
        {
            _doorHighlight.SetVisible(_gameProgressProvider.AreAllGeneratorsCompleted);
        }
    }
}
