using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum HAND_STATE_ENUM
{
    EMPTY,
    GRAPPING,
    HOLDING,
    AIMING,
    THROW,
    RECOVERY,
}

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private Transform _handItemPivot;
    [SerializeField] private Transform _throwDirectionTrans;
    [SerializeField] private float _throwMaxPower;
    [SerializeField] private float _chargeSpeed;

    [SerializeField] private Animator _animator;

    private GrapItem _grapItem;
    private float _currentPower;
    public HAND_STATE_ENUM _state = HAND_STATE_ENUM.EMPTY;

    public bool GrappedItem => _grapItem != null;
    public bool IsAiming => _state == HAND_STATE_ENUM.AIMING;
    public float NormalizedPower => _throwMaxPower > 0f
        ? _currentPower / _throwMaxPower
        : 0f;

    public event Action<bool> OnAimStateChanged;
    public event Action<float> OnThrowPowerChanged;

    private void Awake()
    {
        if (_throwDirectionTrans == null && Camera.main != null)
            _throwDirectionTrans = Camera.main.transform;
    }

    private void Update()
    {
        float previousPower = _currentPower;
        _currentPower = Mathf.MoveTowards(_currentPower, _throwMaxPower, _chargeSpeed * Time.deltaTime);

        if (!Mathf.Approximately(previousPower, _currentPower))
            OnThrowPowerChanged?.Invoke(NormalizedPower);

        if(_state == HAND_STATE_ENUM.GRAPPING)
        {
            if (_animator.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.7f)
            {
                Grap();
                _recoveryState = HAND_STATE_ENUM.HOLDING;
                _state = HAND_STATE_ENUM.RECOVERY;
            }
        }


        if (_state == HAND_STATE_ENUM.RECOVERY)
        {
            _recoveryTime -= Time.deltaTime * 2;
            _animator.SetLayerWeight(1, _recoveryTime);

            if (_recoveryTime <= 0)
            {
                _state = _recoveryState;
                _recoveryTime = 1;
            }
        }

        if (_state == HAND_STATE_ENUM.THROW)
        {
            if (_animator.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.6f)
            {
                ThrowItem();

                _recoveryState = HAND_STATE_ENUM.EMPTY;
                _state = HAND_STATE_ENUM.RECOVERY;
            }
        }
    }

    private HAND_STATE_ENUM _recoveryState;

    private void OnDisable()
    {
        CancelAim();
    }

    float _recoveryTime;

    private void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Aim();
            return;
        }

        if (_state == HAND_STATE_ENUM.AIMING)
        {
            _state = HAND_STATE_ENUM.THROW;
            _animator.SetTrigger("Throw");
        }
    }

    

    private void OnCancelAim(InputValue value)
    {
        Debug.Log($"우클릭 : {value.isPressed}");

        if (value.isPressed)
            CancelAim();
    }

    public void GrapItem(GrapItem grapItem)
    {
        if (grapItem == null)
            return;

        _grapItem = grapItem;

        _animator.SetLayerWeight(1, 1);
        _animator.SetTrigger("Grap");

        _state = HAND_STATE_ENUM.GRAPPING;
    }

    public void Grap()
    {
        CancelAim();

        if (_grapItem != null)
        {
            _grapItem.transform.parent = null;
            _grapItem.Release();
        }

        _grapItem.transform.parent = _handItemPivot;
        _grapItem.transform.localPosition = Vector3.zero;
        _grapItem.Grapped();

        _state = HAND_STATE_ENUM.HOLDING;
    }

    public void Aim()
    {
        if (_state != HAND_STATE_ENUM.HOLDING || _grapItem == null)
            return;

        _animator.SetLayerWeight(1, 1);

        _animator.SetTrigger("Aim");
        _recoveryTime = 1;

        _state = HAND_STATE_ENUM.AIMING;
        _currentPower = 0f;

        OnThrowPowerChanged?.Invoke(0f);
        OnAimStateChanged?.Invoke(true);
    }

    public void CancelAim()
    {
        if (_state != HAND_STATE_ENUM.AIMING)
            return;

        _state = HAND_STATE_ENUM.HOLDING;
        _currentPower = 0f;

        _animator.SetTrigger("AimCancel");

        _animator.SetLayerWeight(1, 0);

        OnThrowPowerChanged?.Invoke(0f);
        OnAimStateChanged?.Invoke(false);
    }

    public void ThrowItem()
    {
        if (_grapItem == null)
            return;

        Debug.Log("던져랏");

        GrapItem grapItem = _grapItem;
        float throwPower = _currentPower;
        Vector3 throwDirection = _throwDirectionTrans != null ? _throwDirectionTrans.forward : transform.forward;

        _grapItem = null;
        _state = HAND_STATE_ENUM.EMPTY;
        _currentPower = 0f;

        grapItem.transform.SetParent(null, true);
        grapItem.Throw(_throwDirectionTrans.forward, throwPower);


        OnThrowPowerChanged?.Invoke(0f);
        OnAimStateChanged?.Invoke(false);
    }
}
