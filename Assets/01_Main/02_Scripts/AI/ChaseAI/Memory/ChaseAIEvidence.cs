using UnityEngine;

namespace HideSeek.AI
{
    public enum CHASE_AI_EVIDENCE_TYPE
    {
        NONE,
        VISUAL,
        AUDIO,
        DIRECTOR_HINT
    }

    public readonly struct ChaseAIEvidence
    {
        public CHASE_AI_EVIDENCE_TYPE EvidenceType { get; }
        public Vector3 Position { get; }
        public float OccurredTime { get; }
        public float ExpireTime { get; }
        public float Strength { get; }

        public ChaseAIEvidence(
            CHASE_AI_EVIDENCE_TYPE evidenceType ,
            Vector3 position ,
            float occurredTime ,
            float duration ,
            float strength)
        {
            EvidenceType = evidenceType;
            Position = position;
            OccurredTime = occurredTime;
            ExpireTime = occurredTime + duration;
            Strength = strength;
        }

        public bool IsValid(float currentTime)
        {
            return EvidenceType != CHASE_AI_EVIDENCE_TYPE.NONE && currentTime <= ExpireTime;
        }

        public float GetRemainingTime(float currentTime)
        {
            if ( !IsValid(currentTime) )
            {
                return 0f;
            }

            return Mathf.Max(0f , ExpireTime - currentTime);
        }

        public static ChaseAIEvidence Empty =>
            new ChaseAIEvidence(
                CHASE_AI_EVIDENCE_TYPE.NONE ,
                Vector3.zero ,
                0f ,
                0f ,
                0f);
    }
}