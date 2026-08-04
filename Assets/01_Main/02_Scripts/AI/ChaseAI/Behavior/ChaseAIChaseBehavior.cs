using System;
using UnityEngine;

namespace HideSeek.AI
{
    public enum CHASE_AI_CHASE_RESULT
    {
        RUNNING,
        TARGET_LOST,
        MOVEMENT_FAILED
    }

    public sealed class ChaseAIChaseBehavior
    {
        private readonly ChaseAIConfig CHASE_AI_CONFIG;
        private readonly ChaseAIMovement CHASE_AI_MOVEMENT;
        private readonly ChaseAIAnger CHASE_AI_ANGER;

        private float _chaseRepathTimer;
        private float _lostSightTimer;

        public string LastResultReason { get; private set; } = string.Empty;

        public ChaseAIChaseBehavior(
            ChaseAIConfig chaseAIConfig ,
            ChaseAIMovement chaseAIMovement ,
            ChaseAIAnger chaseAIAnger)
        {
            CHASE_AI_CONFIG = chaseAIConfig != null ? chaseAIConfig : throw new ArgumentNullException(nameof(chaseAIConfig));
            CHASE_AI_MOVEMENT = chaseAIMovement != null ? chaseAIMovement : throw new ArgumentNullException(nameof(chaseAIMovement));
            CHASE_AI_ANGER = chaseAIAnger != null ? chaseAIAnger : throw new ArgumentNullException(nameof(chaseAIAnger));
        }

        public void Begin()
        {
            CHASE_AI_MOVEMENT.SetSpeed(CHASE_AI_CONFIG.ChaseSpeed * CHASE_AI_ANGER.ChaseSpeedMultiplier);
            _chaseRepathTimer = 0f;
            _lostSightTimer = 0f;
            LastResultReason = string.Empty;
        }

        public CHASE_AI_CHASE_RESULT Tick(
            float deltaTime ,
            ChaseAIVisualObservation visualObservation)
        {
            _chaseRepathTimer -= deltaTime;

            if ( visualObservation.HasLineOfSight )
            {
                _lostSightTimer = 0f;

                UpdateVisibleTargetDestination(visualObservation.VisiblePosition);
            }
            else
            {
                _lostSightTimer += deltaTime;

                if ( _lostSightTimer >= CHASE_AI_CONFIG.VisualLoseTime )
                {
                    LastResultReason = "Visual lose time expired";

                    return CHASE_AI_CHASE_RESULT.TARGET_LOST;
                }
            }

            CHASE_AI_MOVE_STATUS moveStatus = CHASE_AI_MOVEMENT.UpdateMovement(deltaTime);

            if ( moveStatus == CHASE_AI_MOVE_STATUS.PATH_FAILED || moveStatus == CHASE_AI_MOVE_STATUS.STUCK )
            {
                LastResultReason = $"Chase movement failed: {moveStatus}";

                return CHASE_AI_CHASE_RESULT.MOVEMENT_FAILED;
            }

            return CHASE_AI_CHASE_RESULT.RUNNING;
        }

        public void RefreshAngerEffect()
        {
            CHASE_AI_MOVEMENT.SetSpeed(CHASE_AI_CONFIG.ChaseSpeed * CHASE_AI_ANGER.ChaseSpeedMultiplier);
        }

        public void Stop()
        {
            _chaseRepathTimer = 0f;
            _lostSightTimer = 0f;
            LastResultReason = string.Empty;
        }

        private void UpdateVisibleTargetDestination(Vector3 visiblePosition)
        {
            float updateDistance = CHASE_AI_CONFIG.ChaseDestinationUpdateDistance;
            float squaredUpdateDistance = updateDistance * updateDistance;

            bool hasMovedFromDestination =
                !CHASE_AI_MOVEMENT.HasDestination ||
                ( visiblePosition - CHASE_AI_MOVEMENT.CurrentDestination).sqrMagnitude >= squaredUpdateDistance;

            if ( _chaseRepathTimer > 0f || !hasMovedFromDestination )
            {
                return;
            }

            RequestDestination(visiblePosition , "Chase target");
            _chaseRepathTimer = CHASE_AI_CONFIG.ChaseRepathInterval;
        }

        private bool RequestDestination(Vector3 position , string context)
        {
            CHASE_AI_MOVE_REQUEST_RESULT result = CHASE_AI_MOVEMENT.TrySetDestination(position);

            if ( result == CHASE_AI_MOVE_REQUEST_RESULT.ACCEPTED )
            {
                return true;
            }

            Debug.LogWarning($"[ChaseAIStateMachine] {context} 목적지 요청 실패: {result}");

            return false;
        }
    }
}
