using UnityEngine;

public class DestinationMission : MissionBase
{
    [SerializeField] private GameObject _barrior;

    private void Awake()
    {
        if(_barrior != null)
            _barrior.SetActive(true);
    }

    protected override void OnBeginMission()
    {
        if (_barrior != null)
            _barrior.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsRunning && other.CompareTag("Player"))
        {
            CompleteMission();
        }
    }
}
