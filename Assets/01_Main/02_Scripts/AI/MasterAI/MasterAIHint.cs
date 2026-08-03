using UnityEngine;

namespace HideSeek.AI
{
    public readonly struct MasterAIHint
    {
        public int TargetZoneId { get; }
        public Vector3 SearchAnchorPosition { get; }
        public float SearchRadius { get; }
        public float Urgency { get; }
        public float ExpireTime { get; }

        public MasterAIHint(int targetZoneId , Vector3 searchAnchorPosition , float searchRadius , float urgency , float expireTime)
        {
            TargetZoneId = targetZoneId;
            SearchAnchorPosition = searchAnchorPosition;
            SearchRadius = Mathf.Max(0f , searchRadius);
            Urgency = Mathf.Clamp01(urgency);
            ExpireTime = expireTime;
        }

        public bool IsValid(float currentTime)
        {
            return currentTime <= ExpireTime;
        }
    }
}