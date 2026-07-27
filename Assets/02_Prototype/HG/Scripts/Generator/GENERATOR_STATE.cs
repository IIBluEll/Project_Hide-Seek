namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 상태. GAME_DESIGN_DOCUMENT.md 7.2 발전기 상태를 그대로 따른다.
    /// </summary>
    public enum GENERATOR_STATE
    {
        /// <summary>상호작용 전 또는 작업 중단 상태. 유예 시간 후 진행도가 감소한다.</summary>
        INACTIVE,

        /// <summary>플레이어가 수리 중인 상태.</summary>
        INTERACTING,

        /// <summary>수리가 끝난 상태. 진행도가 감소하지 않는다.</summary>
        COMPLETED
    }
}
