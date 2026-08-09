using UnityEngine;
using UnityEngine.AI;

namespace HideSeek.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AutomaticDoorSensor : MonoBehaviour
    {
        private const int OVERLAP_BUFFER_SIZE = 16;

        [Header("Door")]
        [SerializeField] private SlidingDoor _door;

        [Header("Detection")]
        [SerializeField] private Vector3 _detectionCenter;
        [SerializeField] private Vector3 _detectionSize = new(3f , 3f , 4f);
        [SerializeField] private LayerMask _actorMask = ~0;
        [SerializeField, Min(0.02f)] private float _scanInterval = 0.1f;
        [SerializeField, Min(0f)] private float _closeDelay = 1.5f;

        private readonly Collider[] OVERLAP_BUFFER = new Collider[OVERLAP_BUFFER_SIZE];

        private float _scanTimer;
        private float _emptyTimer;
        [SerializeField] private bool _hasOccupant;
        private bool _wasOpenRequested;

        public bool HasOccupant => _hasOccupant;

        private void Awake()
        {
            if ( _door == null )
            {
                _door = GetComponentInChildren<SlidingDoor>();
            }

            if ( _door != null )
            {
                return;
            }

            Debug.LogError($"[{nameof(AutomaticDoorSensor)}] 제어할 문이 없습니다." , this);
            enabled = false;
        }

        private void OnEnable()
        {
            _scanTimer = _scanInterval;
            _emptyTimer = 0f;
            _hasOccupant = false;
            _wasOpenRequested = _door != null && _door.IsOpenRequested;
        }

        private void Update()
        {
            _scanTimer += Time.deltaTime;

            if ( _scanTimer >= _scanInterval )
            {
                _scanTimer %= _scanInterval;
                _hasOccupant = DetectOccupant();

                if ( _hasOccupant )
                {
                    _door.Open();
                }
            }

            if ( _hasOccupant )
            {
                _emptyTimer = 0f;
                _wasOpenRequested = true;

                return;
            }

            bool isOpenRequested = _door.IsOpenRequested;

            if ( isOpenRequested && !_wasOpenRequested )
            {
                _emptyTimer = 0f;
            }

            _wasOpenRequested = isOpenRequested;

            if ( !isOpenRequested )
            {
                _emptyTimer = 0f;

                return;
            }

            _emptyTimer += Time.deltaTime;

            if ( _emptyTimer < _closeDelay )
            {
                return;
            }

            if ( _door.Close() )
            {
                _wasOpenRequested = false;
            }
        }

        private bool DetectOccupant()
        {
            Vector3 worldCenter = transform.TransformPoint(_detectionCenter);
            Vector3 lossyScale = transform.lossyScale;
            Vector3 worldHalfExtents = new(
                Mathf.Abs(_detectionSize.x * lossyScale.x) * 0.5f ,
                Mathf.Abs(_detectionSize.y * lossyScale.y) * 0.5f ,
                Mathf.Abs(_detectionSize.z * lossyScale.z) * 0.5f);

            int overlapCount = Physics.OverlapBoxNonAlloc(
                worldCenter ,
                worldHalfExtents ,
                OVERLAP_BUFFER ,
                transform.rotation ,
                _actorMask ,
                QueryTriggerInteraction.Ignore);

            for ( int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++ )
            {
                Collider overlapCollider = OVERLAP_BUFFER[ overlapIndex ];

                if ( IsDoorUser(overlapCollider) )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDoorUser(Collider overlapCollider)
        {
            if ( overlapCollider == null )
            {
                return false;
            }

            CharacterController characterController =
                overlapCollider.GetComponentInParent<CharacterController>();

            if ( characterController != null && characterController.enabled )
            {
                return true;
            }

            NavMeshAgent navMeshAgent = overlapCollider.GetComponentInParent<NavMeshAgent>();

            return navMeshAgent != null && navMeshAgent.enabled;
        }

        private void OnValidate()
        {
            _detectionSize = new Vector3(
                Mathf.Max(0.01f , _detectionSize.x) ,
                Mathf.Max(0.01f , _detectionSize.y) ,
                Mathf.Max(0.01f , _detectionSize.z));
            _scanInterval = Mathf.Max(0.02f , _scanInterval);
            _closeDelay = Mathf.Max(0f , _closeDelay);
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = Matrix4x4.TRS(
                transform.position ,
                transform.rotation ,
                transform.lossyScale);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_detectionCenter , _detectionSize);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
