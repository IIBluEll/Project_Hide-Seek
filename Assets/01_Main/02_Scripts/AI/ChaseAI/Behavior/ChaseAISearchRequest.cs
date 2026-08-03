using UnityEngine;

namespace HideSeek.AI
{
    public readonly struct ChaseAISearchRequest
    {
        public const int NO_ZONE_ID = -1;

        public Vector3 CenterPosition { get; }
        public Vector3 PreferredDirection { get; }
        public float SearchRadius { get; }
        public float SearchDuration { get; }
        public bool ShouldMoveToCenter { get; }
        public float AngerInfluence { get; }
        public string Context { get; }
        public bool CanInspectHidingSpot { get; }
        public float HidingSpotInspectionChance { get; }
        public int SearchZoneId { get; }
        public bool ShouldRestrictToZone { get; }

        public ChaseAISearchRequest(
            Vector3 centerPosition ,
            Vector3 preferredDirection ,
            float searchRadius ,
            float searchDuration ,
            bool shouldMoveToCenter ,
            float angerInfluence ,
            string context ,
            bool canInspectHidingSpot ,
            float hidingSpotInspectionChance ,
            int searchZoneId = NO_ZONE_ID ,
            bool shouldRestrictToZone = false)
        {
            CenterPosition = centerPosition;
            PreferredDirection = preferredDirection;
            SearchRadius = searchRadius;
            SearchDuration = searchDuration;
            ShouldMoveToCenter = shouldMoveToCenter;
            AngerInfluence = angerInfluence;
            Context = context;
            CanInspectHidingSpot = canInspectHidingSpot;
            HidingSpotInspectionChance = Mathf.Clamp01(hidingSpotInspectionChance);
            SearchZoneId = searchZoneId;
            ShouldRestrictToZone = shouldRestrictToZone;
        }
    }
}
