using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIMovementTester : MonoBehaviour
    {
        [SerializeField] private ChaseAIMovement _chaseAIMovement;
        [SerializeField] private List<Transform> _patrolPoints = new();

        [SerializeField, Min(0f)] private float _waitTime = 1f;

        private int _currentPointIndex;
        private float _waitTimer;
        private bool _isWaiting;
        private bool _isRunning;

        private void Start()
        {
            if ( _chaseAIMovement == null )
            {
                Debug.LogError(
                    "[ChaseAIMovementTester] ChaseAIMovement가 할당되지 않았습니다." ,
                    this);

                return;
            }

            if ( _patrolPoints.Count == 0 )
            {
                Debug.LogError(
                    "[ChaseAIMovementTester] 순찰 지점이 없습니다." ,
                    this);

                return;
            }

            _isRunning = true;
            RequestCurrentPoint();
        }

        private void Update()
        {
            if ( !_isRunning )
            {
                return;
            }

            if ( _isWaiting )
            {
                UpdateWaiting();
                return;
            }

            CHASE_AI_MOVE_STATUS moveStatus =
                _chaseAIMovement.UpdateMovement(Time.deltaTime);

            switch ( moveStatus )
            {
                case CHASE_AI_MOVE_STATUS.ARRIVED:
                    Debug.Log(
                        $"[MovementTester] {_currentPointIndex}번 지점 도착" ,
                        this);

                    WaitForNextPoint();
                    break;

                case CHASE_AI_MOVE_STATUS.PATH_FAILED:
                    Debug.LogWarning(
                        $"[MovementTester] {_currentPointIndex}번 지점 경로 실패" ,
                        this);

                    WaitForNextPoint();
                    break;

                case CHASE_AI_MOVE_STATUS.STUCK:
                    Debug.LogWarning(
                        $"[MovementTester] {_currentPointIndex}번 지점 이동 중 끼임" ,
                        this);

                    WaitForNextPoint();
                    break;
            }
        }

        private void RequestCurrentPoint()
        {
            Transform targetPoint = _patrolPoints[_currentPointIndex];

            if ( targetPoint == null )
            {
                Debug.LogWarning(
                    $"[MovementTester] {_currentPointIndex}번 지점이 비어 있습니다." ,
                    this);

                WaitForNextPoint();
                return;
            }

            CHASE_AI_MOVE_REQUEST_RESULT requestResult =
                _chaseAIMovement.TrySetDestination(
                    targetPoint.position,
                    out Vector3 correctedDestination);

            if ( requestResult != CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                Debug.LogWarning(
                    $"[MovementTester] 목적지 요청 실패: {requestResult}" ,
                    this);

                WaitForNextPoint();
                return;
            }

            Debug.Log(
                $"[MovementTester] {_currentPointIndex}번 지점으로 이동: " +
                $"{correctedDestination}" ,
                this);
        }

        private void WaitForNextPoint()
        {
            _currentPointIndex =
                ( _currentPointIndex + 1 ) % _patrolPoints.Count;

            _waitTimer = _waitTime;
            _isWaiting = true;
        }

        private void UpdateWaiting()
        {
            _waitTimer -= Time.deltaTime;

            if ( _waitTimer > 0f )
            {
                return;
            }

            _isWaiting = false;
            RequestCurrentPoint();
        }

        private void OnDrawGizmosSelected()
        {
            if ( _patrolPoints == null || _patrolPoints.Count == 0 )
            {
                return;
            }

            Gizmos.color = Color.green;

            for ( int index = 0; index < _patrolPoints.Count; index++ )
            {
                Transform currentPoint = _patrolPoints[index];

                if ( currentPoint == null )
                {
                    continue;
                }

                Gizmos.DrawWireSphere(currentPoint.position , 0.25f);

                Transform nextPoint =
                    _patrolPoints[(index + 1) % _patrolPoints.Count];

                if ( nextPoint != null )
                {
                    Gizmos.DrawLine(
                        currentPoint.position ,
                        nextPoint.position);
                }
            }
        }
    }
}
