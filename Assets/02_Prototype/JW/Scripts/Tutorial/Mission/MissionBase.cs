using System;
using UnityEngine;

public abstract class MissionBase : MonoBehaviour, IMission
{
    private bool _isRunning;
    private bool _isCleared;

    public event Action OnMissionClear;

    public bool IsRunning => _isRunning;

    public void BeginMission()
    {
        Debug.Log("Mission");
        _isRunning = true;
        _isCleared = false;
        OnBeginMission();
    }

    public void EndMission()
    {
        OnEndMission();
        _isRunning = false;
    }

    protected void CompleteMission()
    {
        if (!_isRunning || _isCleared)
            return;

        _isCleared = true;
        OnMissionClear?.Invoke();
    }

    protected virtual void OnBeginMission() { }
    protected virtual void OnEndMission() { }
}
