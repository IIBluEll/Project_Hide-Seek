using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TUTORIAL_STEP_TYPE
{
    DESCRIPTION,
    MISSION
}

[System.Serializable]
public class TutorialStepData
{
    public TUTORIAL_STEP_TYPE StepType;
    public DescriptionData Description;
    public MissionBase Mission;
}

public class MissionController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private List<TutorialStepData> _tutorialSteps;
    [SerializeField] private MissionIndicator_View _missionIndicator;
    [SerializeField] private TutorialDescription_View _tutorialDescription;
    [SerializeField, Min(0f)] private float _completedIndicatorDelay = 0.5f;

    private int _stepIndex;
    private MissionBase _currentMission;
    private Coroutine _nextStepCor;

    public event Action AllMissionsCompleted;

    public void StartMissionFlow()
    {
        _stepIndex = 0;
        BeginCurrentStep();
    }
    public void CompleteCurrentMission()
    {
        OnMissionClearActioned();
    }
    private void BeginCurrentStep()
    {
        if (_tutorialSteps == null || _stepIndex >= _tutorialSteps.Count)
        {
            FinishMissionFlow();
            return;
        }

        TutorialStepData stepData = _tutorialSteps[_stepIndex];
        if (stepData == null)
        {
            BeginNextStep();
            return;
        }

        switch (stepData.StepType)
        {
            case TUTORIAL_STEP_TYPE.DESCRIPTION:
                BeginDescriptionStep(stepData.Description);
                break;

            case TUTORIAL_STEP_TYPE.MISSION:
                BeginMissionStep(stepData.Mission);
                break;
        }
    }
    private void BeginDescriptionStep(DescriptionData descriptionData)
    {
        UnbindCurrentMission();

        if (_missionIndicator != null)
            _missionIndicator.HideIndicator();

        if (_tutorialDescription == null || descriptionData == null)
        {
            BeginNextStep();
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        _playerController.State.SetActionState(PLAYER_ACTION_STATE.TRANSITION);

        _tutorialDescription.OnClickConfirm -= OnDescriptionConfirmActioned;
        _tutorialDescription.OnClickConfirm += OnDescriptionConfirmActioned;
        _tutorialDescription.ShowDescription(descriptionData);
    }
    private void BeginMissionStep(MissionBase mission)
    {
        UnbindCurrentDescription();
        UnbindCurrentMission();

        if (mission == null)
        {
            BeginNextStep();
            return;
        }

        _currentMission = mission;

        if (_missionIndicator != null)
            _missionIndicator.ShowIndicator(_currentMission.IndicatorData);

        _currentMission.OnMissionClear -= OnMissionClearActioned;
        _currentMission.OnMissionClear += OnMissionClearActioned;

        if (_currentMission is ICountMission countMission)
        {
            countMission.OnCountChanged -= OnCountChangedActioned;
            countMission.OnCountChanged += OnCountChangedActioned;
        }

        _currentMission.BeginMission();
    }
    private void OnDescriptionConfirmActioned()
    {
        if (_tutorialDescription != null)
            _tutorialDescription.HideDescription();

        Cursor.lockState = CursorLockMode.Locked;
        _playerController.State.SetActionState(PLAYER_ACTION_STATE.IDLE);

        UnbindCurrentDescription();
        BeginNextStep();
    }
    private void OnMissionClearActioned()
    {
        if (_nextStepCor != null)
            return;

        if (_missionIndicator != null)
            _missionIndicator.SetCompleted(true);

        UnbindCurrentMission();

        _nextStepCor = StartCoroutine(CoBeginNextStep_cor());
    }
    private IEnumerator CoBeginNextStep_cor()
    {
        yield return new WaitForSeconds(_completedIndicatorDelay);

        if (_missionIndicator != null)
            _missionIndicator.HideIndicator();

        _nextStepCor = null;
        BeginNextStep();
    }
    private void BeginNextStep()
    {
        _stepIndex++;
        BeginCurrentStep();
    }
    private void OnCountChangedActioned(int currentCount, int targetCount)
    {
        if (_missionIndicator != null)
            _missionIndicator.UpdateCount(currentCount, targetCount);
    }
    private void FinishMissionFlow()
    {
        UnbindCurrentDescription();
        UnbindCurrentMission();

        if (_missionIndicator != null)
            _missionIndicator.HideIndicator();

        AllMissionsCompleted?.Invoke();
    }
    private void UnbindCurrentDescription()
    {
        if (_tutorialDescription != null)
            _tutorialDescription.OnClickConfirm -= OnDescriptionConfirmActioned;
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
        if (_nextStepCor != null)
        {
            StopCoroutine(_nextStepCor);
            _nextStepCor = null;
        }

        UnbindCurrentDescription();
        UnbindCurrentMission();
    }
}
