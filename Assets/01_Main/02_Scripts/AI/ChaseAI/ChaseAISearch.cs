using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    public enum CHASE_AI_SEARCH_POINT_SOURCE
    {
        PREDICTED_DIRECTION,
        DIRECTIONAL,
        ZONE_COVERAGE,
        HIDING_SPOT,
        RANDOM
    }

    // NavMesh 위에 도달 가능한 방향 우선, 공간 확인 및 무작위 수색 지점을 생성하고 순서대로 제공하는 역할
    public sealed class ChaseAISearch
    {
        private const int MIN_GENERATION_ATTEMPT_COUNT_PER_POINT = 1;

        private readonly List<Vector3> SEARCH_POINTS = new();
        private readonly List<CHASE_AI_SEARCH_POINT_SOURCE> SEARCH_POINT_SOURCES = new();
        private readonly List<AIHidingSpot> SEARCH_POINT_HIDING_SPOTS = new();
        private readonly List<AISearchPoint> ZONE_COVERAGE_CANDIDATES = new();
        private readonly NavMeshPath SEARCH_PATH = new();

        private IReadOnlyList<AIWorldZone> _zones = Array.Empty<AIWorldZone>();
        private AIWorldZone _activeSearchZone;
        private int _currentPointIndex;
        private int _zoneCoveragePointCount;
        private int _hidingSpotPointCount;
        private bool _isZoneRestricted;

        public IReadOnlyList<Vector3> SearchPoints => SEARCH_POINTS;
        public int CurrentPointIndex => _currentPointIndex;
        public int PointCount => SEARCH_POINTS.Count;
        public int ZoneCoveragePointCount => _zoneCoveragePointCount;
        public int HidingSpotPointCount => _hidingSpotPointCount;
        public bool HasCurrentPoint => _currentPointIndex >= 0 && _currentPointIndex < SEARCH_POINTS.Count;
        public bool IsZoneRestricted => _isZoneRestricted;
        public string ActiveSearchZoneName => _activeSearchZone != null ? _activeSearchZone.DisplayName : "NONE";
        public string HidingSpotCandidateName { get; private set; } = "NONE";
        public float HidingSpotInspectionChance { get; private set; }
        public float HidingSpotInspectionRoll { get; private set; } = -1f;
        public bool WasHidingSpotSelected { get; private set; }

        public void ConfigureZones(IReadOnlyList<AIWorldZone> zones)
        {
            _zones = zones ?? Array.Empty<AIWorldZone>();
        }

        public bool BuildSearchPoints(
            Vector3 startPosition ,
            Vector3 centerPosition ,
            float searchRadius ,
            int searchPointCount ,
            Vector3 preferredDirection ,
            float predictionDistance ,
            float directionalPointRatio ,
            float zoneCoveragePointRatio ,
            float directionalSearchAngle ,
            float minimumPointDistance ,
            float hidingSpotEvidenceDistance ,
            float sampleRadius ,
            int areaMask ,
            int generationAttemptCountPerPoint ,
            int requestedZoneId ,
            bool canInspectHidingSpot ,
            float hidingSpotInspectionChance ,
            bool shouldRestrictToZone)
        {
            Clear();

            int targetPointCount = Mathf.Max(0 , searchPointCount);
            float validSearchRadius = Mathf.Max(0f , searchRadius);
            float validMinimumPointDistance = Mathf.Max(0f , minimumPointDistance);
            float validHidingSpotEvidenceDistance = Mathf.Max(0f , hidingSpotEvidenceDistance);
            float validSampleRadius = Mathf.Max(0.1f , sampleRadius);

            if ( targetPointCount == 0 || validSearchRadius <= 0f )
            {
                return false;
            }

            ResolveActiveSearchZone(centerPosition , requestedZoneId , shouldRestrictToZone);

            bool hasStartPosition = NavMesh.SamplePosition(
                startPosition ,
                out NavMeshHit startHit ,
                validSampleRadius ,
                areaMask);

            if ( !hasStartPosition )
            {
                return false;
            }

            int validAttemptCountPerPoint = Mathf.Max(MIN_GENERATION_ATTEMPT_COUNT_PER_POINT , generationAttemptCountPerPoint);
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

            int directionalPointCount = CalculateDirectionalPointCount(
                targetPointCount ,
                directionalPointRatio ,
                normalizedPreferredDirection != Vector3.zero);

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
                    CHASE_AI_SEARCH_POINT_SOURCE.PREDICTED_DIRECTION ,
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

                float directionAngle = UnityEngine.Random.Range(-validDirectionalSearchAngle * 0.5f , validDirectionalSearchAngle * 0.5f);
                Vector3 searchDirection = Quaternion.AngleAxis(directionAngle , Vector3.up) * normalizedPreferredDirection;
                float searchDistance = UnityEngine.Random.Range(minimumDirectionalDistance , validSearchRadius);
                Vector3 directionalPosition = centerPosition + searchDirection * searchDistance;

                TryAddSearchPoint(
                    directionalPosition ,
                    centerPosition ,
                    validSearchRadius ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    CHASE_AI_SEARCH_POINT_SOURCE.DIRECTIONAL ,
                    ref pathStartPosition);
            }

            HidingSpotInspectionChance = Mathf.Clamp01(hidingSpotInspectionChance);

            AISearchPoint hidingSpotCandidate = canInspectHidingSpot && HidingSpotInspectionChance > 0f
                ? FindNearestHidingSpot(centerPosition , validHidingSpotEvidenceDistance)
                : null;

            HidingSpotCandidateName = hidingSpotCandidate != null
                ? hidingSpotCandidate.name
                : "NONE";
            HidingSpotInspectionRoll = hidingSpotCandidate != null
                ? UnityEngine.Random.value
                : -1f;
            WasHidingSpotSelected = hidingSpotCandidate != null &&
                HidingSpotInspectionRoll <= HidingSpotInspectionChance;

            if ( hidingSpotCandidate != null )
            {
                Debug.Log(
                    $"[ChaseAISearch] 은신처 조사 확률 판정: " +
                    $"Candidate={HidingSpotCandidateName}, " +
                    $"Chance={HidingSpotInspectionChance:P0}, " +
                    $"Roll={HidingSpotInspectionRoll:F2}, " +
                    $"Selected={WasHidingSpotSelected}");
            }

            if ( !WasHidingSpotSelected )
            {
                hidingSpotCandidate = null;
            }

            int reservedHidingSpotPointCount = hidingSpotCandidate != null ? 1 : 0;
            int availablePointCount = Mathf.Max(
                0 ,
                targetPointCount - SEARCH_POINTS.Count - reservedHidingSpotPointCount);
            int requestedCoveragePointCount = Mathf.Clamp(
                Mathf.RoundToInt(targetPointCount * Mathf.Clamp01(zoneCoveragePointRatio)) ,
                0 ,
                availablePointCount);

            AddZoneCoveragePoints(
                requestedCoveragePointCount ,
                centerPosition ,
                validSearchRadius ,
                validMinimumPointDistance ,
                validSampleRadius ,
                areaMask ,
                ref pathStartPosition);

            if ( hidingSpotCandidate != null && SEARCH_POINTS.Count < targetPointCount )
            {
                bool wasAdded = TryAddSearchPoint(
                    hidingSpotCandidate.Position ,
                    centerPosition ,
                    validHidingSpotEvidenceDistance ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    CHASE_AI_SEARCH_POINT_SOURCE.HIDING_SPOT ,
                    ref pathStartPosition ,
                    hidingSpotCandidate.HidingSpot);

                if ( wasAdded )
                {
                    Debug.Log(
                        $"[ChaseAISearch] 증거 기반 은신처 조사 지점 추가: " +
                        $"Position={hidingSpotCandidate.Position}, " +
                        $"EvidenceDistance={Vector3.Distance(centerPosition , hidingSpotCandidate.Position):F1}");
                }
            }

            for ( int attemptIndex = 0; attemptIndex < maximumAttemptCount; attemptIndex++ )
            {
                if ( SEARCH_POINTS.Count >= targetPointCount )
                {
                    break;
                }

                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * validSearchRadius;

                Vector3 randomPosition = centerPosition +
                    new Vector3(
                        randomOffset.x ,
                        0f ,
                        randomOffset.y);

                TryAddSearchPoint(
                    randomPosition ,
                    centerPosition ,
                    validSearchRadius ,
                    validMinimumPointDistance ,
                    validSampleRadius ,
                    areaMask ,
                    CHASE_AI_SEARCH_POINT_SOURCE.RANDOM ,
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

        public bool TryGetSearchPointSource(
            int pointIndex ,
            out CHASE_AI_SEARCH_POINT_SOURCE searchPointSource)
        {
            searchPointSource = default;

            if ( pointIndex < 0 || pointIndex >= SEARCH_POINT_SOURCES.Count )
            {
                return false;
            }

            searchPointSource = SEARCH_POINT_SOURCES[ pointIndex ];

            return true;
        }

        public bool TryGetCurrentHidingSpot(out AIHidingSpot hidingSpot)
        {
            hidingSpot = null;

            if ( !HasCurrentPoint || _currentPointIndex >= SEARCH_POINT_HIDING_SPOTS.Count )
            {
                return false;
            }

            hidingSpot = SEARCH_POINT_HIDING_SPOTS[ _currentPointIndex ];

            return hidingSpot != null;
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
            SEARCH_POINT_SOURCES.Clear();
            SEARCH_POINT_HIDING_SPOTS.Clear();
            ZONE_COVERAGE_CANDIDATES.Clear();
            _activeSearchZone = null;
            _currentPointIndex = 0;
            _zoneCoveragePointCount = 0;
            _hidingSpotPointCount = 0;
            _isZoneRestricted = false;
            HidingSpotCandidateName = "NONE";
            HidingSpotInspectionChance = 0f;
            HidingSpotInspectionRoll = -1f;
            WasHidingSpotSelected = false;
        }

        private AISearchPoint FindNearestHidingSpot(
            Vector3 evidencePosition ,
            float evidenceDistance)
        {
            if ( evidenceDistance <= 0f )
            {
                return null;
            }

            AISearchPoint nearestHidingSpot = null;
            float nearestSquaredDistance = evidenceDistance * evidenceDistance;

            for ( int zoneIndex = 0; zoneIndex < _zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = _zones[ zoneIndex ];

                if ( zone == null )
                {
                    continue;
                }

                IReadOnlyList<AISearchPoint> searchPoints = zone.SearchPoints;

                for ( int pointIndex = 0; pointIndex < searchPoints.Count; pointIndex++ )
                {
                    AISearchPoint searchPoint = searchPoints[ pointIndex ];

                    if ( searchPoint == null ||
                         searchPoint.PointType != AI_SEARCH_POINT_TYPE.HIDING_SPOT ||
                         searchPoint.HidingSpot == null ||
                         !zone.Contains(searchPoint.Position) )
                    {
                        continue;
                    }

                    Vector3 evidenceOffset = searchPoint.Position - evidencePosition;
                    evidenceOffset.y = 0f;

                    float squaredDistance = evidenceOffset.sqrMagnitude;

                    if ( squaredDistance > nearestSquaredDistance )
                    {
                        continue;
                    }

                    nearestHidingSpot = searchPoint;
                    nearestSquaredDistance = squaredDistance;
                }
            }

            return nearestHidingSpot;
        }

        private void AddZoneCoveragePoints(
            int requestedPointCount ,
            Vector3 centerPosition ,
            float searchRadius ,
            float minimumPointDistance ,
            float sampleRadius ,
            int areaMask ,
            ref Vector3 pathStartPosition)
        {
            if ( requestedPointCount <= 0 || _activeSearchZone == null )
            {
                return;
            }

            ZONE_COVERAGE_CANDIDATES.Clear();
            _activeSearchZone.CollectSearchPoints(AI_SEARCH_POINT_TYPE.COVERAGE , ZONE_COVERAGE_CANDIDATES);

            int addedPointCount = 0;

            while ( addedPointCount < requestedPointCount && ZONE_COVERAGE_CANDIDATES.Count > 0 )
            {
                int candidateIndex = UnityEngine.Random.Range(0 , ZONE_COVERAGE_CANDIDATES.Count);
                AISearchPoint candidatePoint = ZONE_COVERAGE_CANDIDATES[ candidateIndex ];

                ZONE_COVERAGE_CANDIDATES.RemoveAt(candidateIndex);

                if ( candidatePoint == null || !_activeSearchZone.Contains(candidatePoint.Position) )
                {
                    continue;
                }

                bool wasAdded = TryAddSearchPoint(
                    candidatePoint.Position ,
                    centerPosition ,
                    searchRadius ,
                    minimumPointDistance ,
                    sampleRadius ,
                    areaMask ,
                    CHASE_AI_SEARCH_POINT_SOURCE.ZONE_COVERAGE ,
                    ref pathStartPosition);

                if ( wasAdded )
                {
                    addedPointCount++;
                }
            }
        }

        private bool TryAddSearchPoint(
            Vector3 candidatePosition ,
            Vector3 centerPosition ,
            float searchRadius ,
            float minimumPointDistance ,
            float sampleRadius ,
            int areaMask ,
            CHASE_AI_SEARCH_POINT_SOURCE searchPointSource ,
            ref Vector3 pathStartPosition ,
            AIHidingSpot hidingSpot = null)
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

            if ( _isZoneRestricted && !_activeSearchZone.Contains(sampledPosition) )
            {
                return false;
            }

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
            SEARCH_POINT_SOURCES.Add(searchPointSource);
            SEARCH_POINT_HIDING_SPOTS.Add(hidingSpot);
            pathStartPosition = sampledPosition;

            if ( searchPointSource == CHASE_AI_SEARCH_POINT_SOURCE.ZONE_COVERAGE )
            {
                _zoneCoveragePointCount++;
            }
            else if ( searchPointSource == CHASE_AI_SEARCH_POINT_SOURCE.HIDING_SPOT )
            {
                _hidingSpotPointCount++;
            }

            return true;
        }

        private void ResolveActiveSearchZone(
            Vector3 centerPosition ,
            int requestedZoneId ,
            bool shouldRestrictToZone)
        {
            AIWorldZone requestedZone = FindZoneById(requestedZoneId);

            _activeSearchZone = requestedZone != null
                ? requestedZone
                : FindContainingZone(centerPosition);

            _isZoneRestricted = shouldRestrictToZone && requestedZone != null;

            if ( shouldRestrictToZone && requestedZone == null )
            {
                Debug.LogWarning(
                    $"[ChaseAISearch] 요청 Zone을 찾지 못해 반경 수색으로 대체합니다: ZoneId={requestedZoneId}");
            }
        }

        private AIWorldZone FindZoneById(int zoneId)
        {
            if ( zoneId == ChaseAISearchRequest.NO_ZONE_ID )
            {
                return null;
            }

            for ( int zoneIndex = 0; zoneIndex < _zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = _zones[ zoneIndex ];

                if ( zone != null && zone.ZoneId == zoneId )
                {
                    return zone;
                }
            }

            return null;
        }

        private AIWorldZone FindContainingZone(Vector3 worldPosition)
        {
            for ( int zoneIndex = 0; zoneIndex < _zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = _zones[ zoneIndex ];

                if ( zone != null && zone.Contains(worldPosition) )
                {
                    return zone;
                }
            }

            return null;
        }

        private bool IsInsideSearchRadius(
            Vector3 candidatePosition ,
            Vector3 centerPosition ,
            float searchRadius)
        {
            Vector3 offset = candidatePosition - centerPosition;
            offset.y = 0f;

            return offset.sqrMagnitude <= searchRadius * searchRadius;
        }

        private bool IsTooCloseToExistingPoint(
            Vector3 candidatePosition ,
            float minimumPointDistance)
        {
            float squaredMinimumDistance = minimumPointDistance * minimumPointDistance;

            for ( int pointIndex = 0; pointIndex < SEARCH_POINTS.Count; pointIndex++ )
            {
                Vector3 offset = SEARCH_POINTS[ pointIndex ] - candidatePosition;

                offset.y = 0f;

                if ( offset.sqrMagnitude < squaredMinimumDistance )
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCompletePath(
            Vector3 startPosition ,
            Vector3 destination ,
            int areaMask)
        {
            SEARCH_PATH.ClearCorners();

            bool hasPath = NavMesh.CalculatePath(
                startPosition ,
                destination ,
                areaMask ,
                SEARCH_PATH);

            return hasPath && SEARCH_PATH.status == NavMeshPathStatus.PathComplete;
        }

        private static int CalculateDirectionalPointCount(
            int targetPointCount ,
            float directionalPointRatio ,
            bool hasPreferredDirection)
        {
            if ( !hasPreferredDirection )
            {
                return 0;
            }

            float validDirectionalPointRatio = Mathf.Clamp01(directionalPointRatio);

            if ( validDirectionalPointRatio <= 0f )
            {
                return 0;
            }

            return Mathf.Clamp(
                Mathf.Max(1 , Mathf.RoundToInt(targetPointCount * validDirectionalPointRatio)) ,
                0 ,
                targetPointCount);
        }
    }
}
