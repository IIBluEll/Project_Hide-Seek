using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWakeUpBlink : MonoBehaviour
{
    [SerializeField] private List<BlinkData> _blinkDatas;

    public event Action OnFinishedBlinkEvent;

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(Play_cor());
    }

    public IEnumerator Play_cor()
    {
        foreach (BlinkData blinkData in _blinkDatas)
        {
            yield return ScreenFader.Instance.FadeCo(1, blinkData.FadeOutTime);
            yield return new WaitForSeconds(blinkData.WaitTime);
            yield return ScreenFader.Instance.FadeCo(0, blinkData.FadeInTime);
        }

        OnFinishedBlinkEvent?.Invoke();
    }
}