using UnityEngine;

public class DestinationMission : MissionBase
{
    private void OnTriggerStay(Collider other)
    {
        if (IsRunning && other.CompareTag("Player"))
        {
            CompleteMission();
        }
    }
}
