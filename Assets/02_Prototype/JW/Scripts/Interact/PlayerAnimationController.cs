using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int X_MOVE_HASH = Animator.StringToHash("XMove");
    private static readonly int Z_MOVE_HASH = Animator.StringToHash("ZMove");
    private static readonly int IS_RUN_HASH = Animator.StringToHash("IsRun");
    private static readonly int Is_STANDING_HASH = Animator.StringToHash("IsStanding");
    private static readonly int IS_CROUCH_HASH = Animator.StringToHash("IsCrouch");
    private static readonly int IS_PRONE_HASH = Animator.StringToHash("IsProne");

    [SerializeField] private Animator _animator;

    #region Float
    public void SetMoveAnima(Vector2 move)
    {
        SetFloat(X_MOVE_HASH, move.x);
        SetFloat(Z_MOVE_HASH, move.y);
    }
    public void SetFloat(string name, float value)
    {
        _animator.SetFloat(name, value);
    }
    public void SetFloat(int hash, float value)
    {
        _animator.SetFloat(hash, value);
    }
    #endregion

    #region Bool

    public void SetLocomotionAnima(LOCOMOTION_STATE_ENUM locomotion, bool value)
    {
        if(locomotion == LOCOMOTION_STATE_ENUM.RUN)
        {
            SetBool(IS_RUN_HASH, value);
        }
    }
    public void SetPostureParam(POSTURE_STATE_ENUM posture, bool value)
    {
        switch (posture)
        {
            case POSTURE_STATE_ENUM.STANDING:
                SetBool(Is_STANDING_HASH, value);
                break;
            case POSTURE_STATE_ENUM.CROUCH:
                SetBool(IS_CROUCH_HASH, value);
                break;
            case POSTURE_STATE_ENUM.PRONE:
                SetBool(IS_PRONE_HASH, value);
                break;
        }
    }

    public void SetBool(string name, bool value)
    {
        _animator.SetBool(name, value);
    }
    public void SetBool(int hash, bool value)
    {
        _animator.SetBool(hash, value);
    }
    #endregion

    #region Trigger
    public void SetTrigger(string name)
    {
        _animator.SetTrigger(name);
    }
    public void SetTrigger(int hash)
    {
        _animator.SetTrigger(hash);
    }
    #endregion
}
