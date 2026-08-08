using HideSeek.Generators;
using UnityEngine;

public sealed class TutorialGeneratorCompleteMission : ATutorialMission
{
    [SerializeField] private Generator _generator;

    protected override void OnBeginMission()
    {
        if (_generator == null)
        {
            Debug.LogError($"[{nameof(TutorialGeneratorCompleteMission)}] Generator 참조가 비어 있습니다.", this);
            return;
        }

        _generator.Completed -= OnGeneratorCompletedActioned;
        _generator.Completed += OnGeneratorCompletedActioned;
    }

    protected override void OnEndMission()
    {
        if (_generator != null)
            _generator.Completed -= OnGeneratorCompletedActioned;
    }

    private void OnGeneratorCompletedActioned()
    {
        CompleteMission();
    }
}
