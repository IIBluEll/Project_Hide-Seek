using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIPatrolRoute
    {
        private readonly IReadOnlyList<Transform> FALLBACK_PATROL_POINTS;
        private readonly List<AISearchPoint> ZONE_PATROL_POINTS = new();
        private readonly List<AISearchPoint> ZONE_POINT_BUFFER = new();
        private readonly List<int> RECENT_POINT_INDICES = new();
        private readonly List<AIWorldZone> ADJACENT_ZONE_CANDIDATES = new();
        private readonly List<AIWorldZone> OTHER_ZONE_CANDIDATES = new();
        private readonly List<AIWorldZone> RECENT_ZONES = new();
        private readonly ChaseAIConfig CHASE_AI_CONFIG;

        private IReadOnlyList<AIWorldZone> _zones = Array.Empty<AIWorldZone>();
        private int _currentPointIndex;
        private int _visitedPointCountInCurrentZone;
        private int _targetPointVisitCountInCurrentZone;

        public AIWorldZone CurrentZone { get; private set; }
        public bool IsUsingZoneRoute => CurrentZone != null && ZONE_PATROL_POINTS.Count > 0;
        public int PointCount => IsUsingZoneRoute
            ? ZONE_PATROL_POINTS.Count
            : FALLBACK_PATROL_POINTS?.Count ?? 0;

        public ChaseAIPatrolRoute(
            ChaseAIConfig chaseAIConfig ,
            IReadOnlyList<Transform> fallbackPatrolPoints)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));
            FALLBACK_PATROL_POINTS = fallbackPatrolPoints;
        }

        public void ConfigureZones(IReadOnlyList<AIWorldZone> zones)
        {
            _zones = zones ?? Array.Empty<AIWorldZone>();

            Clear();
        }

        public void Refresh(Vector3 currentPosition)
        {
            AIWorldZone previousZone = CurrentZone;

            ResetActiveRoute();

            AIWorldZone containingZone = FindContainingZone(currentPosition);

            if ( containingZone == null )
            {
                _currentPointIndex = FindNearestFallbackPointIndex(currentPosition);

                return;
            }

            if ( previousZone != null && previousZone != containingZone )
            {
                RememberZone(previousZone);
            }

            if ( !TrySetCurrentZone(containingZone , currentPosition) )
            {
                _currentPointIndex = FindNearestFallbackPointIndex(currentPosition);
            }
        }

        public bool TryGetCurrentPoint(
            out Vector3 patrolPosition ,
            out string context)
        {
            if ( IsUsingZoneRoute )
            {
                AISearchPoint patrolPoint = ZONE_PATROL_POINTS[ _currentPointIndex ];

                patrolPosition = patrolPoint.Position;
                context = $"Zone patrol point {CurrentZone.DisplayName}/{_currentPointIndex}";

                return true;
            }

            if ( FALLBACK_PATROL_POINTS == null || FALLBACK_PATROL_POINTS.Count == 0 )
            {
                patrolPosition = default;
                context = string.Empty;

                return false;
            }

            Transform fallbackPoint = FALLBACK_PATROL_POINTS[ _currentPointIndex ];

            if ( fallbackPoint == null )
            {
                patrolPosition = default;
                context = string.Empty;

                return false;
            }

            patrolPosition = fallbackPoint.position;
            context = $"Fallback patrol point {_currentPointIndex}";

            return true;
        }

        public void Advance(Vector3 currentPosition , bool didReachPoint)
        {
            if ( PointCount == 0 )
            {
                return;
            }

            RememberCurrentPointIndex();

            if ( IsUsingZoneRoute )
            {
                if ( didReachPoint )
                {
                    _visitedPointCountInCurrentZone++;
                }

                bool shouldSelectNewZone = !didReachPoint ||
                    _visitedPointCountInCurrentZone >= _targetPointVisitCountInCurrentZone;

                if ( shouldSelectNewZone && TryMoveToNextZone(currentPosition) )
                {
                    return;
                }
            }

            _currentPointIndex = SelectNextPointIndex(currentPosition);
        }

        public void Clear()
        {
            ResetActiveRoute();
            RECENT_ZONES.Clear();
        }

        private void ResetActiveRoute()
        {
            CurrentZone = null;
            ZONE_PATROL_POINTS.Clear();
            ZONE_POINT_BUFFER.Clear();
            RECENT_POINT_INDICES.Clear();
            ADJACENT_ZONE_CANDIDATES.Clear();
            OTHER_ZONE_CANDIDATES.Clear();
            _currentPointIndex = 0;
            _visitedPointCountInCurrentZone = 0;
            _targetPointVisitCountInCurrentZone = 0;
        }

        private bool TryMoveToNextZone(Vector3 currentPosition)
        {
            if ( !TrySelectNextZone(out AIWorldZone nextZone , out bool isAdjacentZone) )
            {
                return false;
            }

            AIWorldZone previousZone = CurrentZone;
            int completedPointVisitCount = _visitedPointCountInCurrentZone;

            RememberZone(previousZone);

            if ( !TrySetCurrentZone(nextZone , currentPosition) )
            {
                Refresh(currentPosition);

                return false;
            }

            string relation = isAdjacentZone ? "Adjacent" : "Other";

            Debug.Log(
                $"[ChaseAIPatrolRoute] 순찰 Zone 변경: " +
                $"{previousZone.DisplayName} -> {nextZone.DisplayName}, " +
                $"Relation={relation}, " +
                $"CompletedPoints={completedPointVisitCount}, " +
                $"NextTargetPoints={_targetPointVisitCountInCurrentZone}");

            return true;
        }

        private bool TrySelectNextZone(
            out AIWorldZone nextZone ,
            out bool isAdjacentZone)
        {
            BuildZoneCandidates(true);

            if ( ADJACENT_ZONE_CANDIDATES.Count == 0 && OTHER_ZONE_CANDIDATES.Count == 0 )
            {
                BuildZoneCandidates(false);
            }

            bool hasAdjacentZone = ADJACENT_ZONE_CANDIDATES.Count > 0;
            bool hasOtherZone = OTHER_ZONE_CANDIDATES.Count > 0;

            if ( !hasAdjacentZone && !hasOtherZone )
            {
                nextZone = null;
                isAdjacentZone = false;

                return false;
            }

            bool shouldSelectAdjacentZone = UnityEngine.Random.value <
                CHASE_AI_CONFIG.PatrolAdjacentZoneSelectionChance;

            isAdjacentZone = shouldSelectAdjacentZone
                ? hasAdjacentZone
                : !hasOtherZone;

            List<AIWorldZone> candidates = isAdjacentZone
                ? ADJACENT_ZONE_CANDIDATES
                : OTHER_ZONE_CANDIDATES;

            nextZone = candidates[ UnityEngine.Random.Range(0 , candidates.Count) ];

            return true;
        }

        private void BuildZoneCandidates(bool shouldAvoidRecentZones)
        {
            ADJACENT_ZONE_CANDIDATES.Clear();
            OTHER_ZONE_CANDIDATES.Clear();

            for ( int zoneIndex = 0; zoneIndex < _zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = _zones[ zoneIndex ];

                if ( zone == null ||
                     zone == CurrentZone ||
                     !zone.isActiveAndEnabled ||
                     (shouldAvoidRecentZones && RECENT_ZONES.Contains(zone)) ||
                     !HasActiveCoveragePoint(zone) )
                {
                    continue;
                }

                if ( CurrentZone.IsAdjacentTo(zone) )
                {
                    ADJACENT_ZONE_CANDIDATES.Add(zone);
                }
                else
                {
                    OTHER_ZONE_CANDIDATES.Add(zone);
                }
            }
        }

        private bool HasActiveCoveragePoint(AIWorldZone zone)
        {
            ZONE_POINT_BUFFER.Clear();
            zone.CollectSearchPoints(AI_SEARCH_POINT_TYPE.COVERAGE , ZONE_POINT_BUFFER);

            for ( int pointIndex = 0; pointIndex < ZONE_POINT_BUFFER.Count; pointIndex++ )
            {
                AISearchPoint searchPoint = ZONE_POINT_BUFFER[ pointIndex ];

                if ( searchPoint != null && searchPoint.isActiveAndEnabled )
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySetCurrentZone(AIWorldZone zone , Vector3 currentPosition)
        {
            CurrentZone = zone;
            ZONE_PATROL_POINTS.Clear();
            RECENT_POINT_INDICES.Clear();

            CurrentZone.CollectSearchPoints(
                AI_SEARCH_POINT_TYPE.COVERAGE ,
                ZONE_PATROL_POINTS);

            ZONE_PATROL_POINTS.RemoveAll(
                searchPoint => searchPoint == null || !searchPoint.isActiveAndEnabled);

            if ( ZONE_PATROL_POINTS.Count == 0 )
            {
                CurrentZone = null;

                return false;
            }

            _currentPointIndex = FindNearestPointIndex(currentPosition);
            _visitedPointCountInCurrentZone = 0;

            int maximumPointVisitCount = Mathf.Min(
                CHASE_AI_CONFIG.MaximumPatrolPointVisitCountPerZone ,
                ZONE_PATROL_POINTS.Count);
            int minimumPointVisitCount = Mathf.Min(
                CHASE_AI_CONFIG.MinimumPatrolPointVisitCountPerZone ,
                maximumPointVisitCount);

            _targetPointVisitCountInCurrentZone = UnityEngine.Random.Range(
                minimumPointVisitCount ,
                maximumPointVisitCount + 1);

            return true;
        }

        private int SelectNextPointIndex(Vector3 currentPosition)
        {
            if ( PointCount <= 1 )
            {
                return 0;
            }

            float totalWeight = 0f;

            for ( int pointIndex = 0; pointIndex < PointCount; pointIndex++ )
            {
                totalWeight += GetSelectionWeight(pointIndex , currentPosition);
            }

            if ( totalWeight <= Mathf.Epsilon )
            {
                return FindNextValidPointIndex();
            }

            float selectionValue = UnityEngine.Random.value * totalWeight;

            for ( int pointIndex = 0; pointIndex < PointCount; pointIndex++ )
            {
                float selectionWeight = GetSelectionWeight(pointIndex , currentPosition);

                if ( selectionWeight <= 0f )
                {
                    continue;
                }

                selectionValue -= selectionWeight;

                if ( selectionValue <= 0f )
                {
                    return pointIndex;
                }
            }

            return FindNextValidPointIndex();
        }

        private float GetSelectionWeight(int pointIndex , Vector3 currentPosition)
        {
            if ( pointIndex == _currentPointIndex || !TryGetPointPosition(pointIndex , out Vector3 pointPosition) )
            {
                return 0f;
            }

            Vector3 offset = pointPosition - currentPosition;
            offset.y = 0f;

            float distanceWeight = 1f / (1f + offset.magnitude);
            float historyWeight = RECENT_POINT_INDICES.Contains(pointIndex)
                ? CHASE_AI_CONFIG.PatrolRecentPointWeightMultiplier
                : 1f;

            return distanceWeight * historyWeight;
        }

        private bool TryGetPointPosition(int pointIndex , out Vector3 pointPosition)
        {
            if ( pointIndex < 0 || pointIndex >= PointCount )
            {
                pointPosition = default;

                return false;
            }

            if ( IsUsingZoneRoute )
            {
                AISearchPoint searchPoint = ZONE_PATROL_POINTS[ pointIndex ];

                pointPosition = searchPoint != null ? searchPoint.Position : default;

                return searchPoint != null;
            }

            Transform fallbackPoint = FALLBACK_PATROL_POINTS[ pointIndex ];

            pointPosition = fallbackPoint != null ? fallbackPoint.position : default;

            return fallbackPoint != null;
        }

        private int FindNextValidPointIndex()
        {
            for ( int offset = 1; offset <= PointCount; offset++ )
            {
                int pointIndex = ( _currentPointIndex + offset ) % PointCount;

                if ( TryGetPointPosition(pointIndex , out _) )
                {
                    return pointIndex;
                }
            }

            return _currentPointIndex;
        }

        private void RememberCurrentPointIndex()
        {
            int historyCapacity = CHASE_AI_CONFIG.PatrolRecentPointHistoryCapacity;

            if ( historyCapacity <= 0 )
            {
                RECENT_POINT_INDICES.Clear();

                return;
            }

            RECENT_POINT_INDICES.Remove(_currentPointIndex);
            RECENT_POINT_INDICES.Add(_currentPointIndex);

            while ( RECENT_POINT_INDICES.Count > historyCapacity )
            {
                RECENT_POINT_INDICES.RemoveAt(0);
            }
        }

        private void RememberZone(AIWorldZone zone)
        {
            int historyCapacity = CHASE_AI_CONFIG.PatrolRecentZoneHistoryCapacity;

            if ( historyCapacity <= 0 || zone == null )
            {
                if ( historyCapacity <= 0 )
                {
                    RECENT_ZONES.Clear();
                }

                return;
            }

            RECENT_ZONES.Remove(zone);
            RECENT_ZONES.Add(zone);

            while ( RECENT_ZONES.Count > historyCapacity )
            {
                RECENT_ZONES.RemoveAt(0);
            }
        }

        private AIWorldZone FindContainingZone(Vector3 currentPosition)
        {
            for ( int zoneIndex = 0; zoneIndex < _zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = _zones[ zoneIndex ];

                if ( zone != null && zone.Contains(currentPosition) )
                {
                    return zone;
                }
            }

            return null;
        }

        private int FindNearestPointIndex(Vector3 currentPosition)
        {
            int nearestPointIndex = 0;
            float nearestSqrDistance = float.MaxValue;

            for ( int pointIndex = 0; pointIndex < ZONE_PATROL_POINTS.Count; pointIndex++ )
            {
                float sqrDistance = (
                    ZONE_PATROL_POINTS[ pointIndex ].Position - currentPosition
                ).sqrMagnitude;

                if ( sqrDistance >= nearestSqrDistance )
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                nearestPointIndex = pointIndex;
            }

            return nearestPointIndex;
        }

        private int FindNearestFallbackPointIndex(Vector3 currentPosition)
        {
            if ( FALLBACK_PATROL_POINTS == null || FALLBACK_PATROL_POINTS.Count == 0 )
            {
                return 0;
            }

            int nearestPointIndex = 0;
            float nearestSqrDistance = float.MaxValue;

            for ( int pointIndex = 0; pointIndex < FALLBACK_PATROL_POINTS.Count; pointIndex++ )
            {
                Transform patrolPoint = FALLBACK_PATROL_POINTS[ pointIndex ];

                if ( patrolPoint == null )
                {
                    continue;
                }

                float sqrDistance = ( patrolPoint.position - currentPosition ).sqrMagnitude;

                if ( sqrDistance >= nearestSqrDistance )
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                nearestPointIndex = pointIndex;
            }

            return nearestPointIndex;
        }
    }
}
