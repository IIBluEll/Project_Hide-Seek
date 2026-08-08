using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Tutorial/Mission", order = 0)]
public class MissionIndicatorData : ScriptableObject
{
    [SerializeField] private string _missionText;
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private bool _hasCount;
    [SerializeField, Min(1)] private int _targetCount = 1;

    public string MissionText => _missionText;
    public Sprite IconSprite => _iconSprite;
    public bool HasCount => _hasCount;
    public int TargetCount => _targetCount;
}
