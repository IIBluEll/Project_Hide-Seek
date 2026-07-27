using System;
using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 원형 게이지 QTE 1회의 진행과 판정을 담당한다.
    /// Unity 컴포넌트와 입력에 직접 의존하지 않으므로 단독으로 검증할 수 있다.
    /// </summary>
    public sealed class QteRunner
    {
        /// <summary>판정이 끝났을 때 결과와 함께 발생한다.</summary>
        public event Action<QTE_RESULT> Finished;

        /// <summary>현재 QTE가 진행 중인지 여부.</summary>
        public bool IsActive { get; private set; }

        /// <summary>인디케이터의 현재 위치. 0에서 1까지 한 방향으로 이동한다.</summary>
        public float Indicator01 { get; private set; }

        /// <summary>성공 구간 시작 지점.</summary>
        public float ZoneStart01 { get; private set; }

        /// <summary>성공 구간 종료 지점.</summary>
        public float ZoneEnd01 { get; private set; }

        private float _sweepDuration = 1f;

        /// <summary>
        /// QTE 1회를 시작한다. 성공 구간은 매번 무작위 위치에 배치된다.
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
        /// 인디케이터를 이동시키고 입력을 판정한다.
        /// 성공 구간 이전에 입력하면 실패하므로 연타로 통과할 수 없다.
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

        /// <summary>
        /// 판정 없이 QTE를 중단한다. 작업 취소나 수리 완료 시 사용한다.
        /// </summary>
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
