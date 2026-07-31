using UnityEngine;

namespace HideSeek.AI
{
    // 발소리, 디코이, 문, 발전기 등이 사용할 소음 발생 컴포넌
    public sealed class NoiseEmitter : MonoBehaviour
    {
        [SerializeField]
        private NOISE_TYPE _noiseType = NOISE_TYPE.DECOY;

        [SerializeField, Min(0.1f)] private float _radius = 10f;
        [SerializeField, Min(0.01f)] private float _intensity = 1f;

        [ContextMenu("Emit Test Noise")]
        public void EmitNoise()
        {
            EmitNoiseAt(transform.position);
        }

        // 발전기처럼 다른 오브젝트가 알려준 위치에서 소음을 낼 때 사용한다.
        public void EmitNoiseAt(Vector3 position)
        {
            NoiseData noiseData = new NoiseData(
                position,
                _radius,
                _intensity,
                _noiseType,
                Time.time,
                gameObject);

            bool wasEmitted = NoiseProvider.Emit(noiseData);

            if ( !wasEmitted )
            {
                return;
            }

            Debug.Log(
                $"[NoiseEmitter] {_noiseType} 소음 발생, " +
                $"반경: {_radius:F1}, 강도: {_intensity:F2}" ,
                this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(
                1f ,
                0.5f ,
                0f ,
                0.35f);

            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
