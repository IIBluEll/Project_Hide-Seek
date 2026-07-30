using UnityEngine;

namespace HideSeek.AI
{
    public readonly struct ChaseAISearchRequest
    {
        public Vector3 CenterPosition { get; }
        public Vector3 PreferredDirection { get; }
        public float SearchRadius { get; }
        public float SearchDuration { get; }
        public bool ShouldMoveToCenter { get; }
        public float AngerInfluence { get; }
        public string Context { get; }

        public ChaseAISearchRequest(
            Vector3 centerPosition ,
            Vector3 preferredDirection ,
            float searchRadius ,
            float searchDuration ,
            bool shouldMoveToCenter ,
            float angerInfluence ,
            string context)
        {
            CenterPosition = centerPosition;
            PreferredDirection = preferredDirection;
            SearchRadius = searchRadius;
            SearchDuration = searchDuration;
            ShouldMoveToCenter = shouldMoveToCenter;
            AngerInfluence = angerInfluence;
            Context = context;
        }
    }
}
