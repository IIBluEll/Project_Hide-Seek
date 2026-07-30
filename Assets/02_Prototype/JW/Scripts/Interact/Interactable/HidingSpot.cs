using Cysharp.Threading.Tasks;
using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _hidePosition;
    [SerializeField] private Transform _exposePosition;
    private bool _isInPlayer;
    private bool _isTransitioning;

    public string InteractionPrompt => _isInPlayer ? "³ª¿À±â" : "¼û±â";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        Transform targetPosition = _isInPlayer ? _exposePosition : _hidePosition;
        return !_isTransitioning && targetPosition != null;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        if (!CanInteract(playerInteractor))
            return;

        ChangeHidingState_async(playerInteractor).Forget();
    }

    private async UniTask ChangeHidingState_async(PlayerInteractionController playerInteractor)
    {
        _isTransitioning = true;

        bool isEntering = !_isInPlayer;
        Transform targetPosition = isEntering ? _hidePosition : _exposePosition;

        try
        {
            bool completed = await playerInteractor.TransitionPlayer_async(
                targetPosition,
                !isEntering,
                this.GetCancellationTokenOnDestroy());

            if (completed)
                _isInPlayer = isEntering;
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}
