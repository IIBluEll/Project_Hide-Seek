using UnityEngine;

public class InGameMissionController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private MissionController _missionController;

    private void Start()
    {
        _missionController.StartMissionFlow();

        _missionController.AllMissionsCompleted += OnClear;
    }

    private void OnClear()
    {
        Debug.Log($"JW GameClear");
        _playerController.gameObject.SetActive(false);
    }
}
