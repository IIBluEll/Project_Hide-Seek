using System;

namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 1회가 시작될 때 UI에 넘기는 표시용 값. 판정은 <see cref="QteRunner"/>가 한다.
    /// </summary>
    public readonly struct QteChallenge
    {
        public readonly float ZONE_START_01; // 게이지 전체를 0~1로 본 성공 구간 시작
        public readonly float ZONE_END_01;

        public QteChallenge(float zoneStart01, float zoneEnd01)
        {
            ZONE_START_01 = zoneStart01;
            ZONE_END_01 = zoneEnd01;
        }
    }
    
    /// <summary>
    /// QTE UI가 값을 읽는 창구. <see cref="Generator"/>가 구현한다.
    /// 여기의 이벤트는 QTE 1회 수명이라, 발전기 수명인 <see cref="IGeneratorProgressModel"/>과 분리했다.
    ///
    /// TODO: 인터페이스 이름은 회의에서 확정한다. 현재 이름은 임시다.
    /// </summary>
    public interface IGeneratorQteModel
    {
        event Action<QteChallenge> QteStarted;

        event Action<float> QteIndicatorChanged; // 인자는 인디케이터 위치(0~1)

        event Action<QTE_RESULT> QteFinished;
    }
}
