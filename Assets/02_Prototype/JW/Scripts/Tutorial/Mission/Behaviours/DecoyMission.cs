using UnityEngine;

public class DecoyMission : MissionBase
{
    [SerializeField] private PlayerHandController _handController;
    [SerializeField] private HAND_STATE_ENUM _completeState;

    protected override void OnBeginMission()
    {
        Debug.Log("!!");
        _handController.OnStateChangeEvent -= CheckMissionState;
        _handController.OnStateChangeEvent += CheckMissionState;
    }
    private void CheckMissionState(HAND_STATE_ENUM state)
    {
        Debug.Log($"{IsRunning} / {state}");

        if(IsRunning && state == _completeState)
        {
            CompleteMission();
        }
    }
    protected override void OnEndMission()
    {
        if(_handController != null)
            _handController.OnStateChangeEvent -= CheckMissionState;
    }
}

