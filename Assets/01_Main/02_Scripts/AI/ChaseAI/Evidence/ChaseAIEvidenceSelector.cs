using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIEvidenceSelector
    {
        private const float AUDIO_REPLACEMENT_TOLERANCE = 0.05f;

        private readonly ChaseAIConfig CHASE_AI_CONFIG;
        private readonly ChaseAIMemory CHASE_AI_MEMORY;
        private readonly ChaseAIInvestigationContext INVESTIGATION_CONTEXT;

        public string ActiveInvestigationName => INVESTIGATION_CONTEXT.HasActiveAudioInvestigation
            ? "Audio evidence"
            : "Director hint";

        public ChaseAIEvidenceSelector(
            ChaseAIConfig chaseAIConfig ,
            ChaseAIMemory chaseAIMemory ,
            ChaseAIInvestigationContext investigationContext)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));
            CHASE_AI_MEMORY = chaseAIMemory != null ? chaseAIMemory : throw new ArgumentNullException(nameof(chaseAIMemory));
            INVESTIGATION_CONTEXT = investigationContext != null ? investigationContext : throw new ArgumentNullException(nameof(investigationContext));
        }

        public bool TryReceiveDirectorHint(MasterAIHint directorHint , float currentTime)
        {
            if ( !directorHint.IsValid(currentTime) )
            {
                return false;
            }

            bool hasHigherPriorityEvidence =
                INVESTIGATION_CONTEXT.HasActiveAudioInvestigation ||
                CHASE_AI_MEMORY.HasValidVisualEvidence(currentTime) ||
                CHASE_AI_MEMORY.HasValidAudioEvidence(currentTime);

            if ( hasHigherPriorityEvidence )
            {
                return false;
            }

            INVESTIGATION_CONTEXT.SetDirectorInvestigation(directorHint);

            return true;
        }

        public bool TryReceiveAudioEvidence(ChaseAIAudioObservation observation)
        {
            float newIntensity = Mathf.Clamp01(observation.PerceivedIntensity);

            bool canReplaceEvidence =
                !INVESTIGATION_CONTEXT.HasActiveAudioInvestigation ||
                newIntensity + AUDIO_REPLACEMENT_TOLERANCE >= INVESTIGATION_CONTEXT.CurrentAudioIntensity;

            if ( !canReplaceEvidence )
            {
                Debug.Log(
                    $"[ChaseAIStateMachine] 약한 소음 무시: " +
                    $"Current={INVESTIGATION_CONTEXT.CurrentAudioIntensity:F2}, " +
                    $"New={newIntensity:F2}");

                return false;
            }

            INVESTIGATION_CONTEXT.SetAudioInvestigation(observation);

            Debug.Log(
                $"[ChaseAIStateMachine] 청각 증거 적용: " +
                $"Intensity={INVESTIGATION_CONTEXT.CurrentAudioIntensity:F2}, " +
                $"Radius={INVESTIGATION_CONTEXT.AudioSearchRadius:F1}, " +
                $"Duration={INVESTIGATION_CONTEXT.AudioSearchDuration:F1}");

            return true;
        }

        public bool TryGetInvestigationDestination(
            out Vector3 investigationPosition ,
            out string context)
        {
            if ( INVESTIGATION_CONTEXT.HasActiveAudioInvestigation )
            {
                investigationPosition = INVESTIGATION_CONTEXT.AudioSearchPosition;
                context = "Audio evidence";

                return true;
            }

            if ( INVESTIGATION_CONTEXT.HasActiveDirectorInvestigation )
            {
                investigationPosition = INVESTIGATION_CONTEXT.ActiveDirectorHint.SearchAnchorPosition;
                context = "Director hint";

                return true;
            }

            investigationPosition = Vector3.zero;
            context = string.Empty;

            return false;
        }

        public bool TryCreateSearchRequest(float currentTime , out ChaseAISearchRequest searchRequest)
        {
            if ( INVESTIGATION_CONTEXT.HasActiveAudioInvestigation )
            {
                Debug.Log(
                    $"[ChaseAIStateMachine] 청각 수색 시작: " +
                    $"Position={INVESTIGATION_CONTEXT.AudioSearchPosition}, " +
                    $"Radius={INVESTIGATION_CONTEXT.AudioSearchRadius:F1}, " +
                    $"Duration={INVESTIGATION_CONTEXT.AudioSearchDuration:F1}");

                searchRequest = new ChaseAISearchRequest(
                    INVESTIGATION_CONTEXT.AudioSearchPosition ,
                    Vector3.zero ,
                    INVESTIGATION_CONTEXT.AudioSearchRadius ,
                    INVESTIGATION_CONTEXT.AudioSearchDuration ,
                    false ,
                    1f ,
                    "Audio search center" ,
                    INVESTIGATION_CONTEXT.CurrentAudioIntensity >= CHASE_AI_CONFIG.StrongNoiseThreshold);

                return true;
            }

            if ( CHASE_AI_MEMORY.HasValidVisualEvidence(currentTime) )
            {
                searchRequest = new ChaseAISearchRequest(
                    CHASE_AI_MEMORY.VisualEvidence.Position ,
                    CHASE_AI_MEMORY.LastSeenMovementDirection ,
                    CHASE_AI_CONFIG.VisualSearchRadius ,
                    CHASE_AI_CONFIG.SearchWaitTime ,
                    true ,
                    1f ,
                    "Last seen position" ,
                    true);

                return true;
            }

            if ( CHASE_AI_MEMORY.HasValidAudioEvidence(currentTime) )
            {
                float intensity = Mathf.Clamp01(CHASE_AI_MEMORY.AudioEvidence.Strength);
                float searchRadius = Mathf.Lerp(CHASE_AI_CONFIG.MaxAudioSearchRadius , CHASE_AI_CONFIG.MinAudioSearchRadius , intensity);
                float searchDuration = Mathf.Lerp(CHASE_AI_CONFIG.MinAudioSearchDuration , CHASE_AI_CONFIG.MaxAudioSearchDuration , intensity);

                searchRequest = new ChaseAISearchRequest(
                    CHASE_AI_MEMORY.AudioEvidence.Position ,
                    Vector3.zero ,
                    searchRadius ,
                    searchDuration ,
                    true ,
                    1f ,
                    "Last heard position" ,
                    intensity >= CHASE_AI_CONFIG.StrongNoiseThreshold);

                return true;
            }

            if ( INVESTIGATION_CONTEXT.HasActiveDirectorInvestigation )
            {
                MasterAIHint directorHint = INVESTIGATION_CONTEXT.ActiveDirectorHint;

                if ( !directorHint.IsValid(currentTime) )
                {
                    Debug.Log(
                        $"[ChaseAIStateMachine] 만료된 Director Hint 수색 생략: " +
                        $"Zone={directorHint.TargetZoneId}");

                    INVESTIGATION_CONTEXT.ClearDirectorInvestigation();
                    searchRequest = default;

                    return false;
                }

                Debug.Log(
                    $"[ChaseAIStateMachine] Director Hint 수색 시작: " +
                    $"Zone={directorHint.TargetZoneId}, " +
                    $"Position={directorHint.SearchAnchorPosition}, " +
                    $"Radius={directorHint.SearchRadius:F1}, " +
                    $"Urgency={directorHint.Urgency:F2}");

                searchRequest = new ChaseAISearchRequest(
                    directorHint.SearchAnchorPosition ,
                    Vector3.zero ,
                    directorHint.SearchRadius ,
                    CHASE_AI_CONFIG.SearchWaitTime ,
                    false ,
                    CHASE_AI_CONFIG.DirectorHintAngerInfluence ,
                    "Director hint search center" ,
                    false ,
                    directorHint.TargetZoneId ,
                    true);

                return true;
            }

            searchRequest = default;

            return false;
        }

        public void ClearAudioInvestigation()
        {
            INVESTIGATION_CONTEXT.ClearAudioInvestigation();
        }

        public void ClearDirectorInvestigation()
        {
            INVESTIGATION_CONTEXT.ClearDirectorInvestigation();
        }

        public void ClearInvestigations()
        {
            INVESTIGATION_CONTEXT.Clear();
        }
    }
}
