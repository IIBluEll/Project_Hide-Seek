using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIZoneSelector
    {
        private readonly List<AIWorldZone> ZONES = new();

        public IReadOnlyList<AIWorldZone> Zones => ZONES;

        public MasterAIZoneSelector(IReadOnlyList<AIWorldZone> zones)
        {
            if ( zones == null )
            {
                throw new ArgumentNullException(nameof(zones));
            }

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
    }
}