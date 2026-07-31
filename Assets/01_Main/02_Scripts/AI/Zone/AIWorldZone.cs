using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class AIWorldZone : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField, Min(0)] private int _zoneId;
        [SerializeField] private string _displayName;

        [Header("Bounds")]
        [SerializeField] private BoxCollider _boundsCollider;

        [Header("Connections")]
        [SerializeField] private List<AIWorldZone> _adjacentZones = new();

        [Header("Search Points")]
        [SerializeField] private List<AISearchPoint> _searchPoints = new();

        public int ZoneId => _zoneId;
        public string DisplayName => _displayName;
        public Vector3 CenterPosition => _boundsCollider != null ? _boundsCollider.transform.TransformPoint(_boundsCollider.center) : transform.position;
        public IReadOnlyList<AIWorldZone> AdjacentZones => _adjacentZones;
        public IReadOnlyList<AISearchPoint> SearchPoints => _searchPoints;

        public bool Contains(Vector3 worldPosition)
        {
            if ( _boundsCollider == null )
            {
                return false;
            }

            Vector3 localPosition = _boundsCollider.transform.InverseTransformPoint(worldPosition) - _boundsCollider.center;
            Vector3 halfSize = _boundsCollider.size * 0.5f;

            return Mathf.Abs(localPosition.x) <= halfSize.x && Mathf.Abs(localPosition.y) <= halfSize.y && Mathf.Abs(localPosition.z) <= halfSize.z;
        }

        public bool IsAdjacentTo(AIWorldZone otherZone)
        {
            return otherZone != null && _adjacentZones.Contains(otherZone);
        }

        public Vector3 GetRandomWorldPosition()
        {
            if ( _boundsCollider == null )
            {
                return transform.position;
            }

            Vector3 localCenter = _boundsCollider.center;
            Vector3 halfSize = _boundsCollider.size * 0.5f;

            Vector3 localPosition = new Vector3(Random.Range(localCenter.x - halfSize.x , localCenter.x + halfSize.x) , localCenter.y , Random.Range(localCenter.z - halfSize.z , localCenter.z + halfSize.z));

            return _boundsCollider.transform.TransformPoint(localPosition);
        }

        public void CollectSearchPoints(
            AI_SEARCH_POINT_TYPE pointType ,
            List<AISearchPoint> searchPointBuffer)
        {
            if ( searchPointBuffer == null )
            {
                return;
            }

            if ( _searchPoints == null )
            {
                return;
            }

            for ( int pointIndex = 0; pointIndex < _searchPoints.Count; pointIndex++ )
            {
                AISearchPoint searchPoint = _searchPoints[ pointIndex ];

                if ( searchPoint != null && searchPoint.PointType == pointType )
                {
                    searchPointBuffer.Add(searchPoint);
                }
            }
        }

        [ContextMenu("Search Points/Collect From Children")]
        private void CollectSearchPointsFromChildren()
        {
            _searchPoints ??= new List<AISearchPoint>();
            _searchPoints.Clear();
            _searchPoints.AddRange(GetComponentsInChildren<AISearchPoint>(true));
        }

        private void Reset()
        {
            _boundsCollider = GetComponent<BoxCollider>();
            _boundsCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            if ( _boundsCollider == null )
            {
                _boundsCollider = GetComponent<BoxCollider>();
            }

            if ( _boundsCollider != null )
            {
                _boundsCollider.isTrigger = true;
            }

            _adjacentZones.RemoveAll(zone => zone == null || zone == this);

            _searchPoints ??= new List<AISearchPoint>();
            _searchPoints.RemoveAll(searchPoint => searchPoint == null);

            for ( int zoneIndex = _adjacentZones.Count - 1; zoneIndex >= 0; zoneIndex-- )
            {
                if ( _adjacentZones.IndexOf(_adjacentZones[ zoneIndex ]) != zoneIndex )
                {
                    _adjacentZones.RemoveAt(zoneIndex);
                }
            }

            for ( int pointIndex = _searchPoints.Count - 1; pointIndex >= 0; pointIndex-- )
            {
                if ( _searchPoints.IndexOf(_searchPoints[ pointIndex ]) != pointIndex )
                {
                    _searchPoints.RemoveAt(pointIndex);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if ( _boundsCollider == null )
            {
                return;
            }

            Gizmos.color = new Color(0f , 0.7f , 1f , 0.7f);
            Gizmos.matrix = _boundsCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_boundsCollider.center , _boundsCollider.size);

            Gizmos.matrix = Matrix4x4.identity;

            for ( int pointIndex = 0; pointIndex < _searchPoints.Count; pointIndex++ )
            {
                AISearchPoint searchPoint = _searchPoints[ pointIndex ];

                if ( searchPoint == null )
                {
                    continue;
                }

                Gizmos.color = searchPoint.PointType == AI_SEARCH_POINT_TYPE.COVERAGE
                    ? new Color(0f , 0.85f , 1f , 0.35f)
                    : new Color(1f , 0.25f , 0.7f , 0.35f);

                Gizmos.DrawLine(CenterPosition , searchPoint.Position);
            }
        }
    }
}
