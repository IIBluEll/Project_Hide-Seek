using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIProvider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MasterAIConfig _config;
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private Transform _playerTrans;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = true;
        [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

        [SerializeField] private Transform _retreatPointTrans;

        private MasterAIDirector _director;

        private float _debugLogTimer;
        private bool _isInitialized;

        private CHASE_AI_STATE _previousChaseAIState;

        public MASTER_AI_STATE CurrentState => _director != null ? _director.CurrentState : MASTER_AI_STATE.DORMANT;
        public float GlobalStress => _director != null ? _director.GlobalStress : 0f;
        public float GlobalStressRatio => _director != null ? _director.GlobalStressRatio : 0f;

        private void Start()
        {
            if ( !ValidateReferences() )
            {
                return;
            }

            _director = new MasterAIDirector(_config);
            _previousChaseAIState = _chaseAIController.CurrentState;
            _isInitialized = true;

            Debug.Log("[MasterAIProvider] 초기화 완료: Director=DORMANT" , this);
        }

        private void Update()
        {
            if ( !_isInitialized )
            {
                return;
            }

            float distanceToPlayer = Vector3.Distance(_chaseAIController.transform.position , _playerTrans.position);
            MASTER_AI_COMMAND command = _director.Tick(Time.deltaTime , _chaseAIController.CurrentState , distanceToPlayer);

            ProcessCommand(command);
            UpdateChaseStateNotification();
            UpdateDebugLog(Time.deltaTime , distanceToPlayer);
        }

        private void ProcessCommand(MASTER_AI_COMMAND command)
        {
            switch ( command )
            {
                case MASTER_AI_COMMAND.ACTIVATE:
                    ProcessActivationCommand();
                    break;

                case MASTER_AI_COMMAND.RETREAT:
                    ProcessRetreatCommand();
                    break;
            }
        }

        private void ProcessActivationCommand()
        {
            bool wasActivated = _chaseAIController.RequestActivation();

            if ( !wasActivated )
            {
                _director.NotifyChaseAIDormant();

                Debug.LogWarning("[MasterAIProvider] Chase AI 출현 요청에 실패했습니다." , this);

                return;
            }

            Debug.Log("[MasterAIProvider] Chase AI 출현 요청 성공" , this);
        }

        private void ProcessRetreatCommand()
        {
            bool wasAccepted = _chaseAIController.RequestRetreat(_retreatPointTrans.position);

            if ( !wasAccepted )
            {
                Debug.LogWarning("[MasterAIProvider] Chase AI 이탈 요청이 거부되었습니다." , this);

                return;
            }

            Debug.Log($"[MasterAIProvider] Chase AI 이탈 요청 전달 성공: State={_chaseAIController.CurrentState}, RetreatPending={_chaseAIController.IsRetreatPending}" , this);
        }

        private void UpdateChaseStateNotification()
        {
            CHASE_AI_STATE currentState = _chaseAIController.CurrentState;

            if ( _previousChaseAIState != CHASE_AI_STATE.DORMANT && currentState == CHASE_AI_STATE.DORMANT )
            {
                _director.NotifyChaseAIDormant();

                Debug.Log("[MasterAIProvider] Chase AI 이탈 완료: Director=DORMANT" , this);
            }

            _previousChaseAIState = currentState;
        }

        private void UpdateDebugLog(float deltaTime , float distanceToPlayer)
        {
            if ( !_enableDebugLog )
            {
                return;
            }

            _debugLogTimer -= deltaTime;

            if ( _debugLogTimer > 0f )
            {
                return;
            }

            _debugLogTimer = _debugLogInterval;

            Debug.Log($"[MasterAIProvider] Director={_director.CurrentState}, GlobalStress={_director.GlobalStress:F1}/{_config.MaximumGlobalStress:F1}, Chase={_chaseAIController.CurrentState}, RetreatPending={_chaseAIController.IsRetreatPending}, Distance={distanceToPlayer:F1}" , this);
        }

        private bool ValidateReferences()
        {
            if ( _config == null )
            {
                Debug.LogError("[MasterAIProvider] MasterAIConfig가 없습니다." , this);

                return false;
            }

            if ( _chaseAIController == null )
            {
                Debug.LogError("[MasterAIProvider] ChaseAIController가 없습니다." , this);

                return false;
            }

            if ( _playerTrans == null )
            {
                Debug.LogError("[MasterAIProvider] Player Transform이 없습니다." , this);

                return false;
            }

            if ( _retreatPointTrans == null )
            {
                Debug.LogError("[MasterAIProvider] Retreat Point Transform이 없습니다." , this);

                return false;
            }

            return true;
        }
    }
}