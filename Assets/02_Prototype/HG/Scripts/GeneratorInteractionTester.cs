using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HideSeek.Generators
{
    /// <summary>
    /// 프로토타입 검증용 임시 상호작용 입력.
    /// Raycast 상호작용(진우 담당)이 들어오면 이 컴포넌트는 삭제하고
    /// <see cref="Generator.TryBeginRepair"/>와 <see cref="Generator.CancelRepair"/>를 직접 호출한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratorInteractionTester : MonoBehaviour
    {
        [SerializeField] private Generator _generator;
        [SerializeField] private Key _repairKey = Key.E;

#if UNITY_EDITOR
        private void Reset()
        {
            _generator = GetComponentInParent<Generator>();
        }
#endif

        private void Awake()
        {
            // 인스펙터 연결을 잊어도 같은 발전기 안에서 찾는다. 다른 발전기를 가리켜야 할 때만 인스펙터로 지정한다.
            if (_generator == null)
            {
                _generator = GetComponentInParent<Generator>();
            }
        }

        private void Update()
        {
            if (_generator == null)
            {
                return;
            }

            Keyboard tKeyboard = Keyboard.current;
            if (tKeyboard == null)
            {
                return;
            }

            KeyControl tRepairKeyControl = tKeyboard[ _repairKey ];

            if (tRepairKeyControl.wasPressedThisFrame)
            {
                _generator.TryBeginRepair();
                return;
            }

            // 키를 떼면 상호작용 유지가 끊긴 것으로 보고 작업을 중단한다. GDD 7.3.2, 7.3.6
            if (tRepairKeyControl.wasReleasedThisFrame)
            {
                _generator.CancelRepair();
            }
        }
    }
}
