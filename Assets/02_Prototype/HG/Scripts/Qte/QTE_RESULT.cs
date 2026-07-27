namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 1회의 판정 결과.
    /// </summary>
    public enum QTE_RESULT
    {
        /// <summary>성공 구간 안에서 입력했다.</summary>
        SUCCESS,

        /// <summary>성공 구간 밖에서 입력했거나, 입력하지 않아 인디케이터가 끝까지 이동했다.</summary>
        FAILURE
    }
}
