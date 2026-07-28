namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 1회가 시작될 때 UI에 넘기는 표시용 값. 판정은 <see cref="QteRunner"/>가 한다.
    /// </summary>
    public readonly struct QteChallenge
    {
        public readonly float ZONE_START_01; // 게이지 전체를 0~1로 본 성공 구간 시작
        public readonly float ZONE_END_01;
        public readonly string KEY_LABEL;

        public QteChallenge(float zoneStart01 , float zoneEnd01 , string keyLabel)
        {
            ZONE_START_01 = zoneStart01;
            ZONE_END_01 = zoneEnd01;
            KEY_LABEL = keyLabel;
        }
    }
}
