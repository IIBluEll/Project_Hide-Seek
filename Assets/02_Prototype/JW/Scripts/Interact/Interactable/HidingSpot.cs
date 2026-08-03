using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _hidePosition;
    [SerializeField] private Transform _exposePosition;
    private bool _isInPlayer;
    private bool _isTransitioning;

    public string InteractionPrompt => _isInPlayer ? "나오기" : "숨기";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return !_isTransitioning;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        _isTransitioning = true;
        playerInteractor.BeginTransition();
        ScreenFader.Instance.FadeOut(() => PlayerTeleport(playerInteractor));
    }

    private void PlayerTeleport(PlayerInteractionController playerInteractor)
    {
        Transform teleportPosition = _isInPlayer ? _exposePosition : _hidePosition;
        playerInteractor.OnTeleport(teleportPosition);

        _isInPlayer = !_isInPlayer;

        PLAYER_POSITION_STATE state = _isInPlayer
            ? PLAYER_POSITION_STATE.HIDING
            : PLAYER_POSITION_STATE.NORMAL;

        playerInteractor.SetPositionState(state);

        ScreenFader.Instance.FadeIn(() =>
        {
            _isTransitioning = false;
            playerInteractor.EndTransition();
        });
    }
}
