using System;
using UnityEngine;

namespace HideSeek.AI
{
    public static class NoiseProvider
    {
        public static event Action<NoiseData> NoiseEmitted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            NoiseEmitted = null;
        }

        public static bool Emit(NoiseData noiseData)
        {
            if ( !noiseData.IsValid() )
            {
                Debug.LogWarning("[NoiseProvider] 유효하지 않은 소음이 무시됐습니다.");

                return false;
            }

            NoiseEmitted?.Invoke(noiseData);

            return true;
        }
    }
}
