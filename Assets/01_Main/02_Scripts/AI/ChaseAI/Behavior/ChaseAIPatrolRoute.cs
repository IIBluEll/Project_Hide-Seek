using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIPatrolRoute
    {
        private readonly IReadOnlyList<Transform> FALLBACK_PATROL_POINTS;
        private readonly List<AISearchPoint> ZONE_PATROL_POINTS = new();

        private IReadOnlyList<AIWorldZone> _zones = Array.Empty<AIWorldZone>();
        private int _currentPointIndex;

        public AIWorldZone CurrentZone { get; private set; }
        public bool IsUsingZoneRoute => CurrentZone != null && ZONE_PATROL_POINTS.Count > 0;
        public int PointCount => IsUsingZoneRoute
            ? ZONE_PATROL_POINTS.Count
            : FALLBACK_PATROL_POINTS?.Count ?? 0;

        public ChaseAIPatrolRoute(IReadOnlyList<Transform> fallbackPatrolPoints)
        {
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
                return;
            }

            CurrentZone.CollectSearchPoints(
                AI_SEARCH_POINT_TYPE.COVERAGE ,
                ZONE_PATROL_POINTS);

            ZONE_PATROL_POINTS.RemoveAll(
                searchPoint => searchPoint == null || !searchPoint.isActiveAndEnabled);

            if ( ZONE_PATROL_POINTS.Count == 0 )
            {
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

        public void Advance()
        {
            if ( PointCount == 0 )
            {
                return;
            }

            _currentPointIndex = ( _currentPointIndex + 1 ) % PointCount;
        }

        public void Clear()
        {
            CurrentZone = null;
            ZONE_PATROL_POINTS.Clear();
            _currentPointIndex = 0;
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
    }
}
