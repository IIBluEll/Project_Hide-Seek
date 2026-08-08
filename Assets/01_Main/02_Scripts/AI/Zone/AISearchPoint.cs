using UnityEngine;

namespace HideSeek.AI
{
    public enum AI_SEARCH_POINT_TYPE
    {
        COVERAGE,
        HIDING_SPOT
    }

    [DisallowMultipleComponent]
    public sealed class AISearchPoint : MonoBehaviour
    {
        [SerializeField] private AI_SEARCH_POINT_TYPE _pointType = AI_SEARCH_POINT_TYPE.COVERAGE;
        [SerializeField] private AIHidingSpot _hidingSpot;

        public AI_SEARCH_POINT_TYPE PointType => _pointType;
        public AIHidingSpot HidingSpot => _hidingSpot;
        public Vector3 Position => _pointType == AI_SEARCH_POINT_TYPE.HIDING_SPOT && _hidingSpot != null
            ? _hidingSpot.InspectionPosition
            : transform.position;

        private void OnValidate()
        {
            if ( _pointType == AI_SEARCH_POINT_TYPE.HIDING_SPOT && _hidingSpot == null )
            {
                TryGetComponent(out _hidingSpot);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _pointType == AI_SEARCH_POINT_TYPE.COVERAGE
                ? new Color(0f , 0.85f , 1f , 0.9f)
                : new Color(1f , 0.25f , 0.7f , 0.9f);

            Gizmos.DrawWireSphere(transform.position , 0.35f);
            Gizmos.DrawLine(transform.position , transform.position + Vector3.up * 0.8f);
        }
    }
}
