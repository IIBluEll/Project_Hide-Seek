using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIAnger
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;

        public event Action Changed;

        public float CurrentAnger { get; private set; }
        public float AngerFloor { get; private set; }
        public int CompletedGeneratorCount { get; private set; }

        public float AngerRatio => Mathf.Clamp01(CurrentAnger / CHASE_AI_CONFIG.MaximumAnger);
        public float ChaseSpeedMultiplier => Mathf.Lerp(1f , CHASE_AI_CONFIG.MaximumAngerChaseSpeedMultiplier , AngerRatio);
        public float SearchDurationMultiplier => Mathf.Lerp(1f , CHASE_AI_CONFIG.MaximumAngerSearchDurationMultiplier , AngerRatio);

        public ChaseAIAnger(ChaseAIConfig chaseAIConfig)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));

            Reset();
        }

        public void SetCompletedGeneratorCount(int completedGeneratorCount)
        {
            int normalizedCompletedGeneratorCount = Mathf.Max(0 , completedGeneratorCount);
            bool wasProgressReset = normalizedCompletedGeneratorCount < CompletedGeneratorCount;

            float previousAnger = CurrentAnger;
            float previousAngerFloor = AngerFloor;
            int previousCompletedGeneratorCount = CompletedGeneratorCount;

            CompletedGeneratorCount = normalizedCompletedGeneratorCount;
            AngerFloor = CHASE_AI_CONFIG.GetGeneratorAngerFloor(CompletedGeneratorCount);
            CurrentAnger = wasProgressReset ? AngerFloor : Mathf.Max(CurrentAnger , AngerFloor);

            NotifyChangedIfNeeded(previousAnger , previousAngerFloor , previousCompletedGeneratorCount);
        }

        public void IncreaseAnger(float amount)
        {
            if ( amount <= 0f )
            {
                return;
            }

            float previousAnger = CurrentAnger;

            CurrentAnger = Mathf.Clamp(CurrentAnger + amount , AngerFloor , CHASE_AI_CONFIG.MaximumAnger);

            if ( !Mathf.Approximately(previousAnger , CurrentAnger) )
            {
                Changed?.Invoke();
            }
        }

        public void DecreaseAnger(float amount)
        {
            if ( amount <= 0f )
            {
                return;
            }

            float previousAnger = CurrentAnger;

            CurrentAnger = Mathf.Clamp(CurrentAnger - amount , AngerFloor , CHASE_AI_CONFIG.MaximumAnger);

            if ( !Mathf.Approximately(previousAnger , CurrentAnger) )
            {
                Changed?.Invoke();
            }
        }

        public void Reset()
        {
            float previousAnger = CurrentAnger;
            float previousAngerFloor = AngerFloor;
            int previousCompletedGeneratorCount = CompletedGeneratorCount;

            CompletedGeneratorCount = 0;
            AngerFloor = CHASE_AI_CONFIG.GetGeneratorAngerFloor(0);
            CurrentAnger = AngerFloor;

            NotifyChangedIfNeeded(previousAnger , previousAngerFloor , previousCompletedGeneratorCount);
        }

        private void NotifyChangedIfNeeded(
            float previousAnger ,
            float previousAngerFloor ,
            int previousCompletedGeneratorCount)
        {
            bool wasChanged =
                !Mathf.Approximately(previousAnger , CurrentAnger) ||
                !Mathf.Approximately(previousAngerFloor , AngerFloor) ||
                previousCompletedGeneratorCount != CompletedGeneratorCount;

            if ( wasChanged )
            {
                Changed?.Invoke();
            }
        }
    }
}
