using UnityEngine;

public class PlayerVisibilityState : MonoBehaviour, IPlayerVisibilityState
{
    public bool IsFullyHidden { get; private set; }

    public void EnterHiding()
    {
        IsFullyHidden = true;
    }

    public void ExitHiding()
    {
        IsFullyHidden = false;
    }
}
