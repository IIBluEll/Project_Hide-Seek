namespace HideSeek.Common
{
    /// <summary>
    /// 씬 전환에 쓰는 씬 이름.
    ///
    /// 값은 씬 파일 이름과 정확히 같아야 하고, 그 씬이 Build Settings에 등록되어 있어야 이동할 수 있다.
    /// 씬 파일 이름을 바꿀 때는 여기 값도 같이 바꾼다.
    /// </summary>
    public static class SceneNames
    {
        public const string TITLE = "TitleScene";
        public const string TUTORIAL = "TutorialScene";

        // 게임 시작과 재시작은 인게임 씬으로 바로 가지 않고 이 씬을 거친다. GDD 4.1
        public const string LOADING = "LoadingScene";

        public const string IN_GAME = "InGame_Map";
    }
}
