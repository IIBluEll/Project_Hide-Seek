using UnityEngine;

public sealed class TutorialPlayerPostureMission : MissionBase
{
    [SerializeField] private MoveController _moveController;
    [SerializeField] private POSTURE_STATE_ENUM _targetPosture;

    protected override void OnBeginMission()
    {
        if (_moveController == null)
        {
            Debug.LogError($"[{nameof(TutorialPlayerPostureMission)}] MoveController 참조가 비어 있습니다.", this);
            return;
        }

        _moveController.OnPostureChanged -= OnPostureChangedActioned;
        _moveController.OnPostureChanged += OnPostureChangedActioned;
    }

    protected override void OnEndMission()
    {
        if (_moveController != null)
            _moveController.OnPostureChanged -= OnPostureChangedActioned;
    }

    private void OnPostureChangedActioned(POSTURE_STATE_ENUM posture, bool isEnabled)
    {
        if (isEnabled && posture == _targetPosture)
            CompleteMission();
    }
}
