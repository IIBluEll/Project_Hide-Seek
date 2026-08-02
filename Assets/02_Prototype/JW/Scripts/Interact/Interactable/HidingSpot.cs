using Cysharp.Threading.Tasks;
using System.Collections;
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
        return !_isTransitioning;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        _isTransitioning = true;
        ScreenFader.Instance.FadeOut(() => PlayerTeleport(playerInteractor));
    }

    private void PlayerTeleport(PlayerInteractionController playerInteractor)
    {
        Transform teleportPosition = _isInPlayer ? _exposePosition : _hidePosition;
        playerInteractor.OnTeleport(teleportPosition);

        _isInPlayer = !_isInPlayer;

        EPLAYER_STATE_TYPE type = _isInPlayer ? EPLAYER_STATE_TYPE.HIDING : EPLAYER_STATE_TYPE.NOMAL;
        playerInteractor.OnEndTransition(type);

        ScreenFader.Instance.FadeIn(()=> { _isTransitioning = false; });
    }
}
