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
    void EnterHiding();
    void ExitHiding();
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
    AIMING,
    REPAIRING_GENERATOR,
    CATCHED
}

public class PlayerStateController : IStateService, IPlayerVisibilityState
{
    private PLAYER_POSITION_STATE _positionState = PLAYER_POSITION_STATE.NORMAL;
    private PLAYER_ACTION_STATE _actionState = PLAYER_ACTION_STATE.IDLE;

    public event Action<PLAYER_POSITION_STATE> OnChangedPositionStateEvent;
    public event Action<PLAYER_ACTION_STATE> OnChangedActionStateEvent;

    public bool CanMove =>
    _positionState == PLAYER_POSITION_STATE.NORMAL &&
    (_actionState == PLAYER_ACTION_STATE.IDLE || _actionState == PLAYER_ACTION_STATE.AIMING);
    public bool CanRotate =>
        _actionState != PLAYER_ACTION_STATE.REPAIRING_GENERATOR && _actionState != PLAYER_ACTION_STATE.TRANSITION;
    public bool CanCrouch =>
        _positionState == PLAYER_POSITION_STATE.NORMAL &&
        _actionState != PLAYER_ACTION_STATE.REPAIRING_GENERATOR;
    public bool CanInteraction => _actionState == PLAYER_ACTION_STATE.IDLE;
    public bool CanAction => _positionState == PLAYER_POSITION_STATE.NORMAL;
    public bool IsFullyHidden { get; private set; } = false;

    public PLAYER_POSITION_STATE PositionState => _positionState;
    public PLAYER_ACTION_STATE ActionState => _actionState;

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

    public void EnterHiding()
    {
        IsFullyHidden = true;
    }

    public void ExitHiding()
    {
        IsFullyHidden = false;
    }
}
