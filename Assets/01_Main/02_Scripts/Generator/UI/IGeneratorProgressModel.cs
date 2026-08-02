using System;

namespace HideSeek.Generators
{
    /// <summary>
    /// 수리 진행도 UI가 값을 읽는 창구. <see cref="Generator"/>가 구현한다.
    /// 조작 메서드를 포함하지 않으므로 UI가 게임 로직을 바꿀 수 없다. GDD 16.2
    ///
    /// TODO: 인터페이스 이름은 회의에서 확정한다. 현재 이름은 임시다.
    /// </summary>
    public interface IGeneratorProgressModel
    {
        float Progress01 { get; } // 현재 수리 진행도(0~1)

        event Action<float> ProgressChanged;
    }
}
