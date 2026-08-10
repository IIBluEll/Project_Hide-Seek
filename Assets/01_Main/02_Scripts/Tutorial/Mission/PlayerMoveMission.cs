using UnityEngine;

public class PlayerMoveMission : MissionBase
{
    [SerializeField] private PlayerController _playerController;
    private IInputReader _inputReader;
    private IStateService _stateController;

    private void Awake()
    {
        _inputReader = _playerController.InputReader;
        _stateController = _playerController.State;

        _inputReader.OnMoveEvent += CheckComplete;
    }
    private void CheckComplete(Vector2 look)
    {
        if (!IsRunning)
            return;

        if (look.magnitude > 0.01f && _stateController.CanMove)
            CompleteMission();
    }
}
