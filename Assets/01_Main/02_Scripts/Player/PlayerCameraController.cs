using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Transform _camera;

    private float _pitchSensitive;
    private float _minPitch = -50f;
    private float _maxPitch = 60f;

    [Space()]
    [SerializeField] private float _standingCameraHeight = 1.55f;
    [SerializeField] private float _crouchCameraHeight = 1.05f;

    [Space()]
    [SerializeField, Range(0f, 3f)] private float _crouchWalkShakeIntensity = 0.15f;
    [SerializeField, Range(0f, 3f)] private float _walkShakeIntensity = 0.35f;
    [SerializeField, Range(0f, 3f)] private float _runShakeIntensity = 0.6f;
    [SerializeField] private float _crouchWalkShakeFrequency = 4.5f;
    [SerializeField] private float _walkShakeFrequency = 6f;
    [SerializeField] private float _runShakeFrequency = 8f;
    [SerializeField, Range(0f, 0.1f)] private float _shakeVerticalAmplitude = 0.018f;
    [SerializeField, Range(0f, 0.1f)] private float _shakeHorizontalAmplitude = 0.003f;
    [SerializeField] private float _shakeSmoothSpeed = 8f;

    [SerializeField] private float _pitch;
    public float _currentShakeIntensity;
    private float _currentShakeFrequency;
    private float _targetShakeIntensity;
    private float _targetShakeFrequency;
    private float _shakeTimer;
    private Vector3 _defaultCameraLocalPosition;
    private Transform _cameraPositionOverride;

    private void Awake()
    {
        _defaultCameraLocalPosition = _camera.localPosition;
        _pitchSensitive = PlayerPrefs.GetFloat(HashKey.PITCH_SENSITIVE, ConstValue.PITCH_SENSITIVE_DEFAULT);
    }
    private void LateUpdate()
    {
        ApplyCameraPositionOverride();
        ShakeCameraTransform();
        ApplyCameraRotation(_pitch, 0f, 0f);
    }

    private void ApplyCameraPositionOverride()
    {
        if (_cameraPositionOverride == null)
            return;

        _cameraParent.position = _cameraPositionOverride.position;
    }

    private void ShakeCameraTransform()
    {
        if (_cameraPositionOverride != null)
            return;

        _currentShakeIntensity = Mathf.Lerp(
            _currentShakeIntensity,
            _targetShakeIntensity,
            Time.deltaTime * _shakeSmoothSpeed);

        _currentShakeFrequency = Mathf.Lerp(
            _currentShakeFrequency,
            _targetShakeFrequency,
            Time.deltaTime * _shakeSmoothSpeed);

        if (_currentShakeIntensity <= 0.001f || _currentShakeFrequency <= 0.001f)
        {
            _shakeTimer = 0f;
            _camera.localPosition = Vector3.Lerp(
                _camera.localPosition,
                _defaultCameraLocalPosition,
                Time.deltaTime * _shakeSmoothSpeed);
            return;
        }

        _shakeTimer += Time.deltaTime * _currentShakeFrequency;

        float verticalBob = Mathf.Sin(_shakeTimer);

        Vector3 shakeOffset = new Vector3(
            Mathf.Sin(_shakeTimer * 0.5f) * _shakeHorizontalAmplitude,
            verticalBob * _shakeVerticalAmplitude,
            0f);

        _camera.localPosition = Vector3.Lerp(
            _camera.localPosition,
            _defaultCameraLocalPosition + shakeOffset * _currentShakeIntensity,
            Time.deltaTime * _shakeSmoothSpeed);
    }
    private void ApplyCameraRotation(float x, float y, float z)
    {
        _cameraParent.rotation = transform.rotation * Quaternion.Euler(_pitch, 0f, 0f);
    }
    public void RotateXAxis(float value)
    {
        _pitch -= value * _pitchSensitive;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
    }

    public void SetRotation(float angle)
    {
        _pitch = Mathf.Clamp(angle, _minPitch, _maxPitch);
    }

    public void SetCameraPositionOverride(Transform cameraPositionOverride)
    {
        _cameraPositionOverride = cameraPositionOverride;

        if (_cameraPositionOverride != null)
        {
            _cameraParent.position = _cameraPositionOverride.position;
            _camera.transform.localPosition = Vector3.zero;
        }
    }

    public void ClearCameraPositionOverride()
    {
        _cameraPositionOverride = null;
        _camera.localPosition = _defaultCameraLocalPosition;
        SetCameraHeight(POSTURE_STATE_ENUM.STANDING);
    }

    public void SetShakeIntensity(POSTURE_STATE_ENUM posture, LOCOMOTION_STATE_ENUM locomotion)
    {
        if (locomotion == LOCOMOTION_STATE_ENUM.IDLE)
        {
            _targetShakeIntensity = 0f;
            _targetShakeFrequency = 0f;
            return;
        }

        if (posture == POSTURE_STATE_ENUM.CROUCH)
        {
            _targetShakeIntensity = _crouchWalkShakeIntensity;
            _targetShakeFrequency = _crouchWalkShakeFrequency;
            return;
        }

        if (locomotion == LOCOMOTION_STATE_ENUM.RUN)
        {
            _targetShakeIntensity = _runShakeIntensity;
            _targetShakeFrequency = _runShakeFrequency;
        }
        else
        {
            _targetShakeIntensity = _walkShakeIntensity;
            _targetShakeFrequency = _walkShakeFrequency;
        }
    }
    public void SetCameraHeight(POSTURE_STATE_ENUM posture)
    {
        Vector3 cameraPosition = _cameraParent.localPosition;

        switch (posture)
        {
            case POSTURE_STATE_ENUM.STANDING:
                cameraPosition.y = _standingCameraHeight;
                break;
            case POSTURE_STATE_ENUM.CROUCH:
                cameraPosition.y = _crouchCameraHeight;
                break;
        }

        _cameraParent.localPosition = cameraPosition;
    }
}
