using UnityEngine;

namespace HideSeek.AI
{
    public sealed class AIVentPoint : MonoBehaviour
    {
        [SerializeField] private AIWorldZone _zone;
        [SerializeField] private bool _isAvailable = true;

        public AIWorldZone Zone => _zone;
        public Vector3 Position => transform.position;
        public bool IsAvailable => _isAvailable && isActiveAndEnabled;

        private void OnDrawGizmos()
        {
            Gizmos.color = _isAvailable ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position , 0.5f);

            if ( _zone == null )
            {
                return;
            }

            Gizmos.DrawLine(transform.position , _zone.CenterPosition);
        }
    }
}
