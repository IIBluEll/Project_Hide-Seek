using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterRotationController : MonoBehaviour
{
    [SerializeField] private Transform _playerBody;
    [SerializeField] private float _mouseSensitive;

    private float _yaw;

    private void Awake()
    {
        if (_playerBody == null)
            _playerBody = transform;

        _yaw = _playerBody.localEulerAngles.y;
    }

    public void Rotate(Vector2 value)
    {
        _yaw += value.x * _mouseSensitive;
        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }
    public void SetYRotation(Vector3 rotation)
    {
        _yaw = rotation.y;
        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }
}
