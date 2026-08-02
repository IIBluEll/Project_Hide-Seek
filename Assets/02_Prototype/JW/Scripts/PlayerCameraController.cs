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

    [Space()]
    [SerializeField] private float _standingCameraHeight = 1.55f;
    [SerializeField] private float _crouchCameraHeight = 1.05f;
    [SerializeField] private float _cameraHeightSmoothTime = 0.12f;

    [Space()]
    [SerializeField, Range(0f, 1f)] private float _crouchWalkShakeIntensity = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _walkShakeIntensity = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _runShakeIntensity = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _airShakeIntensity = 0.1f; 

    private float _pitch;
    private float _currentShakeIntensity;

    private Vector3 _stableCameraLocalPosition;
    private Vector3 _cameraHeadLocalPosition;

    private void Awake()
    {
        _stableCameraLocalPosition = transform.InverseTransformPoint(_camera.position);
        _cameraHeadLocalPosition = _headBoneTrans.InverseTransformPoint(_camera.position);
    }

    private void LateUpdate()
    {
        ShakeCameraTransform();
        ApplyCameraRotation(_pitch,0,0);
    }

    private void ShakeCameraTransform()
    {
        Vector3 stablePosition = transform.TransformPoint(_stableCameraLocalPosition);
        Vector3 animatedPosition = _headBoneTrans.TransformPoint(_cameraHeadLocalPosition);
        _camera.position = Vector3.Lerp(stablePosition, animatedPosition, _currentShakeIntensity);
    }
    private void ApplyCameraRotation(float x, float y, float z)
    {
        _cameraTrans.rotation = transform.rotation * Quaternion.Euler(_pitch, 0f, 0f);
    }

    public void RotateXAxis(float value)
    {
        _pitch -= value * _mouseSensitive;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
    }
    public void SetRotation(float angle)
    {
        _pitch = Mathf.Clamp(angle, _minPitch, _maxPitch);
    }

    public void SetShakeIntensity(POSTURE_STATE_ENUM posture, LOCOMOTION_STATE_ENUM locomotion)
    {
        if (locomotion == LOCOMOTION_STATE_ENUM.IDLE)
        {
            _currentShakeIntensity = 0f;
            return;
        }

        if (locomotion == LOCOMOTION_STATE_ENUM.AIR)
        {
            _currentShakeIntensity = _airShakeIntensity;
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
        Vector3 cameraTrans = _cameraTrans.position;
        cameraTrans.y = posture == POSTURE_STATE_ENUM.STANDING ? _standingCameraHeight : _crouchCameraHeight;

        _cameraTrans.position = cameraTrans;
    }
}
