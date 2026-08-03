using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    /// <summary>
    /// Chase AI의 이동 상태를 Animator Blend 값으로 변환한다.
    /// 이동과 상태 판단은 기존 AI가 담당하며, 이 컴포넌트는 표현만 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ChaseAIAnimator : MonoBehaviour
    {
        private const string BLEND_PARAMETER_NAME = "Blend";

        private static readonly int BLEND_PARAMETER_HASH =
            Animator.StringToHash(BLEND_PARAMETER_NAME);

        [Header("References")]
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private Animator _animator;

        [Header("Blend Values")]
        [SerializeField, Range(0f , 1f)] private float _idleBlend = 0f;
        [SerializeField, Range(0f , 1f)] private float _walkBlend = 0.5f;
        [SerializeField, Range(0f , 1f)] private float _chaseBlend = 1f;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float _blendDampTime = 0.12f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.05f;

        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if ( _chaseAIController == null )
            {
                _chaseAIController = GetComponent<ChaseAIController>();
            }

            if ( _animator == null )
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if ( !ValidateReferences() )
            {
                enabled = false;

                return;
            }

            _animator.applyRootMotion = false;
            _animator.SetFloat(BLEND_PARAMETER_HASH , _idleBlend);
        }

        private void OnEnable()
        {
            if ( _animator == null )
            {
                return;
            }

            _animator.SetFloat(BLEND_PARAMETER_HASH , _idleBlend);
        }

        private void Update()
        {
            float targetBlend = GetTargetBlend();

            _animator.SetFloat(
                BLEND_PARAMETER_HASH ,
                targetBlend ,
                _blendDampTime ,
                Time.deltaTime);
        }

        private float GetTargetBlend()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.ATTACK )
            {
                return _chaseBlend;
            }

            if ( !IsMoving() )
            {
                return _idleBlend;
            }

            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _chaseBlend;
            }

            return _walkBlend;
        }

        private bool IsMoving()
        {
            if ( _agent == null ||
                 !_agent.enabled ||
                 !_agent.isOnNavMesh ||
                 _agent.isStopped )
            {
                return false;
            }

            float movementThresholdSqr = _movementThreshold * _movementThreshold;

            return _agent.velocity.sqrMagnitude > movementThresholdSqr;
        }

        private bool ValidateReferences()
        {
            if ( _chaseAIController == null )
            {
                Debug.LogError(
                    $"[{nameof(ChaseAIAnimator)}] ChaseAIController 참조가 없습니다." ,
                    this);

                return false;
            }

            if ( _animator == null )
            {
                Debug.LogError(
                    $"[{nameof(ChaseAIAnimator)}] Animator 참조가 없습니다." ,
                    this);

                return false;
            }

            if ( _animator.runtimeAnimatorController == null )
            {
                Debug.LogError(
                    $"[{nameof(ChaseAIAnimator)}] Animator Controller가 없습니다." ,
                    this);

                return false;
            }

            if ( !HasBlendParameter() )
            {
                Debug.LogError(
                    $"[{nameof(ChaseAIAnimator)}] Float 파라미터 " +
                    $"'{BLEND_PARAMETER_NAME}'가 없습니다." ,
                    this);

                return false;
            }

            return true;
        }

        private bool HasBlendParameter()
        {
            AnimatorControllerParameter[] parameters = _animator.parameters;

            foreach ( AnimatorControllerParameter parameter in parameters )
            {
                if ( parameter.nameHash == BLEND_PARAMETER_HASH &&
                     parameter.type == AnimatorControllerParameterType.Float )
                {
                    return true;
                }
            }

            return false;
        }

        private void Reset()
        {
            _chaseAIController = GetComponent<ChaseAIController>();
            _animator = GetComponentInChildren<Animator>(true);
        }
    }
}
