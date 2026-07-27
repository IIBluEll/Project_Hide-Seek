namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 1회가 시작될 때 UI에 전달하는 표시 정보.
    /// 판정 자체는 <see cref="QteRunner"/>가 담당하며, 이 구조체는 표시용 값만 가진다.
    /// </summary>
    public readonly struct QteChallenge
    {
        /// <summary>성공 구간 시작 지점. 게이지 전체를 0~1로 정규화한 값이다.</summary>
        public readonly float ZONE_START_01;

        /// <summary>성공 구간 종료 지점. 게이지 전체를 0~1로 정규화한 값이다.</summary>
        public readonly float ZONE_END_01;

        /// <summary>화면에 표시할 입력 키 이름.</summary>
        public readonly string KEY_LABEL;

        public QteChallenge(float zoneStart01 , float zoneEnd01 , string keyLabel)
        {
            ZONE_START_01 = zoneStart01;
            ZONE_END_01 = zoneEnd01;
            KEY_LABEL = keyLabel;
        }
    }
}
