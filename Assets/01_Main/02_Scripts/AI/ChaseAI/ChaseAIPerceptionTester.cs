using UnityEngine;

namespace HideSeek.AI
{
    public sealed class ChaseAIPerceptionTester : MonoBehaviour
    {
        [SerializeField] private ChaseAIPerception _perception;

        private CHASE_AI_VISUAL_STATE _previousState = CHASE_AI_VISUAL_STATE.NONE;

        private void Update()
        {
            if ( _perception == null )
            {
                return;
            }

            ChaseAIVisualObservation observation = _perception.UpdatePerception(Time.deltaTime);

            if ( observation.State == _previousState )
            {
                return;
            }

            Debug.Log(
                $"[PerceptionTester] 시야 상태 변경: " +
                $"{_previousState} → {observation.State}, " +
                $"인지율: {observation.DetectionRatio:F2}" ,
                this);

            _previousState = observation.State;
        }
    }
}
