using UnityEngine;

public sealed class TutorialPlayerPositionMission : MissionBase
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PLAYER_POSITION_STATE _targetPositionState;

    protected override void OnBeginMission()
    {
        if (_playerController == null)
        {
            Debug.LogError($"[{nameof(TutorialPlayerPositionMission)}] PlayerController 참조가 비어 있습니다.", this);
            return;
        }

        _playerController.State.OnChangedPositionStateEvent -= OnChangedPositionStateActioned;
        _playerController.State.OnChangedPositionStateEvent += OnChangedPositionStateActioned;
    }

    protected override void OnEndMission()
    {
        if (_playerController != null)
            _playerController.State.OnChangedPositionStateEvent -= OnChangedPositionStateActioned;
    }

    private void OnChangedPositionStateActioned(PLAYER_POSITION_STATE positionState)
    {
        if (positionState == _targetPositionState)
            CompleteMission();
    }
}
