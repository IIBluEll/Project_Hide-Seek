using System.Collections;
using UnityEngine;

namespace HideSeek.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SlidingDoor : MonoBehaviour, IInteractable
    {
        [Header("Door")]
        [SerializeField] private Transform _doorTrans;
        [SerializeField] private Vector3 _openLocalOffset;
        [SerializeField, Min(0.01f)] private float _moveDuration = 0.75f;
        [SerializeField] private bool _startOpen;

        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;
        private Coroutine _moveCoroutine;
        private bool _isOpen;
        private bool _targetOpen;

        public string InteractionPrompt => _isOpen ? "문 닫기" : "문 열기";
        public bool IsOpen => _isOpen;
        public bool IsMoving => _moveCoroutine != null;
        public bool IsOpenRequested => _targetOpen;

        private void Awake()
        {
            if ( _doorTrans == null )
            {
                Debug.LogError($"[{nameof(SlidingDoor)}] 이동할 문 Transform이 없습니다." , this);
                enabled = false;

                return;
            }

            _closedLocalPosition = _doorTrans.localPosition;
            _openLocalPosition = _closedLocalPosition + _openLocalOffset;
            _isOpen = _startOpen;
            _targetOpen = _startOpen;
            _doorTrans.localPosition = _startOpen
                ? _openLocalPosition
                : _closedLocalPosition;
        }

        private void OnDisable()
        {
            if ( _moveCoroutine == null )
            {
                return;
            }

            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
            _isOpen = _targetOpen;
            _doorTrans.localPosition = _isOpen
                ? _openLocalPosition
                : _closedLocalPosition;
        }

        public bool CanInteract(PlayerInteractionController playerInteractor)
        {
            return enabled && _doorTrans != null && !IsMoving;
        }

        public void InteractAct(PlayerInteractionController playerInteractor)
        {
            Toggle();
        }

        public void InteractRelease(PlayerInteractionController playerInteractor)
        {
        }

        public bool Open()
        {
            return SetOpen(true);
        }

        public bool Close()
        {
            return SetOpen(false);
        }

        public bool Toggle()
        {
            return SetOpen(!_targetOpen);
        }

        public bool SetOpen(bool isOpen)
        {
            if ( !enabled || _doorTrans == null || _targetOpen == isOpen )
            {
                return false;
            }

            _targetOpen = isOpen;

            if ( _moveCoroutine != null )
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = StartCoroutine(MoveDoor_cor(isOpen));

            return true;
        }

        private IEnumerator MoveDoor_cor(bool isOpen)
        {
            Vector3 startPosition = _doorTrans.localPosition;
            Vector3 targetPosition = isOpen
                ? _openLocalPosition
                : _closedLocalPosition;
            float elapsedTime = 0f;

            while ( elapsedTime < _moveDuration )
            {
                elapsedTime += Time.deltaTime;
                float movementRatio = Mathf.Clamp01(elapsedTime / _moveDuration);
                float easedRatio = Mathf.SmoothStep(0f , 1f , movementRatio);
                _doorTrans.localPosition = Vector3.LerpUnclamped(
                    startPosition ,
                    targetPosition ,
                    easedRatio);

                yield return null;
            }

            _doorTrans.localPosition = targetPosition;
            _isOpen = isOpen;
            _moveCoroutine = null;
        }

        private void OnValidate()
        {
            _moveDuration = Mathf.Max(0.01f , _moveDuration);
        }
    }
}
