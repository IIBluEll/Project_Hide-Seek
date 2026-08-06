using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIAnger
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;

        private float _calmElapsed;
        private float _visibleChaseElapsed;

        public event Action Changed;

        public float CurrentAnger { get; private set; }
        public float AngerFloor { get; private set; }
        public int CompletedGeneratorCount { get; private set; }

        public float AngerRatio => Mathf.Clamp01(CurrentAnger / CHASE_AI_CONFIG.MaximumAnger);
        public float ChaseSpeedMultiplier => Mathf.Lerp(1f , CHASE_AI_CONFIG.MaximumAngerChaseSpeedMultiplier , AngerRatio);
        public float SearchRadiusMultiplier => GetSearchRadiusMultiplier(1f);
        public int SearchPointCount => GetSearchPointCount(1f);

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
                _calmElapsed = 0f;
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

        public float GetSearchRadiusMultiplier(float angerInfluence)
        {
            float effectiveAngerRatio = AngerRatio * Mathf.Clamp01(angerInfluence);

            return Mathf.Lerp(1f , CHASE_AI_CONFIG.MaximumAngerSearchRadiusMultiplier , effectiveAngerRatio);
        }

        public void ResetCalmDecay()
        {
            _calmElapsed = 0f;
        }

        public void ResetChaseBuildUp()
        {
            _visibleChaseElapsed = 0f;
        }

        public bool TickChaseBuildUp(float deltaTime , bool hasLineOfSight)
        {
            if ( deltaTime <= 0f || !hasLineOfSight || CurrentAnger >= CHASE_AI_CONFIG.MaximumAnger )
            {
                return false;
            }

            float previousVisibleChaseElapsed = _visibleChaseElapsed;
            _visibleChaseElapsed += deltaTime;

            float buildUpDelay = CHASE_AI_CONFIG.AngerChaseBuildUpDelay;
            float activeBuildUpTime =
                Mathf.Max(0f , _visibleChaseElapsed - buildUpDelay) -
                Mathf.Max(0f , previousVisibleChaseElapsed - buildUpDelay);

            if ( activeBuildUpTime <= 0f )
            {
                return false;
            }

            float previousAnger = CurrentAnger;

            IncreaseAnger(CHASE_AI_CONFIG.AngerIncreasePerChaseSecond * activeBuildUpTime);

            return !Mathf.Approximately(previousAnger , CurrentAnger);
        }

        public void TickCalmDecay(float deltaTime)
        {
            if ( deltaTime <= 0f || CurrentAnger <= AngerFloor )
            {
                return;
            }

            _calmElapsed += deltaTime;

            if ( _calmElapsed < CHASE_AI_CONFIG.AngerCalmDelay )
            {
                return;
            }

            DecreaseAnger(CHASE_AI_CONFIG.AngerDecreasePerSecond * deltaTime);
        }

        public int GetSearchPointCount(float angerInfluence)
        {
            float effectiveAngerRatio = AngerRatio * Mathf.Clamp01(angerInfluence);
            float searchPointCount = Mathf.Lerp(
                CHASE_AI_CONFIG.MinimumAngerSearchPointCount ,
                CHASE_AI_CONFIG.MaximumAngerSearchPointCount ,
                effectiveAngerRatio);

            return Mathf.RoundToInt(searchPointCount);
        }

        public void Reset()
        {
            float previousAnger = CurrentAnger;
            float previousAngerFloor = AngerFloor;
            int previousCompletedGeneratorCount = CompletedGeneratorCount;

            CompletedGeneratorCount = 0;
            AngerFloor = CHASE_AI_CONFIG.GetGeneratorAngerFloor(0);
            CurrentAnger = AngerFloor;
            _calmElapsed = 0f;
            _visibleChaseElapsed = 0f;

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
