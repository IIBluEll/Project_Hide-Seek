using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class MasterAIProvider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MasterAIConfig _config;
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private Transform _playerTrans;
        [SerializeField] private Transform _retreatPointTrans;
        [SerializeField] private List<AIWorldZone> _zones = new();

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = true;
        [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

        private MasterAIDirector _director;
        private MasterAIZoneSelector _zoneSelector;
        private MasterAIHintGenerator _hintGenerator;
        private AIWorldZone _currentPlayerZone;
        private AIWorldZone _targetZone;
        private MasterAIHint _currentHint;

        private CHASE_AI_STATE _previousChaseAIState;

        private float _debugLogTimer;
        private bool _isInitialized;
        private bool _hasCurrentHint;

        public MASTER_AI_STATE CurrentState => _director != null ? _director.CurrentState : MASTER_AI_STATE.DORMANT;
        public AIWorldZone CurrentPlayerZone => _currentPlayerZone;
        public AIWorldZone TargetZone => _targetZone;

        public float GlobalStress => _director != null ? _director.GlobalStress : 0f;
        public float GlobalStressRatio => _director != null ? _director.GlobalStressRatio : 0f;
        public bool HasCurrentHint => _hasCurrentHint && _currentHint.IsValid(Time.time);

        private void Start()
        {
            if ( !ValidateReferences() )
            {
                return;
            }

            try
            {
                _zoneSelector = new MasterAIZoneSelector(_zones , _config);
            }
            catch ( ArgumentException exception )
            {
                Debug.LogError($"[MasterAIProvider] Zone 초기화 실패: {exception.Message}" , this);

                return;
            }

            _director = new MasterAIDirector(_config);
            _hintGenerator = new MasterAIHintGenerator(_config);
            _previousChaseAIState = _chaseAIController.CurrentState;
            _isInitialized = true;

            UpdatePlayerZone();

            Debug.Log("[MasterAIProvider] 초기화 완료: Director=DORMANT" , this);
        }

        private void Update()
        {
            if ( !_isInitialized )
            {
                return;
            }

            UpdatePlayerZone();
            UpdateDirectorHint();

            float distanceToPlayer = Vector3.Distance(_chaseAIController.transform.position , _playerTrans.position);
            MASTER_AI_COMMAND command = _director.Tick(Time.deltaTime , _chaseAIController.CurrentState , distanceToPlayer);

            ProcessCommand(command);
            UpdateChaseStateNotification();
            UpdateDebugLog(Time.deltaTime , distanceToPlayer);
        }

        private void OnEnable()
        {
            if ( _chaseAIController == null )
            {
                return;
            }

            _chaseAIController.RetreatFailed -= OnChaseAIRetreatFailed;
            _chaseAIController.RetreatFailed += OnChaseAIRetreatFailed;
        }

        private void OnDisable()
        {
            if ( _chaseAIController != null )
            {
                _chaseAIController.RetreatFailed -= OnChaseAIRetreatFailed;
            }
        }

        private void UpdatePlayerZone()
        {
            _zoneSelector.TryGetContainingZone(_playerTrans.position , out AIWorldZone containingZone);

            if ( containingZone == _currentPlayerZone )
            {
                return;
            }

            _currentPlayerZone = containingZone;

            if ( _currentPlayerZone == null )
            {
                Debug.LogWarning("[MasterAIProvider] 플레이어가 어떤 Zone에도 포함되지 않습니다." , this);

                return;
            }

            Debug.Log($"[MasterAIProvider] Player Zone 변경: ID={_currentPlayerZone.ZoneId}, Name={_currentPlayerZone.DisplayName}" , this);
        }

        private void OnChaseAIRetreatFailed()
        {
            if ( _director == null )
            {
                return;
            }

            _director.NotifyRetreatFailed();

            Debug.LogWarning($"[MasterAIProvider] Chase AI 이탈 실패 통보 수신: {_config.RetreatRetryDelay:F1}초 후 재시도" , this);
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

        private bool TrySelectTargetZone()
        {
            _targetZone = null;

            bool wasSelected = _zoneSelector.TrySelectTargetZone(_currentPlayerZone , _director.GlobalStressRatio , out AIWorldZone selectedZone , out MASTER_AI_ZONE_RELATION relation);

            if ( !wasSelected )
            {
                Debug.LogWarning("[MasterAIProvider] 목표 Zone을 선택할 수 없습니다." , this);

                return false;
            }

            _targetZone = selectedZone;

            Debug.Log($"[MasterAIProvider] Target Zone 선택: ID={_targetZone.ZoneId}, Name={_targetZone.DisplayName}, Relation={relation}, Stress={_director.GlobalStressRatio:F2}" , this);

            return true;
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

            if ( TrySelectTargetZone() )
            {
                GenerateDirectorHint();
            }
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

                ClearDirectorHint();
                _targetZone = null;

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

            string currentZoneName = _currentPlayerZone != null ? _currentPlayerZone.DisplayName : "NONE";
            string targetZoneName = _targetZone != null ? _targetZone.DisplayName : "NONE";

            Debug.Log($"[MasterAIProvider] Director={_director.CurrentState}, GlobalStress={_director.GlobalStress:F1}/{_config.MaximumGlobalStress:F1}, Chase={_chaseAIController.CurrentState}, RetreatPending={_chaseAIController.IsRetreatPending}, PlayerZone={currentZoneName}, TargetZone={targetZoneName}, Distance={distanceToPlayer:F1}" , this);
        }

        private void GenerateDirectorHint()
        {
            if ( _targetZone == null || _hintGenerator == null )
            {
                return;
            }

            bool wasCreated = _hintGenerator.TryCreateHint(
                _targetZone ,
                _director.GlobalStressRatio ,
                Time.time ,
                out MasterAIHint createdHint);

            if ( !wasCreated )
            {
                ClearDirectorHint();

                Debug.LogWarning($"[MasterAIProvider] Director Hint 생성 실패: Zone={_targetZone.DisplayName} 내부에서 NavMesh 위치를 찾지 못했습니다." , this);

                return;
            }

            _currentHint = createdHint;
            _hasCurrentHint = true;

            Debug.Log($"[MasterAIProvider] Director Hint 생성: Zone={_currentHint.TargetZoneId}, Anchor={_currentHint.SearchAnchorPosition}, Radius={_currentHint.SearchRadius:F1}, Urgency={_currentHint.Urgency:F2}, Duration={_currentHint.ExpireTime - Time.time:F1}" , this);
        }

        private void UpdateDirectorHint()
        {
            if ( !_hasCurrentHint || _currentHint.IsValid(Time.time) )
            {
                return;
            }

            Debug.Log($"[MasterAIProvider] Director Hint 만료: Zone={_currentHint.TargetZoneId}" , this);

            ClearDirectorHint();
        }

        private void ClearDirectorHint()
        {
            _currentHint = default;
            _hasCurrentHint = false;
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

            if ( _zones == null || _zones.Count == 0 )
            {
                Debug.LogError("[MasterAIProvider] Zone 목록이 없습니다." , this);

                return false;
            }

            return true;
        }
    }
}