public enum EPLAYER_STATE_TYPE
{
    NOMAL,
    TRANSITION,
    HIDING,
}

public class PlayerStateController
{
    private EPLAYER_STATE_TYPE _state = EPLAYER_STATE_TYPE.NOMAL;
    public bool CanMove => _state == EPLAYER_STATE_TYPE.NOMAL;
    public bool CanRotate => _state != EPLAYER_STATE_TYPE.TRANSITION;
    public bool CanCrouch => _state == EPLAYER_STATE_TYPE.NOMAL;
    public void SetState(EPLAYER_STATE_TYPE state)
    {
        _state = state;
    }
}
