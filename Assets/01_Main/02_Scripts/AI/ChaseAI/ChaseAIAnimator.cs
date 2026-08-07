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
        private const string ANIMATION_SPEED_PARAMETER_NAME = "AnimationSpeed";

        private static readonly int BLEND_PARAMETER_HASH =
            Animator.StringToHash(BLEND_PARAMETER_NAME);

        private static readonly int ANIMATION_SPEED_PARAMETER_HASH =
            Animator.StringToHash(ANIMATION_SPEED_PARAMETER_NAME);

        [Header("References")]
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private Animator _animator;

        [Header("Blend Values")]
        [SerializeField, Range(0f , 1f)] private float _idleBlend = 0f;
        [SerializeField, Range(0f , 1f)] private float _walkBlend = 0.5f;
        [SerializeField, Range(0f , 1f)] private float _chaseBlend = 1f;

        [Header("Animation Reference Speed")]
        [Tooltip("걷기 애니메이션이 1배속일 때 자연스럽게 보이는 월드 이동속도입니다.")]
        [SerializeField, Min(0.1f)] private float _walkAnimationReferenceSpeed = 4f;

        [Tooltip("달리기 애니메이션이 1배속일 때 자연스럽게 보이는 월드 이동속도입니다.")]
        [SerializeField, Min(0.1f)] private float _chaseAnimationReferenceSpeed = 6f;

        [SerializeField, Min(0.1f)] private float _minimumAnimationSpeed = 0.75f;
        [SerializeField, Min(0.1f)] private float _maximumAnimationSpeed = 1.5f;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float _blendDampTime = 0.12f;
        [SerializeField, Min(0f)] private float _animationSpeedDampTime = 0.1f;
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

            ResetAnimatorParameters();
        }

        private void OnEnable()
        {
            if ( _animator == null )
            {
                return;
            }

            ResetAnimatorParameters();
        }

        private void Update()
        {
            float movementSpeed = GetMovementSpeed();
            float targetBlend = GetTargetBlend(movementSpeed);
            float targetAnimationSpeed = GetTargetAnimationSpeed(movementSpeed);

            _animator.SetFloat(
                BLEND_PARAMETER_HASH ,
                targetBlend ,
                _blendDampTime ,
                Time.deltaTime);

            _animator.SetFloat(
                ANIMATION_SPEED_PARAMETER_HASH ,
                targetAnimationSpeed ,
                _animationSpeedDampTime ,
                Time.deltaTime);
        }

        private float GetTargetBlend(float movementSpeed)
        {
            if ( movementSpeed <= _movementThreshold )
            {
                return _idleBlend;
            }

            float maximumMovementSpeed =
                Mathf.Max(_agent.speed , _movementThreshold);

            float movementRatio =
                Mathf.Clamp01(movementSpeed / maximumMovementSpeed);

            float maximumBlend =
                _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE
                    ? _chaseBlend
                    : _walkBlend;

            return Mathf.Lerp(
                _idleBlend ,
                maximumBlend ,
                movementRatio);
        }

        private float GetTargetAnimationSpeed(float movementSpeed)
        {
            if ( movementSpeed <= _movementThreshold )
            {
                return 1f;
            }

            float referenceSpeed =
                _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE
                    ? _chaseAnimationReferenceSpeed
                    : _walkAnimationReferenceSpeed;

            float animationSpeed = movementSpeed / referenceSpeed;

            return Mathf.Clamp(
                animationSpeed ,
                _minimumAnimationSpeed ,
                _maximumAnimationSpeed);
        }

        private float GetMovementSpeed()
        {
            if ( _agent == null ||
                 !_agent.enabled ||
                 !_agent.isOnNavMesh ||
                 _agent.isStopped )
            {
                return 0f;
            }

            Vector3 planarVelocity = _agent.velocity;
            planarVelocity.y = 0f;

            return planarVelocity.magnitude;
        }

        private void ResetAnimatorParameters()
        {
            _animator.SetFloat(BLEND_PARAMETER_HASH , _idleBlend);
            _animator.SetFloat(ANIMATION_SPEED_PARAMETER_HASH , 1f);
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

            if ( !HasFloatParameter(
                    BLEND_PARAMETER_HASH ,
                    BLEND_PARAMETER_NAME) )
            {
                return false;
            }

            if ( !HasFloatParameter(
                    ANIMATION_SPEED_PARAMETER_HASH ,
                    ANIMATION_SPEED_PARAMETER_NAME) )
            {
                return false;
            }

            return true;
        }

        private bool HasFloatParameter(
            int parameterHash ,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = _animator.parameters;

            foreach ( AnimatorControllerParameter parameter in parameters )
            {
                if ( parameter.nameHash == parameterHash &&
                     parameter.type == AnimatorControllerParameterType.Float )
                {
                    return true;
                }
            }

            Debug.LogError(
                $"[{nameof(ChaseAIAnimator)}] Float 파라미터 " +
                $"'{parameterName}'가 없습니다." ,
                this);

            return false;
        }

        private void Reset()
        {
            _chaseAIController = GetComponent<ChaseAIController>();
            _animator = GetComponentInChildren<Animator>(true);
        }
    }
}
