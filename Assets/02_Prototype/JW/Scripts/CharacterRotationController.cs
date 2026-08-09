using UnityEngine;

public class CharacterRotationController : MonoBehaviour
{
    private Transform _playerBody;
    [SerializeField] private float _mouseSensitive;
    [SerializeField, Min(0f)] private float _rotationSmoothSpeed = 30f;

    private float _yaw;
    private float _targetYaw;

    private void Update()
    {
        if (_playerBody == null)
            return;

        float interpolation = CalculateInterpolation(Time.unscaledDeltaTime);
        _yaw = Mathf.LerpAngle(_yaw, _targetYaw, interpolation);
        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    public void Init(Transform body)
    {
        _playerBody = body;
        _yaw = _playerBody.localEulerAngles.y;
        _targetYaw = _yaw;
    }

    public void Rotate(Vector2 value)
    {
        _targetYaw += value.x * _mouseSensitive;
    }

    public void SetYRotation(Vector3 rotation)
    {
        _yaw = rotation.y;
        _targetYaw = _yaw;
        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private float CalculateInterpolation(float deltaTime)
    {
        if (_rotationSmoothSpeed <= 0f)
            return 1f;

        return 1f - Mathf.Exp(-_rotationSmoothSpeed * deltaTime);
    }
}
