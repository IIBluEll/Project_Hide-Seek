using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        ScreenFader.Instance.FadeOut(MoveToGameScene, 0.5f);
    }

    private void MoveToGameScene()
    {
        SceneManager.LoadScene("InGame_Map", LoadSceneMode.Single);
    }

    // 1. 
    // 2. 
    //
    //
    //
    //
    //
    //


}


