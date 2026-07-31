using UnityEngine.InputSystem;

namespace HideSeek.Generators
{
    /// <summary>
    /// 플레이어 입력 담당자의 InputActions 구조가 확정되면 이 인터페이스의 구현만 교체한다.
    /// 수리 시작과 취소는 여기가 아니라 <see cref="Generator.TryBeginRepair"/>,
    /// <see cref="Generator.CancelRepair"/> 호출로 전달한다.
    /// </summary>
    public interface IInputSource
    {
        bool IsQteKeyDown(); // 이번 프레임에 눌렸는지
        string GetQteKeyLabel(); // 화면에 표시할 키 이름
    }

    /// <summary>
    /// 프로젝트의 Active Input Handling이 Input System Package라 레거시 Input은 쓸 수 없다.
    /// </summary>
    public sealed class KeyboardInputSource : IInputSource
    {
        private readonly Key INPUT_KEY;

        public KeyboardInputSource(Key inputKey)
        {
            INPUT_KEY = inputKey;
        }

        public bool IsQteKeyDown()
        {
            Keyboard tKeyboard = Keyboard.current;
            if (tKeyboard == null)
                return false;

            return tKeyboard[INPUT_KEY].wasPressedThisFrame;
        }

        public string GetQteKeyLabel()
        {
            return INPUT_KEY.ToString();
        }
    }
}
