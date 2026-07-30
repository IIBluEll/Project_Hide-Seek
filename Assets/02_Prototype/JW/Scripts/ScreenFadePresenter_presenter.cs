using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HM.CodeBase;
using UnityEngine;

public sealed class ScreenFadePresenter_presenter : APresenter
{
    private readonly ScreenFadeModel_model SCREEN_FADE_MODEL;
    private readonly ScreenFade_view SCREEN_FADE_VIEW;

    public bool IsTransitioning => SCREEN_FADE_MODEL.IsTransitioning;

    public ScreenFadePresenter_presenter(
        ScreenFadeModel_model screenFadeModel,
        ScreenFade_view screenFadeView)
    {
        SCREEN_FADE_MODEL = screenFadeModel;
        SCREEN_FADE_VIEW = screenFadeView;
    }

    public override void Open()
    {
        SCREEN_FADE_VIEW.Open();
        SCREEN_FADE_VIEW.Clear();
    }

    public override void Close()
    {
        SCREEN_FADE_VIEW.Clear();
        SCREEN_FADE_VIEW.Close();
    }

    public override void Dispose()
    {
        SCREEN_FADE_MODEL.EndTransition();
        SCREEN_FADE_VIEW.Clear();
    }

    public async UniTask<bool> PlayTransition_async(
        Action onFadeOutCompleted,
        CancellationToken cancellationToken)
    {
        if (!SCREEN_FADE_MODEL.TryBeginTransition())
            return false;

        bool transitionActionCompleted = false;
        SCREEN_FADE_VIEW.SetInputBlocking(true);

        try
        {
            await Fade_async(1f, cancellationToken);

            onFadeOutCompleted?.Invoke();
            transitionActionCompleted = true;

            await UniTask.NextFrame(
                PlayerLoopTiming.LastPostLateUpdate,
                cancellationToken);
            await Fade_async(0f, cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            return transitionActionCompleted;
        }
        finally
        {
            SCREEN_FADE_VIEW.SetAlpha(0f);
            SCREEN_FADE_VIEW.SetInputBlocking(false);
            SCREEN_FADE_MODEL.EndTransition();
        }
    }

    private async UniTask Fade_async(float targetAlpha, CancellationToken cancellationToken)
    {
        float startAlpha = SCREEN_FADE_VIEW.Alpha;
        float duration = SCREEN_FADE_MODEL.FadeDuration;

        if (duration <= 0f)
        {
            SCREEN_FADE_VIEW.SetAlpha(targetAlpha);
            return;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsedTime += Time.unscaledDeltaTime;
            SCREEN_FADE_VIEW.SetAlpha(Mathf.Lerp(
                startAlpha,
                targetAlpha,
                elapsedTime / duration));

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        SCREEN_FADE_VIEW.SetAlpha(targetAlpha);
    }
}
