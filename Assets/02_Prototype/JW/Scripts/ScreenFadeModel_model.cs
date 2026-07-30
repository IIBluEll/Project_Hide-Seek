public sealed class ScreenFadeModel_model
{
    private readonly float FADE_DURATION;

    public float FadeDuration => FADE_DURATION;
    public bool IsTransitioning { get; private set; }

    public ScreenFadeModel_model(float fadeDuration)
    {
        FADE_DURATION = fadeDuration > 0f ? fadeDuration : 0f;
    }

    public bool TryBeginTransition()
    {
        if (IsTransitioning)
            return false;

        IsTransitioning = true;
        return true;
    }

    public void EndTransition()
    {
        IsTransitioning = false;
    }
}
