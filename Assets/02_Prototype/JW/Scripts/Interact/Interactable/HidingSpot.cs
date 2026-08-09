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
        bool isExiting = _isInPlayer;
        Transform teleportPosition = isExiting ? _exposePosition : _hidePosition;
        Vector3 targetPosition = teleportPosition.position;
        string targetName = teleportPosition.name;

        if (isExiting)
        {
            playerInteractor.OnTeleport(teleportPosition);
            playerInteractor.ExitHide();
            playerInteractor.ClearCameraPositionOverride();
        }
        else
        {
            playerInteractor.OnTeleport(teleportPosition);
            playerInteractor.EnterHide();
            playerInteractor.SetCameraPositionOverride(_hidePosition);
        }

        if (isExiting)
            LogExitTeleportPosition("BeforeTeleport", playerInteractor, targetName, targetPosition);

        if (isExiting)
        {
            LogExitTeleportPosition("AfterTeleport", playerInteractor, targetName, targetPosition);
            StartCoroutine(LogExitTeleportPositionNextFrame(playerInteractor, targetName, targetPosition));
        }

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

    private System.Collections.IEnumerator LogExitTeleportPositionNextFrame(
        PlayerInteractionController playerInteractor,
        string targetName,
        Vector3 targetPosition)
    {
        yield return null;

        if (playerInteractor == null)
            yield break;

        LogExitTeleportPosition("NextFrame", playerInteractor, targetName, targetPosition);
    }

    private void LogExitTeleportPosition(
        string phase,
        PlayerInteractionController playerInteractor,
        string targetName,
        Vector3 targetPosition)
    {
        Debug.Log(
            $"[HidingSpot ExitTeleport] {phase} | Spot={name} | Target={targetName} {FormatVector(targetPosition)} | Player={FormatVector(playerInteractor.transform.position)}",
            this);
    }

    private static string FormatVector(Vector3 position)
    {
        return $"({position.x:F4}, {position.y:F4}, {position.z:F4})";
    }
}
