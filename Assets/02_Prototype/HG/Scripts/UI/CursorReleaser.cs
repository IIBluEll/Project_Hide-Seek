using UnityEngine;

namespace HideSeek.UI
{
    /// <summary>
    /// 이 오브젝트가 켜져 있는 동안 커서 잠금을 푼다.
    ///
    /// 인게임에서는 PlayerController가 커서를 잠그므로, 버튼이 있는 화면은
    /// 스스로 커서를 확보해야 클릭이 가능하다. 사망·탈출 결과 화면 양쪽에 붙인다.
    ///
    /// 결과 화면 루트에 붙인다. 화면을 켜고 끄는 쪽이 아니라 화면 자신이 커서를 책임진다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CursorReleaser : MonoBehaviour
    {
        private CursorLockMode _previousLockMode;
        private bool _wasCursorVisible;

        private void OnEnable()
        {
            _previousLockMode = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _wasCursorVisible;
        }
    }
}
