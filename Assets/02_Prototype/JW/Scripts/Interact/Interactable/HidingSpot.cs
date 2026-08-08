using UnityEngine;


public class HidingSpot : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _hidePosition;
    [SerializeField] private Transform _exposePosition;
    [SerializeField] private POSTURE_STATE_ENUM _hidePosture;

    private bool _isInPlayer;
    private bool _isTransitioning;

    public string InteractionPrompt => _isInPlayer ? "나오기" : "숨기";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return !_isTransitioning;
    }

    public void InteractAct(PlayerInteractionController playerInteractor)
    {
        _isTransitioning = true;
        playerInteractor.BeginTransition();
        ScreenFader.Instance.FadeOut(() => PlayerTeleport(playerInteractor));
    }

    public void InteractRelease(PlayerInteractionController playerInteractionController) { }

    private void PlayerTeleport(PlayerInteractionController playerInteractor)
    {
        Transform teleportPosition = _isInPlayer ? _exposePosition : _hidePosition;

        if (_isInPlayer)
        {
            playerInteractor.ExitHide();
            playerInteractor.ClearCameraPositionOverride();
        }
        else
        {
            playerInteractor.EnterHide();
            playerInteractor.SetCameraPositionOverride(_hidePosition);
        }

        playerInteractor.OnTeleport(teleportPosition);

        _isInPlayer = !_isInPlayer;

        if (_isInPlayer)
            playerInteractor.SetContextInteractable(this);
        else
            playerInteractor.ClearContextInteractable(this);

        SetHidePosture(playerInteractor);

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

    private void SetHidePosture(PlayerInteractionController playerInteractor)
    {
        POSTURE_STATE_ENUM posture = _isInPlayer
            ? _hidePosture
            : POSTURE_STATE_ENUM.STANDING;

        playerInteractor.SetPosture(posture);
    }
}
