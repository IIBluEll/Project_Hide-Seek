using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    //NavMesh 위에 도달 가능한 랜덤 수색 지점을 생성하고 순서대로 제공하는 역할
    public sealed class ChaseAISearch
    {
        private const int MIN_GENERATION_ATTEMPT_COUNT_PER_POINT = 1;

        private readonly List<Vector3> SEARCH_POINTS = new();
        private readonly NavMeshPath SEARCH_PATH = new();

        private int _currentPointIndex;

        public IReadOnlyList<Vector3> SearchPoints => SEARCH_POINTS;
        public int CurrentPointIndex => _currentPointIndex;
        public int PointCount => SEARCH_POINTS.Count;

        public bool HasCurrentPoint => _currentPointIndex >= 0 && _currentPointIndex < SEARCH_POINTS.Count;

        public bool BuildSearchPoints(
            Vector3 startPosition ,
            Vector3 centerPosition ,
            float searchRadius ,
            int searchPointCount ,
            float minimumPointDistance ,
            float sampleRadius ,
            int areaMask ,
            int generationAttemptCountPerPoint)
        {
            Clear();

            int targetPointCount = Mathf.Max(0, searchPointCount);
            float validSearchRadius = Mathf.Max(0f, searchRadius);
            float validMinimumPointDistance = Mathf.Max(0f, minimumPointDistance);
            float validSampleRadius = Mathf.Max(0.1f, sampleRadius);

            if ( targetPointCount == 0 || validSearchRadius <= 0f )
            {
                return false;
            }

            bool hasStartPosition = NavMesh.SamplePosition(
                startPosition,
                out NavMeshHit startHit,
                validSampleRadius,
                areaMask);

            if ( !hasStartPosition )
            {
                return false;
            }

            int validAttemptCountPerPoint = Mathf.Max(MIN_GENERATION_ATTEMPT_COUNT_PER_POINT, generationAttemptCountPerPoint);

            int maximumAttemptCount = targetPointCount * validAttemptCountPerPoint;

            Vector3 pathStartPosition = startHit.position;

            for ( int attemptIndex = 0; attemptIndex < maximumAttemptCount; attemptIndex++ )
            {
                if ( SEARCH_POINTS.Count >= targetPointCount )
                {
                    break;
                }

                Vector2 randomOffset = Random.insideUnitCircle * validSearchRadius;

                Vector3 randomPosition = centerPosition +
                    new Vector3(
                        randomOffset.x,
                        0f,
                        randomOffset.y);

                bool hasNavMeshPosition = NavMesh.SamplePosition(
                    randomPosition,
                    out NavMeshHit candidateHit,
                    validSampleRadius,
                    areaMask);

                if ( !hasNavMeshPosition )
                {
                    continue;
                }

                Vector3 candidatePosition = candidateHit.position;

                if ( !IsInsideSearchRadius(candidatePosition, centerPosition, validSearchRadius))
                {
                    continue;
                }

                if ( IsTooCloseToExistingPoint(candidatePosition, validMinimumPointDistance))
                {
                    continue;
                }

                if ( !HasCompletePath(pathStartPosition, candidatePosition, areaMask) )
                {
                    continue;
                }

                SEARCH_POINTS.Add(candidatePosition);
                pathStartPosition = candidatePosition;
            }

            return SEARCH_POINTS.Count > 0;
        }

        public bool TryGetCurrentPoint(out Vector3 searchPoint)
        {
            searchPoint = Vector3.zero;

            if ( !HasCurrentPoint )
            {
                return false;
            }

            searchPoint = SEARCH_POINTS[ _currentPointIndex ];

            return true;
        }

        public bool AdvanceToNextPoint()
        {
            if ( !HasCurrentPoint )
            {
                return false;
            }

            _currentPointIndex++;

            return HasCurrentPoint;
        }

        public void Clear()
        {
            SEARCH_POINTS.Clear();
            _currentPointIndex = 0;
        }

        private bool IsInsideSearchRadius(Vector3 candidatePosition, Vector3 centerPosition, float searchRadius)
        {
            Vector3 offset = candidatePosition - centerPosition;
            offset.y = 0f;

            return offset.sqrMagnitude <= searchRadius * searchRadius;
        }

        private bool IsTooCloseToExistingPoint(Vector3 candidatePosition, float minimumPointDistance)
        {
            float squaredMinimumDistance = minimumPointDistance * minimumPointDistance;

            for ( int pointIndex = 0; pointIndex < SEARCH_POINTS.Count; pointIndex++ )
            {
                Vector3 offset = SEARCH_POINTS[pointIndex] - candidatePosition;

                offset.y = 0f;

                if ( offset.sqrMagnitude < squaredMinimumDistance )
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCompletePath(Vector3 startPosition, Vector3 destination, int areaMask)
        {
            SEARCH_PATH.ClearCorners();

            bool hasPath = NavMesh.CalculatePath(
                startPosition,
                destination,
                areaMask,
                SEARCH_PATH);

            return hasPath && SEARCH_PATH.status == NavMeshPathStatus.PathComplete;
        }
    }
}