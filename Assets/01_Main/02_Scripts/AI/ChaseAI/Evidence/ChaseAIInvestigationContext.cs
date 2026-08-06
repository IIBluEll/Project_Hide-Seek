using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIInvestigationContext
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;

        public bool HasActiveAudioInvestigation { get; private set; }
        public Vector3 AudioSearchPosition { get; private set; }
        public float AudioSearchRadius { get; private set; }
        public float AudioSearchDuration { get; private set; }
        public float CurrentAudioIntensity { get; private set; }

        public bool HasActiveDirectorInvestigation { get; private set; }
        public MasterAIHint ActiveDirectorHint { get; private set; }
        public CHASE_AI_EVIDENCE_TYPE ActiveEvidenceType => HasActiveAudioInvestigation
            ? CHASE_AI_EVIDENCE_TYPE.AUDIO
            : HasActiveDirectorInvestigation
                ? CHASE_AI_EVIDENCE_TYPE.DIRECTOR_HINT
                : CHASE_AI_EVIDENCE_TYPE.NONE;

        public ChaseAIInvestigationContext(ChaseAIConfig chaseAIConfig)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));
        }

        public void SetAudioInvestigation(ChaseAIAudioObservation observation)
        {
            float intensity = Mathf.Clamp01(observation.PerceivedIntensity);

            AudioSearchPosition = observation.NoiseData.Position;
            AudioSearchRadius = Mathf.Lerp(CHASE_AI_CONFIG.MaxAudioSearchRadius , CHASE_AI_CONFIG.MinAudioSearchRadius , intensity);
            AudioSearchDuration = Mathf.Lerp(CHASE_AI_CONFIG.MinAudioSearchDuration , CHASE_AI_CONFIG.MaxAudioSearchDuration , intensity);
            CurrentAudioIntensity = intensity;
            HasActiveAudioInvestigation = true;

            ClearDirectorInvestigation();
        }

        public void SetDirectorInvestigation(MasterAIHint directorHint)
        {
            ActiveDirectorHint = directorHint;
            HasActiveDirectorInvestigation = true;
        }

        public void ClearAudioInvestigation()
        {
            AudioSearchPosition = Vector3.zero;
            AudioSearchRadius = 0f;
            AudioSearchDuration = 0f;
            CurrentAudioIntensity = 0f;
            HasActiveAudioInvestigation = false;
        }

        public void ClearDirectorInvestigation()
        {
            ActiveDirectorHint = default;
            HasActiveDirectorInvestigation = false;
        }

        public void Clear()
        {
            ClearAudioInvestigation();
            ClearDirectorInvestigation();
        }
    }
}
