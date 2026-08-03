using UnityEngine;

namespace HideSeek.AI
{
    public readonly struct ChaseAIDebugSnapshot
    {
        public bool IsInitialized { get; }
        public CHASE_AI_STATE State { get; }
        public bool IsRetreatPending { get; }

        public CHASE_AI_VISUAL_STATE VisualState { get; }
        public bool HasLineOfSight { get; }
        public float DetectionRatio { get; }

        public bool HasVisualMemory { get; }
        public Vector3 VisualMemoryPosition { get; }
        public float VisualMemoryStrength { get; }
        public float VisualMemoryRemainingTime { get; }

        public bool HasAudioMemory { get; }
        public Vector3 AudioMemoryPosition { get; }
        public float AudioMemoryStrength { get; }
        public float AudioMemoryRemainingTime { get; }
        public NOISE_TYPE LastNoiseType { get; }

        public string ActiveInvestigationName { get; }
        public string ActiveSearchContext { get; }
        public string ActiveSearchZoneName { get; }
        public bool IsSearchZoneRestricted { get; }
        public int CurrentSearchPointIndex { get; }
        public int SearchPointCount { get; }
        public string CurrentSearchPointSource { get; }

        public float CurrentAnger { get; }
        public float AngerFloor { get; }
        public int CompletedGeneratorCount { get; }
        public float ChaseSpeedMultiplier { get; }
        public float SearchRadiusMultiplier { get; }
        public int AngerSearchPointCount { get; }

        public ChaseAIDebugSnapshot(
            bool isInitialized ,
            CHASE_AI_STATE state ,
            bool isRetreatPending ,
            CHASE_AI_VISUAL_STATE visualState ,
            bool hasLineOfSight ,
            float detectionRatio ,
            bool hasVisualMemory ,
            Vector3 visualMemoryPosition ,
            float visualMemoryStrength ,
            float visualMemoryRemainingTime ,
            bool hasAudioMemory ,
            Vector3 audioMemoryPosition ,
            float audioMemoryStrength ,
            float audioMemoryRemainingTime ,
            NOISE_TYPE lastNoiseType ,
            string activeInvestigationName ,
            string activeSearchContext ,
            string activeSearchZoneName ,
            bool isSearchZoneRestricted ,
            int currentSearchPointIndex ,
            int searchPointCount ,
            string currentSearchPointSource ,
            float currentAnger ,
            float angerFloor ,
            int completedGeneratorCount ,
            float chaseSpeedMultiplier ,
            float searchRadiusMultiplier ,
            int angerSearchPointCount)
        {
            IsInitialized = isInitialized;
            State = state;
            IsRetreatPending = isRetreatPending;
            VisualState = visualState;
            HasLineOfSight = hasLineOfSight;
            DetectionRatio = detectionRatio;
            HasVisualMemory = hasVisualMemory;
            VisualMemoryPosition = visualMemoryPosition;
            VisualMemoryStrength = visualMemoryStrength;
            VisualMemoryRemainingTime = visualMemoryRemainingTime;
            HasAudioMemory = hasAudioMemory;
            AudioMemoryPosition = audioMemoryPosition;
            AudioMemoryStrength = audioMemoryStrength;
            AudioMemoryRemainingTime = audioMemoryRemainingTime;
            LastNoiseType = lastNoiseType;
            ActiveInvestigationName = activeInvestigationName;
            ActiveSearchContext = activeSearchContext;
            ActiveSearchZoneName = activeSearchZoneName;
            IsSearchZoneRestricted = isSearchZoneRestricted;
            CurrentSearchPointIndex = currentSearchPointIndex;
            SearchPointCount = searchPointCount;
            CurrentSearchPointSource = currentSearchPointSource;
            CurrentAnger = currentAnger;
            AngerFloor = angerFloor;
            CompletedGeneratorCount = completedGeneratorCount;
            ChaseSpeedMultiplier = chaseSpeedMultiplier;
            SearchRadiusMultiplier = searchRadiusMultiplier;
            AngerSearchPointCount = angerSearchPointCount;
        }
    }
}
