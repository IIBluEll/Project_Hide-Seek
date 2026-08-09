using UnityEngine;

public sealed class TutorialPlayerActionMission : MissionBase
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PLAYER_ACTION_STATE _targetActionState;

    protected override void OnBeginMission()
    {
        if (_playerController == null)
        {
            Debug.LogError($"[{nameof(TutorialPlayerActionMission)}] PlayerController 참조가 비어 있습니다.", this);
            return;
        }

        _playerController.State.OnChangedActionStateEvent -= OnChangedActionStateActioned;
        _playerController.State.OnChangedActionStateEvent += OnChangedActionStateActioned;
    }

    protected override void OnEndMission()
    {
        if (_playerController != null)
            _playerController.State.OnChangedActionStateEvent -= OnChangedActionStateActioned;
    }

    private void OnChangedActionStateActioned(PLAYER_ACTION_STATE actionState)
    {
        if (actionState == _targetActionState)
            CompleteMission();
    }
}
