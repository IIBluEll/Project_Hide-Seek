using System;
using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// Unity 컴포넌트와 입력에 직접 의존하지 않으므로 단독으로 검증할 수 있다.
    /// </summary>
    public sealed class QteRunner
    {
        public event Action<QTE_RESULT> Finished;

        public bool IsActive { get; private set; }
        public float Indicator01 { get; private set; } // 0에서 1까지 한 방향으로 이동한다
        public float ZoneStart01 { get; private set; }
        public float ZoneEnd01 { get; private set; }

        private float _sweepDuration = 1f;

        /// <summary>
        /// 성공 구간은 매번 무작위 위치에 배치된다.
        /// </summary>
        public void Begin(float sweepDuration , float zoneSize01 , float zoneMinStart01)
        {
            _sweepDuration = Mathf.Max(0.01f , sweepDuration);

            float tZoneSize = Mathf.Clamp(zoneSize01 , 0.01f , 0.9f);
            float tMinStart = Mathf.Clamp(zoneMinStart01 , 0f , 1f - tZoneSize);

            ZoneStart01 = UnityEngine.Random.Range(tMinStart , 1f - tZoneSize);
            ZoneEnd01 = ZoneStart01 + tZoneSize;
            Indicator01 = 0f;
            IsActive = true;
        }

        /// <summary>
        /// 성공 구간 이전에 입력해도 실패하므로 연타로 통과할 수 없다.
        /// </summary>
        public void Tick(float deltaTime , bool isQteKeyDown)
        {
            if (IsActive == false)
            {
                return;
            }

            if (isQteKeyDown)
            {
                bool tIsInZone = Indicator01 >= ZoneStart01 && Indicator01 <= ZoneEnd01;
                Finish(tIsInZone ? QTE_RESULT.SUCCESS : QTE_RESULT.FAILURE);
                return;
            }

            Indicator01 += deltaTime / _sweepDuration;
            if (Indicator01 < 1f)
            {
                return;
            }

            // 입력하지 않고 성공 구간을 지나친 경우도 실패로 처리한다.
            Indicator01 = 1f;
            Finish(QTE_RESULT.FAILURE);
        }

        // 판정 없이 중단한다. 작업 취소나 수리 완료 시 사용한다.
        public void Cancel()
        {
            IsActive = false;
            Indicator01 = 0f;
        }

        private void Finish(QTE_RESULT result)
        {
            IsActive = false;
            Finished?.Invoke(result);
        }
    }
}
