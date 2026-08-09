using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Transform _camera;
    [SerializeField] private Transform _headBoneTrans;

    private float _pitchSensitive;
    private float _minPitch = -50f;
    private float _maxPitch = 60f;

    [Space()]
    [SerializeField] private float _standingCameraHeight = 1.55f;
    [SerializeField] private float _crouchCameraHeight = 1.05f;
    [SerializeField] private float _proneCameraHeight = 0.55f;

    [Space()]
    [SerializeField, Range(0f, 3f)] private float _crouchWalkShakeIntensity = 0.15f;
    [SerializeField, Range(0f, 3f)] private float _walkShakeIntensity = 0.35f;
    [SerializeField, Range(0f, 3f)] private float _runShakeIntensity = 0.6f;

    [SerializeField] private float _pitch;
    public float _currentShakeIntensity;

    private Vector3 _stableCameraLocalPosition;
    private Vector3 _cameraHeadLocalPosition;
    private Transform _cameraPositionOverride;

    private void Awake()
    {
        _stableCameraLocalPosition = _camera.localPosition;
        _cameraHeadLocalPosition = _headBoneTrans.InverseTransformPoint(_camera.position);
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

        Vector3 animatedPosition = _headBoneTrans.TransformPoint(_cameraHeadLocalPosition);
        Vector3 animatedLocalPosition = _cameraParent.InverseTransformPoint(animatedPosition);

        _camera.localPosition = Vector3.Lerp(
            _stableCameraLocalPosition,
            animatedLocalPosition,
            _currentShakeIntensity);
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
        _camera.position = _headBoneTrans.position;
        _stableCameraLocalPosition = _camera.localPosition;
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
        Vector3 cameraPosition = _cameraParent.localPosition;

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

        _cameraParent.localPosition = cameraPosition;
    }
}
