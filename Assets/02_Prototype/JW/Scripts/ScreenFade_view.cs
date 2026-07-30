using HM.CodeBase;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class ScreenFade_view : AView
{
    [SerializeField] private CanvasGroup _canvasGroup;

    public float Alpha => _canvasGroup.alpha;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetAlpha(float alpha)
    {
        _canvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    public void SetInputBlocking(bool inputBlocking)
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = inputBlocking;
    }

    public override void Clear()
    {
        SetAlpha(0f);
        SetInputBlocking(false);
    }
}
