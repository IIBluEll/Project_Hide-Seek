using System;
using UnityEngine;

public class PlayerRotateMission : ATutorialMission, IMission
{
    [SerializeField] private PlayerController _playerController;
    private IInputReader _inputReader;
    private IStateService _stateController;

    private void Awake()
    {
        _inputReader = _playerController.InputReader;
        _stateController = _playerController.State;
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        _inputReader.OnLookEvent += CheckComplete;
    }

    private void CheckComplete(Vector2 look)
    {
        if (look.magnitude > 0.01f && _stateController.CanRotate)
            CompleteMission();
    }
}
