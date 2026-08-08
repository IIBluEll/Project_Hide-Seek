using UnityEngine;

[CreateAssetMenu(fileName = "Description", menuName = "Tutorial/Description", order = 0)]
public class DescriptionData : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite Sprite;
}
