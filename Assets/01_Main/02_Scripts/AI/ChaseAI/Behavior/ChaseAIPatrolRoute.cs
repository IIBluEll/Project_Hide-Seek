using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIPatrolRoute
    {
        private readonly IReadOnlyList<Transform> FALLBACK_PATROL_POINTS;
        private readonly List<AISearchPoint> ZONE_PATROL_POINTS = new();
        private readonly List<int> RECENT_POINT_INDICES = new();
        private readonly ChaseAIConfig CHASE_AI_CONFIG;

        private IReadOnlyList<AIWorldZone> _zones = Array.Empty<AIWorldZone>();
        private int _currentPointIndex;

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
            Clear();

            CurrentZone = FindContainingZone(currentPosition);

            if ( CurrentZone == null )
            {
                _currentPointIndex = FindNearestFallbackPointIndex(currentPosition);

                return;
            }

            CurrentZone.CollectSearchPoints(
                AI_SEARCH_POINT_TYPE.COVERAGE ,
                ZONE_PATROL_POINTS);

            ZONE_PATROL_POINTS.RemoveAll(
                searchPoint => searchPoint == null || !searchPoint.isActiveAndEnabled);

            if ( ZONE_PATROL_POINTS.Count == 0 )
            {
                _currentPointIndex = FindNearestFallbackPointIndex(currentPosition);

                return;
            }

            _currentPointIndex = FindNearestPointIndex(currentPosition);
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

        public void Advance(Vector3 currentPosition)
        {
            if ( PointCount == 0 )
            {
                return;
            }

            RememberCurrentPointIndex();
            _currentPointIndex = SelectNextPointIndex(currentPosition);
        }

        public void Clear()
        {
            CurrentZone = null;
            ZONE_PATROL_POINTS.Clear();
            RECENT_POINT_INDICES.Clear();
            _currentPointIndex = 0;
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
