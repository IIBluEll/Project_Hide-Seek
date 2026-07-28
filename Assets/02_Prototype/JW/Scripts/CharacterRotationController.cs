using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterRotationController : MonoBehaviour
{
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Transform _cameraTrans;

    [SerializeField] private float _mouseSensitive;

    [SerializeField] private float _minPitch = -50f;
    [SerializeField] private float _maxPitch = 60f;

    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        if (_playerBody == null)
            _playerBody = transform;

        _yaw = _playerBody.localEulerAngles.y;
        _pitch = _cameraTrans.localEulerAngles.x;
    }

    void OnLook(InputValue value)
    {
        Vector2 mouseInput = value.Get<Vector2>();

        _yaw += mouseInput.x * _mouseSensitive;
        _pitch += mouseInput.y * _mouseSensitive * -1;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraTrans.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
