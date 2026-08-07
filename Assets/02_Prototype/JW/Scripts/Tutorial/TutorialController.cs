using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerAnimationController _animationController;

    public List<BlinkData> _blinkDatas;

    private void Awake()
    {
        BlockPlayerControl();

        StartTutorial();
    }

    public void StartTutorial()
    {
        _animationController.SetTrigger("Standing");

        _playerController.State.SetActionState(PLAYER_ACTION_STATE.TRANSITION);

        StartCoroutine(CoBlinkScreen(FinishStandAnimation));
    }
    public void BlockPlayerControl()
    {

    }
    public void FinishStandAnimation()
    {
        _playerController.State.SetActionState(PLAYER_ACTION_STATE.IDLE);
        FinishTutorial();
    }
    public void FinishTutorial()
    {

    }
    private IEnumerator CoBlinkScreen(Action endCall)
    {
        foreach (var blickData in _blinkDatas)
        {
            Debug.Log("Start");
            yield return ScreenFader.Instance.FadeCo(1, blickData.FadeOutTime);
            Debug.Log("Fade Out");

            yield return new WaitForSeconds(blickData.WaitTime);

            yield return ScreenFader.Instance.FadeCo(0, blickData.FadeInTime);
            Debug.Log("Fade In");
        }

        endCall?.Invoke();
    }
}

[System.Serializable]
public class BlinkData
{
    public float FadeOutTime;
    public float WaitTime;
    public float FadeInTime;
}
