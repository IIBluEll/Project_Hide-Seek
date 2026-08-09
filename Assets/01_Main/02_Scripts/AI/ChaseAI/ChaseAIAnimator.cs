using System;
using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.AI
{
    public enum CHASE_AI_ANIMATION_ACTION
    {
        NONE,
        SUSPICION,
        LOOK_AROUND,
        INSPECT_HIDING_SPOT
    }

    public enum CHASE_AI_FOOT
    {
        LEFT,
        RIGHT
    }

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
        private const string ACTION_TYPE_PARAMETER_NAME = "ActionType";
        private const string ACTION_PROGRESS_PARAMETER_NAME = "ActionProgress";

        private static readonly int BLEND_PARAMETER_HASH =
            Animator.StringToHash(BLEND_PARAMETER_NAME);

        private static readonly int ANIMATION_SPEED_PARAMETER_HASH =
            Animator.StringToHash(ANIMATION_SPEED_PARAMETER_NAME);
        private static readonly int ACTION_TYPE_PARAMETER_HASH =
            Animator.StringToHash(ACTION_TYPE_PARAMETER_NAME);
        private static readonly int ACTION_PROGRESS_PARAMETER_HASH =
            Animator.StringToHash(ACTION_PROGRESS_PARAMETER_NAME);

        public event Action<CHASE_AI_ANIMATION_ACTION , CHASE_AI_ANIMATION_ACTION> AnimationActionChanged;
        public event Action<CHASE_AI_FOOT> FootstepActioned;
        public event Action SearchContactActioned;
        public event Action HidingSpotContactActioned;

        [Header("References")]
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private Animator _animator;

        [Header("Blend Values")]
        [SerializeField, Range(0f , 1f)] private float _idleBlend = 0f;
        [SerializeField, Range(0f , 1f)] private float _walkBlend = 0.5f;
        [SerializeField, Range(0f , 1f)] private float _evidenceApproachBlend = 0.75f;
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
        private CHASE_AI_ANIMATION_ACTION _currentAnimationAction;

        public CHASE_AI_ANIMATION_ACTION CurrentAnimationAction => _currentAnimationAction;

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

            EnsureAnimationEventRelay();

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
            CHASE_AI_ANIMATION_ACTION targetAnimationAction = GetTargetAnimationAction();

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

            UpdateAnimationAction(targetAnimationAction);
        }

        internal void NotifyFootstepActioned(CHASE_AI_FOOT foot)
        {
            FootstepActioned?.Invoke(foot);
        }

        internal void NotifySearchContactActioned()
        {
            SearchContactActioned?.Invoke();
        }

        internal void NotifyHidingSpotContactActioned()
        {
            HidingSpotContactActioned?.Invoke();
        }

        private CHASE_AI_ANIMATION_ACTION GetTargetAnimationAction()
        {
            if ( _chaseAIController.IsReactingToVisualSuspicion )
            {
                return CHASE_AI_ANIMATION_ACTION.SUSPICION;
            }

            return _chaseAIController.CurrentSearchAction switch
            {
                CHASE_AI_SEARCH_ACTION.CHECK_DIRECTION => CHASE_AI_ANIMATION_ACTION.LOOK_AROUND,
                CHASE_AI_SEARCH_ACTION.OBSERVE_AREA => CHASE_AI_ANIMATION_ACTION.LOOK_AROUND,
                CHASE_AI_SEARCH_ACTION.INSPECT_HIDING_SPOT => CHASE_AI_ANIMATION_ACTION.INSPECT_HIDING_SPOT,
                _ => CHASE_AI_ANIMATION_ACTION.NONE
            };
        }

        private void UpdateAnimationAction(CHASE_AI_ANIMATION_ACTION targetAnimationAction)
        {
            float actionProgress = targetAnimationAction == CHASE_AI_ANIMATION_ACTION.LOOK_AROUND ||
                targetAnimationAction == CHASE_AI_ANIMATION_ACTION.INSPECT_HIDING_SPOT
                ? _chaseAIController.SearchActionProgress
                : 0f;

            _animator.SetFloat(ACTION_PROGRESS_PARAMETER_HASH , actionProgress);

            if ( _currentAnimationAction == targetAnimationAction )
            {
                return;
            }

            CHASE_AI_ANIMATION_ACTION previousAnimationAction = _currentAnimationAction;
            _currentAnimationAction = targetAnimationAction;

            _animator.SetInteger(ACTION_TYPE_PARAMETER_HASH , (int)_currentAnimationAction);

            AnimationActionChanged?.Invoke(previousAnimationAction , _currentAnimationAction);
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

            float maximumBlend = GetMaximumBlend();

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

            float referenceSpeed = GetAnimationReferenceSpeed();

            float animationSpeed = movementSpeed / referenceSpeed;

            return Mathf.Clamp(
                animationSpeed ,
                _minimumAnimationSpeed ,
                _maximumAnimationSpeed);
        }

        private float GetMaximumBlend()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _chaseBlend;
            }

            return _chaseAIController.IsUsingEvidenceApproachSpeed
                ? _evidenceApproachBlend
                : _walkBlend;
        }

        private float GetAnimationReferenceSpeed()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _chaseAnimationReferenceSpeed;
            }

            if ( !_chaseAIController.IsUsingEvidenceApproachSpeed )
            {
                return _walkAnimationReferenceSpeed;
            }

            float evidenceBlendRatio = Mathf.InverseLerp(
                _walkBlend ,
                _chaseBlend ,
                _evidenceApproachBlend);

            return Mathf.Lerp(
                _walkAnimationReferenceSpeed ,
                _chaseAnimationReferenceSpeed ,
                evidenceBlendRatio);
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
            _currentAnimationAction = CHASE_AI_ANIMATION_ACTION.NONE;
            _animator.SetFloat(BLEND_PARAMETER_HASH , _idleBlend);
            _animator.SetFloat(ANIMATION_SPEED_PARAMETER_HASH , 1f);
            _animator.SetInteger(ACTION_TYPE_PARAMETER_HASH , (int)CHASE_AI_ANIMATION_ACTION.NONE);
            _animator.SetFloat(ACTION_PROGRESS_PARAMETER_HASH , 0f);
        }

        private void EnsureAnimationEventRelay()
        {
            ChaseAIAnimationEventRelay eventRelay =
                _animator.GetComponent<ChaseAIAnimationEventRelay>();

            if ( eventRelay == null )
            {
                eventRelay = _animator.gameObject.AddComponent<ChaseAIAnimationEventRelay>();
            }

            eventRelay.Initialize(this);
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

            if ( !HasParameter(
                    BLEND_PARAMETER_HASH ,
                    BLEND_PARAMETER_NAME ,
                    AnimatorControllerParameterType.Float) )
            {
                return false;
            }

            if ( !HasParameter(
                    ANIMATION_SPEED_PARAMETER_HASH ,
                    ANIMATION_SPEED_PARAMETER_NAME ,
                    AnimatorControllerParameterType.Float) )
            {
                return false;
            }

            if ( !HasParameter(
                    ACTION_TYPE_PARAMETER_HASH ,
                    ACTION_TYPE_PARAMETER_NAME ,
                    AnimatorControllerParameterType.Int) )
            {
                return false;
            }

            if ( !HasParameter(
                    ACTION_PROGRESS_PARAMETER_HASH ,
                    ACTION_PROGRESS_PARAMETER_NAME ,
                    AnimatorControllerParameterType.Float) )
            {
                return false;
            }

            return true;
        }

        private bool HasParameter(
            int parameterHash ,
            string parameterName ,
            AnimatorControllerParameterType parameterType)
        {
            AnimatorControllerParameter[] parameters = _animator.parameters;

            foreach ( AnimatorControllerParameter parameter in parameters )
            {
                if ( parameter.nameHash == parameterHash &&
                     parameter.type == parameterType )
                {
                    return true;
                }
            }

            Debug.LogError(
                $"[{nameof(ChaseAIAnimator)}] Animator 파라미터 " +
                $"'{parameterName}'가 없거나 타입이 올바르지 않습니다." ,
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
