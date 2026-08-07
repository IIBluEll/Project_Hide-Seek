using System.Collections;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;

    public void StartTutorial()
    {
        
    }

    private IEnumerator CoBlinkScreen()
    {
        yield return null;
    }
}

public class BlinkData
{
    public float FadeOutTime;
    public float FadeInTime;
}
