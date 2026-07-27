using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// <see cref="Keyboard.current"/>를 직접 읽는 임시 QTE 입력 어댑터.
    /// 프로젝트의 Active Input Handling이 Input System Package로 설정되어 있어 레거시 Input은 사용하지 않는다.
    /// </summary>
    public sealed class KeyboardQteInputSource : IQteInputSource
    {
        private readonly Key QTE_KEY;
        private readonly string QTE_KEY_LABEL;

        public KeyboardQteInputSource(Key qteKey , string qteKeyLabel)
        {
            QTE_KEY = qteKey;
            QTE_KEY_LABEL = qteKeyLabel;
        }

        public bool IsQteKeyDown()
        {
            Keyboard tKeyboard = Keyboard.current;
            if (tKeyboard == null)
                return false;

            return tKeyboard[QTE_KEY].wasPressedThisFrame;
        }

        public string GetQteKeyLabel()
        {
            return QTE_KEY_LABEL;
        }
    }
}
