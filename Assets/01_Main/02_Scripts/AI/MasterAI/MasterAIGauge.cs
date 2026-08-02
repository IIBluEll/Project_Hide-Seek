using System;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIGauge
    {
        private readonly MasterAIConfig MASTER_AI_CONFIG;

        public float GlobalStress { get; private set; }
        public float GlobalStressRatio => Mathf.Clamp01(GlobalStress / MASTER_AI_CONFIG.MaximumGlobalStress);
        public bool IsRetreatThresholdReached => GlobalStress >= MASTER_AI_CONFIG.RetreatStressThreshold;
        public bool IsReactivationThresholdReached => GlobalStress <= MASTER_AI_CONFIG.ReactivationStressThreshold;

        public MasterAIGauge(MasterAIConfig masterAIConfig)
        {
            MASTER_AI_CONFIG = masterAIConfig != null ? masterAIConfig : throw new ArgumentNullException(nameof(masterAIConfig));

            Reset();
        }

        public void IncreaseGlobalStress(float amount)
        {
            if ( amount <= 0f )
            {
                return;
            }

            SetGlobalStress(GlobalStress + amount);
        }

        public void DecreaseGlobalStress(float amount)
        {
            if ( amount <= 0f )
            {
                return;
            }

            SetGlobalStress(GlobalStress - amount);
        }

        public void SetGlobalStress(float value)
        {
            GlobalStress = Mathf.Clamp(value , 0f , MASTER_AI_CONFIG.MaximumGlobalStress);
        }

        public void Reset()
        {
            GlobalStress = 0f;
        }
    }
}