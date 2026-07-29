using System;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    public sealed class MasterAIHintGenerator
    {
        private readonly MasterAIConfig MASTER_AI_CONFIG;

        public MasterAIHintGenerator(MasterAIConfig masterAIConfig)
        {
            MASTER_AI_CONFIG = masterAIConfig != null ? masterAIConfig : throw new ArgumentNullException(nameof(masterAIConfig));
        }

        public bool TryCreateHint(AIWorldZone targetZone , float stressRatio , float currentTime , out MasterAIHint hint)
        {
            hint = default;

            if ( targetZone == null )
            {
                return false;
            }

            if ( !TryFindNavMeshPosition(targetZone , out Vector3 searchAnchorPosition) )
            {
                return false;
            }

            float normalizedStress = Mathf.Clamp01(stressRatio);
            float searchRadius = Mathf.Lerp(MASTER_AI_CONFIG.MaximumHintRadius , MASTER_AI_CONFIG.MinimumHintRadius , normalizedStress);
            float urgency = Mathf.Lerp(MASTER_AI_CONFIG.MinimumHintUrgency , MASTER_AI_CONFIG.MaximumHintUrgency , normalizedStress);

            hint = new MasterAIHint(
                targetZone.ZoneId ,
                searchAnchorPosition ,
                searchRadius ,
                urgency ,
                currentTime + MASTER_AI_CONFIG.HintDuration);

            return true;
        }

        private bool TryFindNavMeshPosition(AIWorldZone targetZone , out Vector3 navMeshPosition)
        {
            for ( int attemptIndex = 0; attemptIndex < MASTER_AI_CONFIG.HintPositionAttemptCount; attemptIndex++ )
            {
                Vector3 candidatePosition = targetZone.GetRandomWorldPosition();

                bool wasSampled = NavMesh.SamplePosition(
            candidatePosition ,
            out NavMeshHit navMeshHit ,
            MASTER_AI_CONFIG.HintNavMeshSampleRadius ,
            NavMesh.AllAreas);

                if ( !wasSampled || !targetZone.Contains(navMeshHit.position) )
                {
                    continue;
                }

                navMeshPosition = navMeshHit.position;

                return true;
            }

            navMeshPosition = Vector3.zero;

            return false;
        }
    }
}