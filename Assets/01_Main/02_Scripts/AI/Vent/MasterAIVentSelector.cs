using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    public sealed class MasterAIVentSelector
    {
        private readonly List<AIVentPoint> VENTS = new();
        private readonly NavMeshPath RETREAT_PATH = new();

        public MasterAIVentSelector(IReadOnlyList<AIVentPoint> vents)
        {
            if ( vents == null )
            {
                throw new ArgumentNullException(nameof(vents));
            }

            for ( int ventIndex = 0; ventIndex < vents.Count; ventIndex++ )
            {
                AIVentPoint vent = vents[ ventIndex ];

                if ( vent != null && !VENTS.Contains(vent) )
                {
                    VENTS.Add(vent);
                }
            }

            if ( VENTS.Count == 0 )
            {
                throw new ArgumentException("사용 가능한 Vent가 없습니다." , nameof(vents));
            }
        }

        public bool TrySelectActivationVent(
            AIWorldZone playerZone ,
            Vector3 playerPosition ,
            float navMeshSampleRadius ,
            int areaMask ,
            out AIVentPoint selectedVent ,
            out Vector3 activationPosition)
        {
            selectedVent = null;
            activationPosition = Vector3.zero;

            if ( playerZone == null )
            {
                return false;
            }

            float farthestSquaredDistance = float.NegativeInfinity;

            for ( int ventIndex = 0; ventIndex < VENTS.Count; ventIndex++ )
            {
                AIVentPoint vent = VENTS[ ventIndex ];

                if ( !TryGetAvailableVentPosition(vent , playerZone , navMeshSampleRadius , areaMask , out Vector3 ventPosition) )
                {
                    continue;
                }

                float squaredDistance = (ventPosition - playerPosition).sqrMagnitude;

                if ( squaredDistance <= farthestSquaredDistance )
                {
                    continue;
                }

                selectedVent = vent;
                activationPosition = ventPosition;
                farthestSquaredDistance = squaredDistance;
            }

            return selectedVent != null;
        }

        public bool TrySelectRetreatVent(
            AIWorldZone playerZone ,
            Vector3 chasePosition ,
            float navMeshSampleRadius ,
            int areaMask ,
            out AIVentPoint selectedVent ,
            out Vector3 retreatPosition)
        {
            selectedVent = null;
            retreatPosition = Vector3.zero;

            if ( playerZone == null || !TryGetNavMeshPosition(chasePosition , navMeshSampleRadius , areaMask , out Vector3 chaseNavMeshPosition) )
            {
                return false;
            }

            float shortestPathDistance = float.PositiveInfinity;

            for ( int ventIndex = 0; ventIndex < VENTS.Count; ventIndex++ )
            {
                AIVentPoint vent = VENTS[ ventIndex ];

                if ( !TryGetAvailableVentPosition(vent , playerZone , navMeshSampleRadius , areaMask , out Vector3 ventPosition) )
                {
                    continue;
                }

                if ( !TryCalculatePathDistance(chaseNavMeshPosition , ventPosition , areaMask , out float pathDistance) || pathDistance >= shortestPathDistance )
                {
                    continue;
                }

                selectedVent = vent;
                retreatPosition = ventPosition;
                shortestPathDistance = pathDistance;
            }

            return selectedVent != null;
        }

        private bool TryGetAvailableVentPosition(
            AIVentPoint vent ,
            AIWorldZone playerZone ,
            float navMeshSampleRadius ,
            int areaMask ,
            out Vector3 ventPosition)
        {
            ventPosition = Vector3.zero;

            if ( vent == null || !vent.IsAvailable || vent.Zone == null || IsPlayerZoneOrAdjacent(vent.Zone , playerZone) )
            {
                return false;
            }

            return TryGetNavMeshPosition(vent.Position , navMeshSampleRadius , areaMask , out ventPosition);
        }

        private bool IsPlayerZoneOrAdjacent(AIWorldZone ventZone , AIWorldZone playerZone)
        {
            if ( ventZone == playerZone )
            {
                return true;
            }

            return playerZone.IsAdjacentTo(ventZone) || ventZone.IsAdjacentTo(playerZone);
        }

        private bool TryGetNavMeshPosition(
            Vector3 position ,
            float navMeshSampleRadius ,
            int areaMask ,
            out Vector3 navMeshPosition)
        {
            bool wasSampled = NavMesh.SamplePosition(
                position ,
                out NavMeshHit navMeshHit ,
                Mathf.Max(0.1f , navMeshSampleRadius) ,
                areaMask);

            navMeshPosition = wasSampled ? navMeshHit.position : Vector3.zero;

            return wasSampled;
        }

        private bool TryCalculatePathDistance(
            Vector3 startPosition ,
            Vector3 destination ,
            int areaMask ,
            out float pathDistance)
        {
            RETREAT_PATH.ClearCorners();

            bool hasPath = NavMesh.CalculatePath(
                startPosition ,
                destination ,
                areaMask ,
                RETREAT_PATH);

            if ( !hasPath || RETREAT_PATH.status != NavMeshPathStatus.PathComplete )
            {
                pathDistance = 0f;

                return false;
            }

            pathDistance = 0f;
            Vector3[] corners = RETREAT_PATH.corners;

            for ( int cornerIndex = 0; cornerIndex < corners.Length - 1; cornerIndex++ )
            {
                pathDistance += Vector3.Distance(corners[ cornerIndex ] , corners[ cornerIndex + 1 ]);
            }

            return true;
        }
    }
}
