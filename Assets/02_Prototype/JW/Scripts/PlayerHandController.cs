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
    [SerializeField] private Transform _itemGrapPivot;
    [SerializeField] private Transform _throwDirectionTrans;

    [SerializeField] private float _throwMaxPower;
    [SerializeField, Min(0)] private float _minimumPower;

    [SerializeField] private float _chargeSpeed;

    private GrappableItem _grapItem;
    private float _currentPower;
    private HAND_STATE_ENUM _state = HAND_STATE_ENUM.EMPTY;
    private HAND_STATE_ENUM _recoveryState;

    public float NormalizedPower => _throwMaxPower > 0f ? _currentPower / _throwMaxPower : 0f;

    public event Action<bool> OnAimStateChanged;
    public event Action<float> OnThrowPowerChanged;

    public event Action<HAND_STATE_ENUM> OnStateChangeEvent;

    private void Update()
    {
        if(_state == HAND_STATE_ENUM.GRAPPING)
        {
            Grap();
            _recoveryState = HAND_STATE_ENUM.HOLDING;
            _state = HAND_STATE_ENUM.RECOVERY;
            
        }
        if (_state == HAND_STATE_ENUM.RECOVERY)
        {
            _state = _recoveryState;
        }
        if (_state == HAND_STATE_ENUM.THROW)
        {
            ThrowItem();
            _recoveryState = HAND_STATE_ENUM.EMPTY;
            _state = HAND_STATE_ENUM.RECOVERY;
        }
        if (_state == HAND_STATE_ENUM.AIMING)
        {
            _currentPower += Time.deltaTime * _chargeSpeed;
            _currentPower = Mathf.Clamp(_currentPower, _minimumPower, _throwMaxPower);
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
                OnStateChangeEvent?.Invoke(_state);
            }
        }
    }
    public void Aim()
    {
        if (_state != HAND_STATE_ENUM.HOLDING || _grapItem == null)
            return;

        _state = HAND_STATE_ENUM.AIMING;

        _currentPower = _minimumPower;

        OnAimStateChanged?.Invoke(true);
    }
    public void OnAimCalcelAction()
    {
        if (_state != HAND_STATE_ENUM.AIMING)
            return;

        _state = HAND_STATE_ENUM.HOLDING;
        _currentPower = _minimumPower;

        OnAimStateChanged?.Invoke(false);
    }
    public void ThrowItem()
    {
        Vector3 throwDirection = _throwDirectionTrans.forward;

        _grapItem.transform.SetParent(null, true);
        _grapItem.Throw(_throwDirectionTrans.forward + Vector3.up, _currentPower);

        _grapItem = null;
        _currentPower = _minimumPower;
        _state = HAND_STATE_ENUM.EMPTY;

        OnThrowPowerChanged?.Invoke(0f);
        OnAimStateChanged?.Invoke(false);
    }
    public void GrapItem(GrappableItem grapItem)
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
        OnStateChangeEvent?.Invoke(_state);
    }
    private void Grap()
    {
        _grapItem.transform.parent = _itemGrapPivot;
        _grapItem.transform.localPosition = Vector3.zero;
        _grapItem.Grapped();

        _state = HAND_STATE_ENUM.HOLDING;
    }
}
