using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private TutorialMissionController _missionController;

    private void Awake()
    {
        if (_missionController != null)
        {
            _missionController.AllMissionsCompleted -= OnAllMissionsCompletedActioned;
            _missionController.AllMissionsCompleted += OnAllMissionsCompletedActioned;
        }

        _playerController.WakeUpDirect();

        _missionController.StartMissionFlow();
    }

    private void OnDestroy()
    {
        if (_missionController != null)
            _missionController.AllMissionsCompleted -= OnAllMissionsCompletedActioned;
    }

    private void OnAllMissionsCompletedActioned()
    {
        Debug.Log("Tutorial End");
    }
}


