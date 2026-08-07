using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIEvidenceSelector
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;
        private readonly ChaseAIMemory CHASE_AI_MEMORY;
        private readonly ChaseAIInvestigationContext INVESTIGATION_CONTEXT;

        public string ActiveInvestigationName => INVESTIGATION_CONTEXT.HasActiveAudioInvestigation
            ? "Audio evidence"
            : "Director hint";
        public CHASE_AI_EVIDENCE_TYPE ActiveInvestigationType => INVESTIGATION_CONTEXT.ActiveEvidenceType;
        public bool HasActiveAudioInvestigation => INVESTIGATION_CONTEXT.HasActiveAudioInvestigation;
        public NOISE_TYPE ActiveAudioNoiseType => INVESTIGATION_CONTEXT.CurrentAudioNoiseType;
        public float CurrentAudioIntensity => INVESTIGATION_CONTEXT.CurrentAudioIntensity;
        public float LastAudioCandidateScore { get; private set; }
        public string LastAudioDecisionReason { get; private set; } = "NONE";

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

            INVESTIGATION_CONTEXT.SetDirectorInvestigation(directorHint);

            return true;
        }

        public bool TryReceiveAudioEvidence(
            ChaseAIAudioObservation observation ,
            float currentTime)
        {
            float newIntensity = Mathf.Clamp01(observation.PerceivedIntensity);
            float newEvidenceDuration = ResolveAudioEvidenceDuration(newIntensity);
            float newFreshness = CalculateFreshness(
                observation.NoiseData.OccurredTime ,
                newEvidenceDuration ,
                currentTime);
            float newScore = CalculateAudioScore(
                newIntensity ,
                observation.NoiseData.NoiseType ,
                newFreshness);

            LastAudioCandidateScore = newScore;

            if ( !INVESTIGATION_CONTEXT.HasActiveAudioInvestigation )
            {
                return AcceptAudioEvidence(
                    observation ,
                    currentTime ,
                    newScore ,
                    "ACCEPTED_NO_ACTIVE_AUDIO");
            }

            float currentScore = GetCurrentAudioScore(currentTime);
            bool hasScoreAdvantage =
                newScore >= currentScore + CHASE_AI_CONFIG.AudioEvidenceReplacementMargin;
            bool isSameSource = IsSameAudioSource(observation.NoiseData);

            if ( isSameSource )
            {
                float newBaseScore = CalculateAudioScore(
                    newIntensity ,
                    observation.NoiseData.NoiseType ,
                    1f);
                float currentBaseScore = CalculateAudioScore(
                    INVESTIGATION_CONTEXT.CurrentAudioIntensity ,
                    INVESTIGATION_CONTEXT.CurrentAudioNoiseType ,
                    1f);
                bool isSameSourceStronger =
                    newBaseScore >= currentBaseScore + CHASE_AI_CONFIG.AudioEvidenceReplacementMargin;

                if ( isSameSourceStronger )
                {
                    return AcceptAudioEvidence(
                        observation ,
                        currentTime ,
                        newScore ,
                        "ACCEPTED_SAME_SOURCE_STRONGER");
                }

                float timeSinceAccepted =
                    Mathf.Max(0f , currentTime - INVESTIGATION_CONTEXT.CurrentAudioAcceptedTime);

                if ( timeSinceAccepted < CHASE_AI_CONFIG.SameSourceRetargetInterval )
                {
                    return RejectAudioEvidence(
                        newScore ,
                        currentScore ,
                        "REJECTED_SAME_SOURCE_COOLDOWN");
                }

                Vector3 positionOffset =
                    observation.NoiseData.Position - INVESTIGATION_CONTEXT.AudioSearchPosition;
                positionOffset.y = 0f;

                float retargetDistance = CHASE_AI_CONFIG.SameSourceRetargetDistance;

                if ( positionOffset.sqrMagnitude < retargetDistance * retargetDistance )
                {
                    return RejectAudioEvidence(
                        newScore ,
                        currentScore ,
                        "REJECTED_SAME_SOURCE_POSITION_UNCHANGED");
                }

                return AcceptAudioEvidence(
                    observation ,
                    currentTime ,
                    newScore ,
                    "ACCEPTED_SAME_SOURCE_MOVED");
            }

            if ( !hasScoreAdvantage )
            {
                return RejectAudioEvidence(
                    newScore ,
                    currentScore ,
                    "REJECTED_LOWER_PRIORITY");
            }

            return AcceptAudioEvidence(
                observation ,
                currentTime ,
                newScore ,
                "ACCEPTED_HIGHER_PRIORITY");
        }

        public float GetCurrentAudioFreshness(float currentTime)
        {
            if ( !INVESTIGATION_CONTEXT.HasActiveAudioInvestigation )
            {
                return 0f;
            }

            return CalculateFreshness(
                INVESTIGATION_CONTEXT.CurrentAudioOccurredTime ,
                INVESTIGATION_CONTEXT.CurrentAudioEvidenceDuration ,
                currentTime);
        }

        public float GetCurrentAudioScore(float currentTime)
        {
            if ( !INVESTIGATION_CONTEXT.HasActiveAudioInvestigation )
            {
                return 0f;
            }

            return CalculateAudioScore(
                INVESTIGATION_CONTEXT.CurrentAudioIntensity ,
                INVESTIGATION_CONTEXT.CurrentAudioNoiseType ,
                GetCurrentAudioFreshness(currentTime));
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
                    $"[ChaseAIEvidenceSelector] Audio search prepared: " +
                    $"Position={INVESTIGATION_CONTEXT.AudioSearchPosition}, " +
                    $"Radius={INVESTIGATION_CONTEXT.AudioSearchRadius:F1}, " +
                    $"Duration={INVESTIGATION_CONTEXT.AudioSearchDuration:F1}");

                searchRequest = new ChaseAISearchRequest(
                    INVESTIGATION_CONTEXT.AudioSearchPosition ,
                    Vector3.zero ,
                    INVESTIGATION_CONTEXT.AudioSearchRadius ,
                    INVESTIGATION_CONTEXT.AudioSearchDuration ,
                    1f ,
                    "Audio search center" ,
                    CHASE_AI_EVIDENCE_TYPE.AUDIO ,
                    INVESTIGATION_CONTEXT.CurrentAudioIntensity >= CHASE_AI_CONFIG.StrongNoiseThreshold ,
                    ResolveAudioHidingSpotInspectionChance(INVESTIGATION_CONTEXT.CurrentAudioIntensity));

                return true;
            }

            if ( CHASE_AI_MEMORY.HasValidVisualEvidence(currentTime) )
            {
                searchRequest = new ChaseAISearchRequest(
                    CHASE_AI_MEMORY.VisualEvidence.Position ,
                    CHASE_AI_MEMORY.LastSeenMovementDirection ,
                    CHASE_AI_CONFIG.VisualSearchRadius ,
                    CHASE_AI_CONFIG.SearchWaitTime ,
                    1f ,
                    "Last seen position" ,
                    CHASE_AI_EVIDENCE_TYPE.VISUAL ,
                    true ,
                    CHASE_AI_CONFIG.VisualHidingSpotInspectionChance);

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
                    1f ,
                    "Last heard position" ,
                    CHASE_AI_EVIDENCE_TYPE.AUDIO ,
                    intensity >= CHASE_AI_CONFIG.StrongNoiseThreshold ,
                    ResolveAudioHidingSpotInspectionChance(intensity));

                return true;
            }

            if ( INVESTIGATION_CONTEXT.HasActiveDirectorInvestigation )
            {
                MasterAIHint directorHint = INVESTIGATION_CONTEXT.ActiveDirectorHint;

                Debug.Log(
                    $"[ChaseAIEvidenceSelector] Director hint search prepared: " +
                    $"Zone={directorHint.TargetZoneId}, " +
                    $"Position={directorHint.SearchAnchorPosition}, " +
                    $"Radius={directorHint.SearchRadius:F1}, " +
                    $"Urgency={directorHint.Urgency:F2}");

                searchRequest = new ChaseAISearchRequest(
                    directorHint.SearchAnchorPosition ,
                    Vector3.zero ,
                    directorHint.SearchRadius ,
                    CHASE_AI_CONFIG.SearchWaitTime ,
                    CHASE_AI_CONFIG.DirectorHintAngerInfluence ,
                    "Director hint search center" ,
                    CHASE_AI_EVIDENCE_TYPE.DIRECTOR_HINT ,
                    false ,
                    0f ,
                    directorHint.TargetZoneId ,
                    true);

                return true;
            }

            searchRequest = default;

            return false;
        }

        public void ClearInvestigations()
        {
            INVESTIGATION_CONTEXT.Clear();
        }

        public void ClearAllEvidence()
        {
            INVESTIGATION_CONTEXT.Clear();
            CHASE_AI_MEMORY.Clear();
            LastAudioCandidateScore = 0f;
            LastAudioDecisionReason = "NONE";
        }

        private bool AcceptAudioEvidence(
            ChaseAIAudioObservation observation ,
            float currentTime ,
            float newScore ,
            string decisionReason)
        {
            INVESTIGATION_CONTEXT.SetAudioInvestigation(observation , currentTime);
            LastAudioDecisionReason = decisionReason;

            Debug.Log(
                $"[ChaseAIEvidenceSelector] Audio evidence accepted: " +
                $"Type={observation.NoiseData.NoiseType}, " +
                $"Intensity={INVESTIGATION_CONTEXT.CurrentAudioIntensity:F2}, " +
                $"Score={newScore:F2}, " +
                $"Reason={decisionReason}");

            return true;
        }

        private bool RejectAudioEvidence(
            float newScore ,
            float currentScore ,
            string decisionReason)
        {
            LastAudioDecisionReason = decisionReason;

            Debug.Log(
                $"[ChaseAIEvidenceSelector] Audio evidence rejected: " +
                $"CurrentScore={currentScore:F2}, " +
                $"CandidateScore={newScore:F2}, " +
                $"Reason={decisionReason}");

            return false;
        }

        private bool IsSameAudioSource(NoiseData noiseData)
        {
            return INVESTIGATION_CONTEXT.CurrentAudioSourceObj != null &&
                noiseData.SourceObj == INVESTIGATION_CONTEXT.CurrentAudioSourceObj &&
                noiseData.NoiseType == INVESTIGATION_CONTEXT.CurrentAudioNoiseType;
        }

        private float CalculateAudioScore(
            float intensity ,
            NOISE_TYPE noiseType ,
            float freshness)
        {
            return Mathf.Clamp01(intensity) *
                CHASE_AI_CONFIG.GetNoisePriorityWeight(noiseType) *
                Mathf.Clamp01(freshness);
        }

        private float CalculateFreshness(
            float occurredTime ,
            float evidenceDuration ,
            float currentTime)
        {
            if ( evidenceDuration <= Mathf.Epsilon )
            {
                return CHASE_AI_CONFIG.MinimumAudioFreshnessMultiplier;
            }

            float evidenceAge = Mathf.Max(0f , currentTime - occurredTime);
            float ageRatio = Mathf.Clamp01(evidenceAge / evidenceDuration);

            return Mathf.Lerp(
                1f ,
                CHASE_AI_CONFIG.MinimumAudioFreshnessMultiplier ,
                ageRatio);
        }

        private float ResolveAudioEvidenceDuration(float intensity)
        {
            return intensity >= CHASE_AI_CONFIG.StrongNoiseThreshold
                ? CHASE_AI_CONFIG.StrongNoiseEvidenceDuration
                : CHASE_AI_CONFIG.WeakNoiseEvidenceDuration;
        }

        private float ResolveAudioHidingSpotInspectionChance(float intensity)
        {
            return intensity >= CHASE_AI_CONFIG.StrongNoiseThreshold
                ? CHASE_AI_CONFIG.StrongAudioHidingSpotInspectionChance
                : 0f;
        }
    }
}
