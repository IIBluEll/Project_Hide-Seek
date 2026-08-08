using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterRotationController : MonoBehaviour
{
    private Transform _playerBody;
    private float _mouseSensitive;

    private float _yaw;

    private void Awake()
    {
        _mouseSensitive = PlayerPrefs.GetFloat(HashKey.YAW_SENSITIVE, ConstValue.YAW_SENSITIVE_DEFAULT);
    }

    public void Init(Transform body)
    {
        _playerBody = body;
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
