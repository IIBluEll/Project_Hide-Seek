using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    //NavMesh 위에 도달 가능한 방향 우선 및 무작위 수색 지점을 생성하고 순서대로 제공하는 역할
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
            Vector3 preferredDirection ,
            float predictionDistance ,
            float directionalPointRatio ,
            float directionalSearchAngle ,
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

            Vector3 normalizedPreferredDirection = preferredDirection;
            normalizedPreferredDirection.y = 0f;

            if ( normalizedPreferredDirection.sqrMagnitude > Mathf.Epsilon )
            {
                normalizedPreferredDirection.Normalize();
            }
            else
            {
                normalizedPreferredDirection = Vector3.zero;
            }

            int directionalPointCount = normalizedPreferredDirection != Vector3.zero
                ? Mathf.Clamp(Mathf.CeilToInt(targetPointCount * Mathf.Clamp01(directionalPointRatio)) , 0 , targetPointCount)
                : 0;

            float validPredictionDistance = Mathf.Clamp(predictionDistance , 0f , validSearchRadius);

            if ( directionalPointCount > 0 && validPredictionDistance > 0f )
            {
                Vector3 predictedPosition = centerPosition + normalizedPreferredDirection * validPredictionDistance;

                TryAddSearchPoint(
                    predictedPosition ,
                    centerPosition ,
                    validSearchRadius ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    ref pathStartPosition);
            }

            float validDirectionalSearchAngle = Mathf.Clamp(directionalSearchAngle , 0f , 180f);
            float minimumDirectionalDistance = Mathf.Min(validMinimumPointDistance , validSearchRadius);

            for ( int attemptIndex = 0; attemptIndex < maximumAttemptCount; attemptIndex++ )
            {
                if ( SEARCH_POINTS.Count >= directionalPointCount )
                {
                    break;
                }

                float directionAngle = Random.Range(-validDirectionalSearchAngle * 0.5f , validDirectionalSearchAngle * 0.5f);
                Vector3 searchDirection = Quaternion.AngleAxis(directionAngle , Vector3.up) * normalizedPreferredDirection;
                float searchDistance = Random.Range(minimumDirectionalDistance , validSearchRadius);
                Vector3 directionalPosition = centerPosition + searchDirection * searchDistance;

                TryAddSearchPoint(
                    directionalPosition ,
                    centerPosition ,
                    validSearchRadius ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    ref pathStartPosition);
            }

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

                TryAddSearchPoint(
                    randomPosition ,
                    centerPosition ,
                    validSearchRadius ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    ref pathStartPosition);
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

        private bool TryAddSearchPoint(
            Vector3 candidatePosition ,
            Vector3 centerPosition ,
            float searchRadius ,
            float minimumPointDistance ,
            float sampleRadius ,
            int areaMask ,
            ref Vector3 pathStartPosition)
        {
            bool hasNavMeshPosition = NavMesh.SamplePosition(
                candidatePosition ,
                out NavMeshHit candidateHit ,
                sampleRadius ,
                areaMask);

            if ( !hasNavMeshPosition )
            {
                return false;
            }

            Vector3 sampledPosition = candidateHit.position;

            if ( !IsInsideSearchRadius(sampledPosition , centerPosition , searchRadius) )
            {
                return false;
            }

            if ( IsTooCloseToExistingPoint(sampledPosition , minimumPointDistance) )
            {
                return false;
            }

            if ( !HasCompletePath(pathStartPosition , sampledPosition , areaMask) )
            {
                return false;
            }

            SEARCH_POINTS.Add(sampledPosition);
            pathStartPosition = sampledPosition;

            return true;
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
