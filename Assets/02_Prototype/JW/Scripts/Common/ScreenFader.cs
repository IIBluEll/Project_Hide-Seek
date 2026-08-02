using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{

    private const int CANVAS_SORTING_LAYER_ORDER = 100;
    private const float FADE_OUT_TIME = 0.5f;
    private const float FADE_IN_TIME = 0.5f;
    private static bool _isTransition = false;


    private static ScreenFader _instance;
    public static ScreenFader Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ScreenFader>();

            if (_instance == null)
            {
                GameObject screenFaderObj = new GameObject("ScreenFader");
                screenFaderObj.AddComponent<Canvas>();
                screenFaderObj.AddComponent<CanvasGroup>().alpha = 0;
                screenFaderObj.AddComponent<Image>().color = Color.black;
                _instance = screenFaderObj.AddComponent<ScreenFader>();
                _instance.name = "ScreenFader";
            }
                

            return _instance;
        }
    }

    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_instance)
            Destroy(this.gameObject);

        _instance = this;

        DontDestroyOnLoad(this);

        if (_canvasGroup == null)
            _canvasGroup = this.GetComponent<CanvasGroup>();

        if (_canvas == null)
            _canvas = this.GetComponent<Canvas>();

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CANVAS_SORTING_LAYER_ORDER;
    }

    public void FadeOut(Action endCall = null, float fadeOutTime = 0)
    {
        if (_isTransition)
            return;

        _isTransition = true;

        float fadeTime = fadeOutTime <= 0 ? FADE_OUT_TIME : fadeOutTime;
        StartCoroutine(FadeCo(1f, fadeTime, endCall));
    }


    public void FadeIn(Action endCall = null, float fadeInTime = 0)
    {
        if (_isTransition)
            return;

        _isTransition = true;
        float fadeTime = fadeInTime <= 0 ? FADE_IN_TIME : fadeInTime;
        StartCoroutine(FadeCo(0f, fadeTime, endCall));
    }

    private IEnumerator FadeCo(float targetAlpha, float time, Action endCall)
    {
        float current = 0;
        float startAlpha = _canvasGroup.alpha;

        if (time <= 0)
        {
            _canvasGroup.alpha = targetAlpha;
            _isTransition = false;
            yield break;
        }

        while (current <= 1)
        {
            current += Time.deltaTime / time;

            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, current);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _isTransition = false;

        endCall?.Invoke();
    }

}
