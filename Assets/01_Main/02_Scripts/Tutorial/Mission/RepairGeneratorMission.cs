using System;
using HideSeek.Gameplay;
using UnityEngine;

public class RepairGeneratorMission : MissionBase, ICountMission
{
    [SerializeField] private GameProgressProvider _provider;

    public event Action<int, int> OnCountChanged;

    protected override void OnBeginMission()
    {
        if (_provider == null)
        {
            Debug.LogError($"[{nameof(RepairGeneratorMission)}] GameProgressProvider 참조가 비어 있습니다.", this);
            return;
        }

        _provider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
        _provider.CompletedGeneratorCountChanged += OnCompletedGeneratorCountChangedActioned;

        NotifyCountChanged();

        if (_provider.AreAllGeneratorsCompleted)
            CompleteMission();
    }

    protected override void OnEndMission()
    {
        if (_provider != null)
            _provider.CompletedGeneratorCountChanged -= OnCompletedGeneratorCountChangedActioned;
    }

    private void OnCompletedGeneratorCountChangedActioned(int completedGeneratorCount)
    {
        if (_provider == null)
            return;

        NotifyCountChanged();

        if (completedGeneratorCount >= _provider.RequiredGeneratorCount)
            CompleteMission();
    }

    private void NotifyCountChanged()
    {
        if (_provider == null)
            return;

        OnCountChanged?.Invoke(_provider.CompletedGeneratorCount, _provider.RequiredGeneratorCount);
    }
}
