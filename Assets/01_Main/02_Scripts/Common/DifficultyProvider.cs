using UnityEngine;

namespace HideSeek.Common
{
    public enum GAME_DIFFICULTY
    {
        EASY,
        NORMAL,
        HARD
    }

    /// <summary>
    /// 선택한 난이도를 씬 너머로 전달한다.
    ///
    /// 타이틀 씬에서 <see cref="Select"/>로 한 번 넣어두면, 난이도를 쓰는 쪽은 각자
    /// <see cref="Current"/>를 읽어 값을 정한다. 난이도를 넘기는 참조선을 따로 만들지 않는다.
    ///
    /// 타이틀을 거치지 않고 인게임 씬을 바로 실행할 때가 많아 기본값을 NORMAL로 둔다.
    /// </summary>
    public static class DifficultyProvider
    {
        public const GAME_DIFFICULTY DEFAULT_DIFFICULTY = GAME_DIFFICULTY.NORMAL;

        public static GAME_DIFFICULTY Current { get; private set; } = DEFAULT_DIFFICULTY;

        public static void Select(GAME_DIFFICULTY difficulty)
        {
            Current = difficulty;

            Debug.Log($"[DifficultyProvider] 난이도 선택: {Current}");
        }

        // static 값은 플레이 종료 후에도 남는다. Enter Play Mode 설정에 따라 도메인 리로드가
        // 생략되면 이전 플레이에서 고른 난이도가 그대로 이어지므로 여기서 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = DEFAULT_DIFFICULTY;
        }
    }
}
