using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.Generators
{
    /// <summary>
    /// QTE 1회 동안만 열려 있다. 결과 피드백을 보여준 뒤 스스로 닫는다.
    ///
    /// 인스펙터 설정 기준
    /// - <see cref="_qteZoneImg"/>는 Image Type을 Filled, Fill Method를 Radial 360,
    ///   Fill Origin을 Top, Clockwise를 켠 상태로 사용한다. fillAmount와 회전으로 성공 구간을 표현한다.
    /// - <see cref="_qteRingImg"/>는 색만 바꾸므로 Filled 설정이 필요 없다.
    /// - <see cref="_qteIndicatorRectTrans"/>는 원 중심을 피벗으로 두고 위쪽을 가리키도록 배치한다.
    /// - Zone과 Indicator는 크기와 중심이 같아야 회전 기준이 맞는다.
    /// </summary>
    public sealed class GeneratorQte_view : AView
    {
        [SerializeField] private Image _qteRingImg;
        [SerializeField] private Image _qteZoneImg;
        [SerializeField] private RectTransform _qteIndicatorRectTrans;

        [Header("결과 피드백")]
        [SerializeField] private Color _ringDefaultColor = new Color(0.8f , 0.8f , 0.8f , 1f);
        [SerializeField] private Color _ringSuccessColor = new Color(0.4f , 1f , 0.5f , 1f);
        [SerializeField] private Color _ringFailureColor = new Color(1f , 0.25f , 0.25f , 1f);
        [SerializeField , Min(0f)] private float _resultFeedbackDuration = 0.25f; // 판정 후 게이지가 남아 있는 시간(초)

        private float _resultFeedbackRemain;

        public override void Clear()
        {
            base.Clear();

            _resultFeedbackRemain = 0f;
            ApplyRingColor(_ringDefaultColor);
            SetIndicator(0f);
        }

        public void ShowQte(float zoneStart01, float zoneEnd01)
        {
            _resultFeedbackRemain = 0f;
            ApplyRingColor(_ringDefaultColor);

            if (_qteZoneImg != null)
            {
                _qteZoneImg.fillAmount = Mathf.Clamp01(zoneEnd01 - zoneStart01);
                _qteZoneImg.rectTransform.localRotation = Quaternion.Euler(0f , 0f , -zoneStart01 * 360f);
            }

            SetIndicator(0f);
        }

        public void SetIndicator(float indicator01)
        {
            if (_qteIndicatorRectTrans == null)
            {
                return;
            }

            _qteIndicatorRectTrans.localRotation = Quaternion.Euler(0f , 0f , -indicator01 * 360f);
        }

        /// <summary>
        /// 판정 결과를 색으로 알리고, 표시 시간이 지나면 스스로 닫는다.
        /// </summary>
        public void PlayResultFeedback(QTE_RESULT result)
        {
            ApplyRingColor(result == QTE_RESULT.SUCCESS ? _ringSuccessColor : _ringFailureColor);

            if (_resultFeedbackDuration <= 0f)
            {
                Close();
                return;
            }

            _resultFeedbackRemain = _resultFeedbackDuration;
        }

        private void Update()
        {
            if (_resultFeedbackRemain <= 0f)
            {
                return;
            }

            _resultFeedbackRemain -= Time.deltaTime;
            if (_resultFeedbackRemain > 0f)
            {
                return;
            }

            Close();
        }

        private void ApplyRingColor(Color color)
        {
            if (_qteRingImg == null)
            {
                return;
            }

            _qteRingImg.color = color;
        }
    }
}
