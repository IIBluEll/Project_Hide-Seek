using UnityEngine;

public class PlayerRunMission : TutorialMission
{
    [SerializeField] private PlayerController _playerController;
    private IInputReader _inputReader;
    private IStateService _stateController;

    private void Awake()
    {
        _inputReader = _playerController.InputReader;
        _stateController = _playerController.State;

        _inputReader.OnSprintEvent += CheckComplete;
    }

    private void CheckComplete(bool isValue)
    {
        if (!IsRunning)
            return;

        if (isValue && _stateController.CanMove)
            CompleteMission();
    }
}
