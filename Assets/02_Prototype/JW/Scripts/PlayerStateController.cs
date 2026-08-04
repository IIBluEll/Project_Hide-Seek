using System;

public interface IStateService
{
    bool CanInteraction { get; }
    bool CanMove { get; }
    bool CanRotate { get; }
    bool CanCrouch { get; }
    bool CanAction { get; }

    event Action<PLAYER_POSITION_STATE> OnChangedPositionStateEvent;
    event Action<PLAYER_ACTION_STATE> OnChangedActionStateEvent;

    void SetPositionState(PLAYER_POSITION_STATE state);
    void SetActionState(PLAYER_ACTION_STATE state);
}

public enum PLAYER_POSITION_STATE
{
    NORMAL,
    HIDING,
}

public enum PLAYER_ACTION_STATE
{
    IDLE,
    TRANSITION,
    ACTIONING,
}

public class PlayerStateController : IStateService
{
    private PLAYER_POSITION_STATE _positionState = PLAYER_POSITION_STATE.NORMAL;
    private PLAYER_ACTION_STATE _actionState = PLAYER_ACTION_STATE.IDLE;

    public event Action<PLAYER_POSITION_STATE> OnChangedPositionStateEvent;
    public event Action<PLAYER_ACTION_STATE> OnChangedActionStateEvent;

    public bool CanMove => _positionState == PLAYER_POSITION_STATE.NORMAL;
    public bool CanRotate => true;
    public bool CanCrouch => _positionState == PLAYER_POSITION_STATE.NORMAL;
    public bool CanInteraction => _actionState == PLAYER_ACTION_STATE.IDLE;
    public bool CanAction => _positionState == PLAYER_POSITION_STATE.NORMAL;

    public void SetPositionState(PLAYER_POSITION_STATE state)
    {
        _positionState = state;
        OnChangedPositionStateEvent?.Invoke(_positionState);

        System.Diagnostics.Debug.WriteLine(_positionState);
    }

    public void SetActionState(PLAYER_ACTION_STATE state)
    {
        _actionState = state;
        OnChangedActionStateEvent?.Invoke(_actionState);

        System.Diagnostics.Debug.WriteLine(_actionState);
    }
}
