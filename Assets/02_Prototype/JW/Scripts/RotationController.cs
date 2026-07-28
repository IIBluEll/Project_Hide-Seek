using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct RotationSet
{
    public Vector3 CurrentRotation;
    public float Torque;
    public float MinAngle;
    public float MaxAngle;
}

public class RotationController : MonoBehaviour
{
    [SerializeField] private Transform _owner;

    [SerializeField] private RotationSet _horizontal;
    [SerializeField] private RotationSet _vertical;

    [SerializeField] private bool _isHorizontalOn;
    [SerializeField] private bool _isVerticalOn;

    void OnLook(InputValue value)
    {
        if (_isHorizontalOn)
        {
            float xAxis = value.Get<Vector2>().x;

            _horizontal.CurrentRotation.y += xAxis * Time.deltaTime * _horizontal.Torque;
            //_horizontal.CurrentRotation.y = Mathf.Clamp(_horizontal.CurrentRotation.y, _horizontal.MinAngle, _horizontal.MaxAngle);

            _owner.rotation = Quaternion.Euler(_horizontal.CurrentRotation);
        }

        if (_isVerticalOn)
        {
            float yAxis = value.Get<Vector2>().y;

            _vertical.CurrentRotation.x -= yAxis * Time.deltaTime * _vertical.Torque;
            _vertical.CurrentRotation.x = Mathf.Clamp(_vertical.CurrentRotation.x, _vertical.MinAngle, _vertical.MaxAngle);

            _owner.rotation = Quaternion.Euler(_vertical.CurrentRotation);
        }
    }
}
