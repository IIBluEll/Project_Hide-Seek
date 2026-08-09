using UnityEngine;

namespace HideSeek.AI
{
    /// <summary>
    /// Animator가 있는 모델 오브젝트에서 발생한 Animation Event를
    /// 루트의 ChaseAIAnimator로 전달한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChaseAIAnimationEventRelay : MonoBehaviour
    {
        private ChaseAIAnimator _chaseAIAnimator;

        public void Initialize(ChaseAIAnimator chaseAIAnimator)
        {
            _chaseAIAnimator = chaseAIAnimator;
        }

        public void OnLeftFootstepActioned()
        {
            _chaseAIAnimator?.NotifyFootstepActioned(CHASE_AI_FOOT.LEFT);
        }

        public void OnRightFootstepActioned()
        {
            _chaseAIAnimator?.NotifyFootstepActioned(CHASE_AI_FOOT.RIGHT);
        }

        public void OnSearchContactActioned()
        {
            _chaseAIAnimator?.NotifySearchContactActioned();
        }

        public void OnHidingSpotContactActioned()
        {
            _chaseAIAnimator?.NotifyHidingSpotContactActioned();
        }
    }
}
