using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIZoneSelector
    {
        private readonly MasterAIConfig MASTER_AI_CONFIG;
        private readonly List<AIWorldZone> ZONES = new();
        private readonly List<AIWorldZone> ADJACENT_CANDIDATES = new();
        private readonly List<AIWorldZone> OTHER_CANDIDATES = new();

        public IReadOnlyList<AIWorldZone> Zones => ZONES;

        public MasterAIZoneSelector(IReadOnlyList<AIWorldZone> zones , MasterAIConfig masterAIConfig)
        {
            if ( zones == null )
            {
                throw new ArgumentNullException(nameof(zones));
            }

            MASTER_AI_CONFIG = masterAIConfig != null ? masterAIConfig : throw new ArgumentNullException(nameof(masterAIConfig));

            HashSet<int> zoneIds = new();

            for ( int zoneIndex = 0; zoneIndex < zones.Count; zoneIndex++ )
            {
                AIWorldZone zone = zones[zoneIndex];

                if ( zone == null )
                {
                    continue;
                }

                if ( !zoneIds.Add(zone.ZoneId) )
                {
                    throw new ArgumentException($"중복된 Zone ID가 있습니다: {zone.ZoneId}" , nameof(zones));
                }

                ZONES.Add(zone);
            }

            if ( ZONES.Count == 0 )
            {
                throw new ArgumentException("사용 가능한 Zone이 없습니다." , nameof(zones));
            }
        }

        public bool TryGetContainingZone(Vector3 worldPosition , out AIWorldZone containingZone)
        {
            for ( int zoneIndex = 0; zoneIndex < ZONES.Count; zoneIndex++ )
            {
                AIWorldZone zone = ZONES[zoneIndex];

                if ( zone.Contains(worldPosition) )
                {
                    containingZone = zone;

                    return true;
                }
            }

            containingZone = null;

            return false;
        }

        public bool TryGetZoneById(int zoneId , out AIWorldZone foundZone)
        {
            for ( int zoneIndex = 0; zoneIndex < ZONES.Count; zoneIndex++ )
            {
                AIWorldZone zone = ZONES[zoneIndex];

                if ( zone.ZoneId == zoneId )
                {
                    foundZone = zone;

                    return true;
                }
            }

            foundZone = null;

            return false;
        }

        public bool TrySelectTargetZone(AIWorldZone playerZone , out AIWorldZone targetZone , out MASTER_AI_ZONE_RELATION relation)
        {
            targetZone = null;
            relation = MASTER_AI_ZONE_RELATION.PLAYER;

            if ( playerZone == null || !ZONES.Contains(playerZone) )
            {
                return false;
            }

            BuildCandidates(playerZone);

            float playerZoneWeight = MASTER_AI_CONFIG.PlayerZoneWeight;

            float adjacentZoneWeight = ADJACENT_CANDIDATES.Count > 0 ? MASTER_AI_CONFIG.AdjacentZoneWeight : 0f;

            float otherZoneWeight = OTHER_CANDIDATES.Count > 0 ? MASTER_AI_CONFIG.OtherZoneWeight : 0f;

            float totalWeight = playerZoneWeight + adjacentZoneWeight + otherZoneWeight;

            if ( totalWeight <= 0f )
            {
                targetZone = playerZone;

                return true;
            }

            float selectionValue = UnityEngine.Random.value * totalWeight;

            if ( selectionValue < playerZoneWeight )
            {
                targetZone = playerZone;
                relation = MASTER_AI_ZONE_RELATION.PLAYER;

                return true;
            }

            selectionValue -= playerZoneWeight;

            if ( selectionValue < adjacentZoneWeight && ADJACENT_CANDIDATES.Count > 0 )
            {
                targetZone = ADJACENT_CANDIDATES[ UnityEngine.Random.Range(0 , ADJACENT_CANDIDATES.Count) ];
                relation = MASTER_AI_ZONE_RELATION.ADJACENT;

                return true;
            }

            if ( OTHER_CANDIDATES.Count > 0 )
            {
                targetZone = OTHER_CANDIDATES[ UnityEngine.Random.Range(0 , OTHER_CANDIDATES.Count) ];
                relation = MASTER_AI_ZONE_RELATION.OTHER;

                return true;
            }

            if ( ADJACENT_CANDIDATES.Count > 0 )
            {
                targetZone = ADJACENT_CANDIDATES[ UnityEngine.Random.Range(0 , ADJACENT_CANDIDATES.Count) ];
                relation = MASTER_AI_ZONE_RELATION.ADJACENT;

                return true;
            }

            targetZone = playerZone;
            relation = MASTER_AI_ZONE_RELATION.PLAYER;

            return true;
        }

        private void BuildCandidates(AIWorldZone playerZone)
        {
            ADJACENT_CANDIDATES.Clear();
            OTHER_CANDIDATES.Clear();

            for ( int zoneIndex = 0; zoneIndex < ZONES.Count; zoneIndex++ )
            {
                AIWorldZone zone = ZONES[zoneIndex];

                if ( zone == playerZone )
                {
                    continue;
                }

                if ( playerZone.IsAdjacentTo(zone) )
                {
                    ADJACENT_CANDIDATES.Add(zone);
                }
                else
                {
                    OTHER_CANDIDATES.Add(zone);
                }
            }
        }
    }
}
