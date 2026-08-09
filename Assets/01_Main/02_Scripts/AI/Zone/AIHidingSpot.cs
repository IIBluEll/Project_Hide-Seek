using UnityEngine;

namespace HideSeek.AI
{
    public enum AI_HIDING_SPOT_TYPE
    {
        DESK,
        CLOSET
    }

    [DisallowMultipleComponent]
    public sealed class AIHidingSpot : MonoBehaviour
    {
        private const float MINIMUM_OCCUPANCY_SIZE = 0.001f;

        [SerializeField] private AI_HIDING_SPOT_TYPE _hidingSpotType = AI_HIDING_SPOT_TYPE.DESK;
        [SerializeField] private Transform _inspectionTrans;
        [SerializeField] private Vector3 _occupancyCenterOffset = Vector3.up;
        [SerializeField] private Vector3 _occupancySize = new(2f , 2f , 2f);

        public AI_HIDING_SPOT_TYPE HidingSpotType => _hidingSpotType;
        public Vector3 InspectionPosition => _inspectionTrans != null
            ? _inspectionTrans.position
            : transform.position;

        public bool ContainsPlayer(
            Transform playerTrans ,
            global::IPlayerVisibilityState playerVisibilityState)
        {
            if ( playerTrans == null ||
                 playerVisibilityState == null ||
                 !playerVisibilityState.IsFullyHidden )
            {
                return false;
            }

            Vector3 localPlayerPosition = transform.InverseTransformPoint(playerTrans.position);
            Vector3 localOffset = localPlayerPosition - _occupancyCenterOffset;
            Vector3 halfOccupancySize = _occupancySize * 0.5f;

            return Mathf.Abs(localOffset.x) <= halfOccupancySize.x &&
                Mathf.Abs(localOffset.y) <= halfOccupancySize.y &&
                Mathf.Abs(localOffset.z) <= halfOccupancySize.z;
        }

        private Vector3 GetWorldOccupancySize()
        {
            Vector3 lossyScale = transform.lossyScale;

            return new Vector3(
                Mathf.Abs(_occupancySize.x * lossyScale.x) ,
                Mathf.Abs(_occupancySize.y * lossyScale.y) ,
                Mathf.Abs(_occupancySize.z * lossyScale.z));
        }

        private void OnValidate()
        {
            _occupancySize = new Vector3(
                Mathf.Max(MINIMUM_OCCUPANCY_SIZE , _occupancySize.x) ,
                Mathf.Max(MINIMUM_OCCUPANCY_SIZE , _occupancySize.y) ,
                Mathf.Max(MINIMUM_OCCUPANCY_SIZE , _occupancySize.z));
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 worldCenter = transform.TransformPoint(_occupancyCenterOffset);
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = new Color(1f , 0.15f , 0.65f , 0.9f);
            Gizmos.matrix = Matrix4x4.TRS(worldCenter , transform.rotation , Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero , GetWorldOccupancySize());
            Gizmos.matrix = previousMatrix;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(InspectionPosition , 0.2f);
        }
    }
}
