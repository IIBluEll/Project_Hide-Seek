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

        public AI_SEARCH_POINT_TYPE PointType => _pointType;
        public Vector3 Position => transform.position;

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
