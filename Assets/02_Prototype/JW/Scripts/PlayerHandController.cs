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
    [SerializeField] private Animator _animator;

    [SerializeField] private Transform _itemGrapPivot;
    [SerializeField] private Transform _throwDirectionTrans;

    [SerializeField] private float _throwMaxPower;
    [SerializeField] private float _chargeSpeed;

    private GrapItem _grapItem;
    private float _currentPower;
    private float _recoveryTime;
    private HAND_STATE_ENUM _state = HAND_STATE_ENUM.EMPTY;
    private HAND_STATE_ENUM _recoveryState;

    public float NormalizedPower => _throwMaxPower > 0f ? _currentPower / _throwMaxPower : 0f;

    public event Action<bool> OnAimStateChanged;
    public event Action<float> OnThrowPowerChanged;

    private void Update()
    {
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
        if (_state == HAND_STATE_ENUM.AIMING)
        {
            _currentPower += Time.deltaTime * _chargeSpeed;
            _currentPower = Mathf.Clamp(_currentPower, 0, _throwMaxPower);
            OnThrowPowerChanged?.Invoke(NormalizedPower);
        }
    }
    public void OnAimAction(bool value)
    {
        if (value)
            Aim();
        else
        {
            if (_state == HAND_STATE_ENUM.AIMING)
            {
                _state = HAND_STATE_ENUM.THROW;
                _animator.SetTrigger("Throw");
            }
        }
    }
    public void Aim()
    {
        if (_state != HAND_STATE_ENUM.HOLDING || _grapItem == null)
            return;

        _state = HAND_STATE_ENUM.AIMING;

        _animator.SetLayerWeight(1, 1);
        _animator.SetTrigger("Aim");
        
        _recoveryTime = 1;
        _currentPower = 0f;

        OnAimStateChanged?.Invoke(true);
    }
    public void OnAimCalcelAction()
    {
        if (_state != HAND_STATE_ENUM.AIMING)
            return;

        _state = HAND_STATE_ENUM.HOLDING;
        _currentPower = 0f;

        _animator.SetTrigger("AimCancel");
        _animator.SetLayerWeight(1, 0);

        OnAimStateChanged?.Invoke(false);
    }
    public void ThrowItem()
    {
        Vector3 throwDirection = _throwDirectionTrans.forward;

        _grapItem.transform.SetParent(null, true);
        _grapItem.Throw(_throwDirectionTrans.forward, _currentPower);

        _grapItem = null;
        _currentPower = 0f;
        _state = HAND_STATE_ENUM.EMPTY;

        OnThrowPowerChanged?.Invoke(0f);
        OnAimStateChanged?.Invoke(false);
    }
    public void GrapItem(GrapItem grapItem)
    {
        if (grapItem == null || _state == HAND_STATE_ENUM.AIMING || _state == HAND_STATE_ENUM.THROW)
            return;

        if (_grapItem != null)
        {
            _grapItem.transform.parent = null;
            _grapItem.Release();
        }

        _grapItem = grapItem;

        _state = HAND_STATE_ENUM.GRAPPING;

        _animator.SetLayerWeight(1, 1);
        _animator.SetTrigger("Grap");
    }
    private void Grap()
    {
        _grapItem.transform.parent = _itemGrapPivot;
        _grapItem.transform.localPosition = Vector3.zero;
        _grapItem.Grapped();

        _state = HAND_STATE_ENUM.HOLDING;
    }
}
