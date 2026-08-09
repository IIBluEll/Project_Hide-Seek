using HideSeek.Gameplay;
using UnityEngine;

public class RepairGeneratorMission : MissionBase
{
    [SerializeField] private GameProgressProvider _provider;
    [SerializeField] private int _count;

    private void Awake()
    {
        _provider.CompletedGeneratorCountChanged += CompleteGeneratorRepair;
    }

    private void Start()
    {
        
    }

    private void CompleteGeneratorRepair(int count)
    {
        if (IsRunning && _count == count)
            CompleteMission();
    }
}
