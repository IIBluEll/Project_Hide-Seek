using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterRotationController : MonoBehaviour
{
    [SerializeField] private Transform _playerBody;
    [SerializeField] private Transform _cameraTrans;
    [SerializeField] private MoveController _moveController;

    [SerializeField] private float _mouseSensitive;

    [SerializeField] private float _minPitch = -50f;
    [SerializeField] private float _maxPitch = 60f;
    [SerializeField] private float _standingCameraHeight = 1.55f;
    [SerializeField] private float _crouchCameraHeight = 1.05f;
    [SerializeField] private float _cameraHeightSmoothTime = 0.12f;

    private float _yaw;
    private float _pitch;
    private float _targetCameraHeight;
    private float _cameraHeightVelocity;

    private void Awake()
    {
        if (_playerBody == null)
            _playerBody = transform;

        if (_moveController == null)
            _moveController = GetComponent<MoveController>();

        if (_cameraTrans.parent != _playerBody)
            _cameraTrans.SetParent(_playerBody, false);

        _yaw = _playerBody.localEulerAngles.y;
        _pitch = _cameraTrans.localEulerAngles.x;

        SetCameraHeight(_moveController.Posture, true);
    }

    private void OnEnable()
    {
        if (_moveController != null)
            _moveController.OnPostureChanged += OnPostureChangedActioned;
    }

    private void OnDisable()
    {
        if (_moveController != null)
            _moveController.OnPostureChanged -= OnPostureChangedActioned;
    }

    private void LateUpdate()
    {
        Vector3 localPosition = _cameraTrans.localPosition;
        localPosition.y = Mathf.SmoothDamp(
            localPosition.y,
            _targetCameraHeight,
            ref _cameraHeightVelocity,
            _cameraHeightSmoothTime);
        _cameraTrans.localPosition = localPosition;
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

    public void SetYRotation(Vector3 rotation)
    {
        _yaw = rotation.y;
        _pitch = Mathf.Clamp(rotation.x, _minPitch, _maxPitch);

        _playerBody.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraTrans.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void OnPostureChangedActioned(POSTURE_STATE_ENUM posture)
    {
        SetCameraHeight(posture, false);
    }

    private void SetCameraHeight(POSTURE_STATE_ENUM posture, bool immediately)
    {
        _targetCameraHeight = posture == POSTURE_STATE_ENUM.CROUCH
            ? _crouchCameraHeight
            : _standingCameraHeight;

        if (!immediately)
            return;

        Vector3 localPosition = _cameraTrans.localPosition;
        localPosition.y = _targetCameraHeight;
        _cameraTrans.localPosition = localPosition;
    }
}
