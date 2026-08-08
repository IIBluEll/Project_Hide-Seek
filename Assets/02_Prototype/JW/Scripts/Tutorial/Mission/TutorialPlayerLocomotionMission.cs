using UnityEngine;

public sealed class TutorialPlayerLocomotionMission : ATutorialMission
{
    [SerializeField] private MoveController _moveController;
    [SerializeField] private LOCOMOTION_STATE_ENUM _targetLocomotion;

    protected override void OnBeginMission()
    {
        if (_moveController == null)
        {
            Debug.LogError($"[{nameof(TutorialPlayerLocomotionMission)}] MoveController 참조가 비어 있습니다.", this);
            return;
        }

        _moveController.OnLocomotionChanged -= OnLocomotionChangedActioned;
        _moveController.OnLocomotionChanged += OnLocomotionChangedActioned;
    }

    protected override void OnEndMission()
    {
        if (_moveController != null)
            _moveController.OnLocomotionChanged -= OnLocomotionChangedActioned;
    }

    private void OnLocomotionChangedActioned(LOCOMOTION_STATE_ENUM locomotion, bool isEnabled)
    {
        if (isEnabled && locomotion == _targetLocomotion)
            CompleteMission();
    }
}
