using System;

namespace HideSeek.Generators
{
    /// <summary>
    /// QTE UI가 표시에 사용하는 읽기 전용 발전기 데이터.
    /// <see cref="Generator"/>가 직접 구현하므로 UI 전용 Model 클래스를 따로 두지 않는다.
    ///
    /// Presenter는 이 인터페이스만 참조한다. 수리 시작이나 취소 같은 게임플레이 조작이
    /// 포함되지 않으므로 UI가 게임 로직을 바꿀 수 없다. GDD 16.2
    ///
    /// TODO: 인터페이스 이름은 회의에서 확정한다. 현재 이름은 임시다.
    /// </summary>
    public interface IGeneratorQteModel
    {
        /// <summary>현재 수리 진행도. 0에서 1 사이다.</summary>
        float Progress01 { get; }

        /// <summary>수리를 시작했다. 인자는 시작한 발전기다.</summary>
        event Action<IGeneratorQteModel> RepairStarted;

        /// <summary>수리가 중단되거나 완료됐다. 인자는 해당 발전기다.</summary>
        event Action<IGeneratorQteModel> RepairStopped;

        /// <summary>수리 진행도가 변경됐다. 인자는 0~1 진행도다.</summary>
        event Action<float> ProgressChanged;

        /// <summary>QTE가 시작됐다.</summary>
        event Action<QteChallenge> QteStarted;

        /// <summary>QTE 인디케이터 위치가 갱신됐다. 인자는 0~1 위치다.</summary>
        event Action<float> QteIndicatorChanged;

        /// <summary>QTE 판정이 끝났다.</summary>
        event Action<QTE_RESULT> QteFinished;
    }
}
