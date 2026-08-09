using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIDirector
    {
        private readonly MasterAIConfig MASTER_AI_CONFIG;
        private readonly MasterAIGauge MASTER_AI_GAUGE;

        private float _dormantTimer;
        private bool _isRetreatRequested;
        private float _retreatRetryTimer;
        private float _hintTimer;

        public MASTER_AI_STATE CurrentState { get; private set; }
        public float GlobalStress => MASTER_AI_GAUGE.GlobalStress;
        public float GlobalStressRatio => MASTER_AI_GAUGE.GlobalStressRatio;

        public MasterAIDirector(MasterAIConfig masterAIConfig)
        {
            MASTER_AI_CONFIG = masterAIConfig != null ? masterAIConfig : throw new ArgumentNullException(nameof(masterAIConfig));
            MASTER_AI_GAUGE = new MasterAIGauge(MASTER_AI_CONFIG);

            Reset();
        }

        public MASTER_AI_COMMAND Tick(float deltaTime , CHASE_AI_STATE chaseAIState , float distanceToPlayer)
        {
            if ( deltaTime <= 0f )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            switch ( CurrentState )
            {
                case MASTER_AI_STATE.DORMANT:
                    return UpdateDormant(deltaTime);

                case MASTER_AI_STATE.ACTIVE:
                    return UpdateActive(deltaTime , chaseAIState , distanceToPlayer);

                default:
                    return MASTER_AI_COMMAND.NONE;
            }
        }

        public void NotifyChaseAIDormant()
        {
            EnterDormant();
        }

        public void NotifyRetreatFailed()
        {
            if ( CurrentState != MASTER_AI_STATE.ACTIVE )
            {
                return;
            }

            _isRetreatRequested = false;
            _retreatRetryTimer = MASTER_AI_CONFIG.RetreatRetryDelay;
        }

        public void Reset()
        {
            MASTER_AI_GAUGE.Reset();
            EnterDormant();
        }

        private MASTER_AI_COMMAND UpdateDormant(float deltaTime)
        {
            _dormantTimer += deltaTime;
            MASTER_AI_GAUGE.DecreaseGlobalStress(MASTER_AI_CONFIG.DormantStressDecreaseRate * deltaTime);

            if ( _dormantTimer < MASTER_AI_CONFIG.MinimumDormantDuration )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            if ( !MASTER_AI_GAUGE.IsReactivationThresholdReached )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            CurrentState = MASTER_AI_STATE.ACTIVE;
            _isRetreatRequested = false;
            _retreatRetryTimer = 0f;

            ResetHintTimer();

            return MASTER_AI_COMMAND.ACTIVATE;
        }

        private MASTER_AI_COMMAND UpdateActive(float deltaTime , CHASE_AI_STATE chaseAIState , float distanceToPlayer)
        {
            if ( chaseAIState == CHASE_AI_STATE.ATTACK )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            UpdateActiveGlobalStress(deltaTime , chaseAIState , distanceToPlayer);
            UpdateHintTimer(deltaTime , chaseAIState);

            _retreatRetryTimer = Mathf.Max(0f , _retreatRetryTimer - deltaTime);

            if ( _isRetreatRequested )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            if ( MASTER_AI_GAUGE.IsRetreatThresholdReached )
            {
                if ( _retreatRetryTimer > 0f )
                {
                    return MASTER_AI_COMMAND.NONE;
                }

                _isRetreatRequested = true;

                return MASTER_AI_COMMAND.RETREAT;
            }

            if ( _retreatRetryTimer > 0f )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            if ( chaseAIState != CHASE_AI_STATE.PATROL || _hintTimer > 0f )
            {
                return MASTER_AI_COMMAND.NONE;
            }

            ResetHintTimer();

            return MASTER_AI_COMMAND.DIRECTOR_HINT;
        }

        private void UpdateActiveGlobalStress(float deltaTime , CHASE_AI_STATE chaseAIState , float distanceToPlayer)
        {
            bool isChasing = chaseAIState == CHASE_AI_STATE.CHASE;
            float proximityRatio = 1f - Mathf.Clamp01(Mathf.Max(0f, distanceToPlayer) / MASTER_AI_CONFIG.SafeDistance);
            bool isNearPlayer = proximityRatio > 0f;

            if ( isChasing )
            {
                MASTER_AI_GAUGE.IncreaseGlobalStress(MASTER_AI_CONFIG.ChaseStressIncreaseRate * deltaTime);
            }

            if ( isNearPlayer )
            {
                float proximityStress = MASTER_AI_CONFIG.NearStressIncreaseRate * proximityRatio * deltaTime;
                MASTER_AI_GAUGE.IncreaseGlobalStress(proximityStress);
            }

            if ( !isChasing && !isNearPlayer )
            {
                MASTER_AI_GAUGE.DecreaseGlobalStress(MASTER_AI_CONFIG.ActiveStressDecreaseRate * deltaTime);
            }
        }

        private void EnterDormant()
        {
            CurrentState = MASTER_AI_STATE.DORMANT;
            _dormantTimer = 0f;
            _retreatRetryTimer = 0f;
            _hintTimer = 0f;
            _isRetreatRequested = false;
        }

        private void UpdateHintTimer(float deltaTime , CHASE_AI_STATE chaseAIState)
        {
            if ( chaseAIState != CHASE_AI_STATE.PATROL || _isRetreatRequested )
            {
                return;
            }

            _hintTimer = Mathf.Max(0f , _hintTimer - deltaTime);
        }

        private void ResetHintTimer()
        {
            _hintTimer = UnityEngine.Random.Range(
                MASTER_AI_CONFIG.MinimumHintInterval ,
                MASTER_AI_CONFIG.MaximumHintInterval);
        }
    }
}
