using System;
using UnityEngine;

public interface IMissionIndicator
{
    void ShowIndicator(MissionIndicatorData missionIndicatorData);
    void UpdateCount(int currentCount, int targetCount);
    void SetCompleted(bool isCompleted);
    void HideIndicator();
}

public interface IMission
{
    event Action OnMissionClear;

    void BeginMission();
    void EndMission();
}

public interface ICountMission : IMission
{
    event Action<int, int> OnCountChanged;
}

public interface IDescription
{
    event Action OnClickConfirm;
    void ShowDescription(string description, Sprite descriptionSprite);

}
