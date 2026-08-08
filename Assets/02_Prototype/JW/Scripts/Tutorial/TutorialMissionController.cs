using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMissionController : MonoBehaviour
{
    [SerializeField] private List<MissionIndicatorData> _missionLists;
    [SerializeField] private MissionIndicator_View _missionIndicator;
    [SerializeField] private List<MonoBehaviour> _missionBehaviours;
    [SerializeField, Min(0f)] private float _completedIndicatorDelay = 0.5f;

    private int _indicatorIndex;
    private IMission _currentMission;
    private Coroutine _nextMissionCor;

    public event Action AllMissionsCompleted;

    public void StartMissionFlow()
    {
        _indicatorIndex = 0;
        BeginCurrentMission();
    }

    public void ShowIndicator()
    {
        ShowCurrentIndicator();
    }

    public void CompleteCurrentMission()
    {
        OnMissionClearActioned();
    }

    private void BeginCurrentMission()
    {
        if (_missionLists == null || _indicatorIndex >= _missionLists.Count)
        {
            FinishMissionFlow();
            return;
        }

        Debug.Log("SS");
        ShowCurrentIndicator();
        BindCurrentMission();
    }

    private void ShowCurrentIndicator()
    {
        if (_missionIndicator == null || _missionLists == null || _indicatorIndex >= _missionLists.Count)
            return;

        _missionIndicator.ShowIndicator(_missionLists[_indicatorIndex]);
    }

    private void BindCurrentMission()
    {
        UnbindCurrentMission();

        Debug.Log("11");

        MonoBehaviour missionBehaviour = GetMissionBehaviour(_indicatorIndex);
        if (missionBehaviour == null)
            return;

        Debug.Log("22");

        _currentMission = missionBehaviour as IMission;
        if (_currentMission == null)
        {
            Debug.LogError($"[{nameof(TutorialMissionController)}] {missionBehaviour.name}은 {nameof(IMission)}을 구현해야 합니다.", missionBehaviour);
            return;
        }

        Debug.Log("33");

        _currentMission.OnMissionClear -= OnMissionClearActioned;
        _currentMission.OnMissionClear += OnMissionClearActioned;

        if (_currentMission is ICountMission countMission)
        {
            countMission.OnCountChanged -= OnCountChangedActioned;
            countMission.OnCountChanged += OnCountChangedActioned;
        }

        Debug.Log(_currentMission);
        _currentMission.BeginMission();
    }

    private MonoBehaviour GetMissionBehaviour(int missionIndex)
    {
        Debug.Log(_missionBehaviours == null);
        Debug.Log(missionIndex < 0);
        Debug.Log(missionIndex >= _missionBehaviours.Count);

        if (_missionBehaviours == null || missionIndex < 0 || missionIndex >= _missionBehaviours.Count)
            return null;

        return _missionBehaviours[missionIndex];
    }

    private void OnMissionClearActioned()
    {
        if (_nextMissionCor != null)
            return;

        if (_missionIndicator != null)
            _missionIndicator.SetCompleted(true);

        _nextMissionCor = StartCoroutine(CoBeginNextMission_cor());
    }

    private IEnumerator CoBeginNextMission_cor()
    {
        yield return new WaitForSeconds(_completedIndicatorDelay);

        UnbindCurrentMission();

        _indicatorIndex++;
        _nextMissionCor = null;

        BeginCurrentMission();
    }

    private void OnCountChangedActioned(int currentCount, int targetCount)
    {
        if (_missionIndicator != null)
            _missionIndicator.UpdateCount(currentCount, targetCount);
    }

    private void FinishMissionFlow()
    {
        UnbindCurrentMission();

        if (_missionIndicator != null)
            _missionIndicator.HideIndicator();

        AllMissionsCompleted?.Invoke();
    }

    private void UnbindCurrentMission()
    {
        if (_currentMission == null)
            return;

        _currentMission.OnMissionClear -= OnMissionClearActioned;

        if (_currentMission is ICountMission countMission)
            countMission.OnCountChanged -= OnCountChangedActioned;

        _currentMission.EndMission();
        _currentMission = null;
    }

    private void OnDisable()
    {
        if (_nextMissionCor != null)
        {
            StopCoroutine(_nextMissionCor);
            _nextMissionCor = null;
        }

        UnbindCurrentMission();
    }
}
