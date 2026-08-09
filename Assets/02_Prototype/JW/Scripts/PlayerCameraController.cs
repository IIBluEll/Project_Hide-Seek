using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTrans;
    [SerializeField] private Transform _camera;
    [SerializeField] private Transform _headBoneTrans;

    [Space()]
    [SerializeField] private float _mouseSensitive;
    [SerializeField] private float _minPitch = -50f;
    [SerializeField] private float _maxPitch = 60f;
    [SerializeField, Min(0f)] private float _rotationSmoothSpeed = 30f;

    [Space()]
    [SerializeField] private float _standingCameraHeight = 1.55f;
    [SerializeField] private float _crouchCameraHeight = 1.05f;
    [SerializeField] private float _proneCameraHeight = 0.55f;

    [Space()]
    [SerializeField, Range(0f, 1f)] private float _crouchWalkShakeIntensity = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _walkShakeIntensity = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _runShakeIntensity = 0.6f;

    private float _pitch;
    private float _targetPitch;
    public float _currentShakeIntensity;

    private Vector3 _stableCameraLocalPosition;
    private Vector3 _cameraHeadLocalPosition;

    private void Awake()
    {
        _stableCameraLocalPosition = _camera.localPosition;
        _cameraHeadLocalPosition = _headBoneTrans.InverseTransformPoint(_camera.position);
        _targetPitch = _pitch;
    }

    private void LateUpdate()
    {
        float interpolation = CalculateRotationInterpolation(Time.unscaledDeltaTime);
        _pitch = Mathf.LerpAngle(_pitch, _targetPitch, interpolation);

        ShakeCameraTransform();
        ApplyCameraRotation(_pitch, 0f, 0f);
    }

    private void ShakeCameraTransform()
    {
        Vector3 animatedPosition = _headBoneTrans.TransformPoint(_cameraHeadLocalPosition);
        Vector3 animatedLocalPosition = _cameraTrans.InverseTransformPoint(animatedPosition);

        _camera.localPosition = Vector3.Lerp(
            _stableCameraLocalPosition,
            animatedLocalPosition,
            _currentShakeIntensity);
    }
    private void ApplyCameraRotation(float x, float y, float z)
    {
        _cameraTrans.rotation = transform.rotation * Quaternion.Euler(_pitch, 0f, 0f);
    }

    public void RotateXAxis(float value)
    {
        _targetPitch -= value * _mouseSensitive;
        _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
    }

    public void SetRotation(float angle)
    {
        _pitch = Mathf.Clamp(angle, _minPitch, _maxPitch);
        _targetPitch = _pitch;
    }

    private float CalculateRotationInterpolation(float deltaTime)
    {
        if (_rotationSmoothSpeed <= 0f)
            return 1f;

        return 1f - Mathf.Exp(-_rotationSmoothSpeed * deltaTime);
    }

    public void SetShakeIntensity(POSTURE_STATE_ENUM posture, LOCOMOTION_STATE_ENUM locomotion)
    {
        if (locomotion == LOCOMOTION_STATE_ENUM.IDLE)
        {
            _currentShakeIntensity = 1f;
            return;
        }

        if (posture == POSTURE_STATE_ENUM.CROUCH)
        {
            _currentShakeIntensity = _crouchWalkShakeIntensity;
            return;
        }

        _currentShakeIntensity = locomotion == LOCOMOTION_STATE_ENUM.RUN ? _runShakeIntensity : _walkShakeIntensity;
    }
    public void SetCameraHeight(POSTURE_STATE_ENUM posture)
    {
        Vector3 cameraPosition = _cameraTrans.localPosition;

        switch (posture)
        {
            case POSTURE_STATE_ENUM.STANDING:
                cameraPosition.y = _standingCameraHeight;
                break;
            case POSTURE_STATE_ENUM.CROUCH:
                cameraPosition.y = _crouchCameraHeight;
                break;
            case POSTURE_STATE_ENUM.PRONE:
                cameraPosition.y = _proneCameraHeight;
                break;
        }

        _cameraTrans.localPosition = cameraPosition;
    }
}
