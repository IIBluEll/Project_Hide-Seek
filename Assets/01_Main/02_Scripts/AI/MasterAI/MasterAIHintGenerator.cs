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

        public bool TryCreateHint(AIWorldZone targetZone , float currentTime , out MasterAIHint hint)
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

            hint = new MasterAIHint(
                targetZone.ZoneId ,
                searchAnchorPosition ,
                MASTER_AI_CONFIG.HintRadius ,
                MASTER_AI_CONFIG.HintUrgency ,
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
