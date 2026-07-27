namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 입력만 담당하는 최소 추상화.
    /// 플레이어 입력 담당자의 InputActions 구조가 확정되면 이 인터페이스의 구현만 교체한다.
    /// 발전기 상호작용 시작과 취소는 이 인터페이스가 아니라
    /// <see cref="Generator.TryBeginRepair"/>, <see cref="Generator.CancelRepair"/> 호출로 전달한다.
    /// </summary>
    public interface IQteInputSource
    {
        /// <summary>이번 프레임에 QTE 입력 키가 눌렸는지 여부.</summary>
        bool IsQteKeyDown();

        /// <summary>화면에 표시할 QTE 입력 키 이름.</summary>
        string GetQteKeyLabel();
    }
}
