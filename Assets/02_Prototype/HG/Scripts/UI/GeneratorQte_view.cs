using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 수리 진행도와 원형 QTE 게이지를 표시한다.
    /// AGENTS.md 3.3에 따라 표시만 담당하며 판정과 진행 규칙은 가지지 않는다.
    ///
    /// 인스펙터 설정 기준
    /// - <see cref="_qteRootObj"/>는 이 스크립트가 붙은 오브젝트가 아니라 자식 오브젝트여야 한다.
    ///   자기 자신을 지정하면 게이지를 숨길 때 Update가 멈춰 결과 피드백이 동작하지 않는다.
    /// - <see cref="_qteZoneImg"/>는 Image Type을 Filled, Fill Method를 Radial 360,
    ///   Fill Origin을 Top, Clockwise를 켠 상태로 사용한다. fillAmount와 회전으로 성공 구간을 표현한다.
    /// - <see cref="_qteRingImg"/>는 색만 변경하므로 Filled 설정이 필요 없다.
    /// - <see cref="_qteIndicatorRectTrans"/>는 원 중심을 피벗으로 두고 위쪽을 가리키도록 배치한다.
    /// - <see cref="_progressImg"/>는 Image Type을 Filled로 두어야 fillAmount가 반영된다.
    /// - Zone과 Indicator는 크기와 중심이 같아야 회전 기준이 맞는다.
    /// </summary>
    public sealed class GeneratorQte_view : AView
    {
        [Header("수리 진행도")]
        [SerializeField] private Image _progressImg;
        [SerializeField] private TMP_Text _progressTxt;

        [Header("QTE")]
        [SerializeField] private GameObject _qteRootObj;
        [SerializeField] private Image _qteRingImg;
        [SerializeField] private Image _qteZoneImg;
        [SerializeField] private RectTransform _qteIndicatorRectTrans;
        [SerializeField] private TMP_Text _qteKeyTxt;

        [Header("결과 피드백")]
        [SerializeField] private Color _ringDefaultColor = new Color(0.8f , 0.8f , 0.8f , 1f);
        [SerializeField] private Color _ringSuccessColor = new Color(0.4f , 1f , 0.5f , 1f);
        [SerializeField] private Color _ringFailureColor = new Color(1f , 0.25f , 0.25f , 1f);
        [SerializeField , Min(0f)] private float _resultFeedbackDuration = 0.25f;

        private float _resultFeedbackRemain;

        public override void Clear()
        {
            base.Clear();

            SetProgress(0f);
            _resultFeedbackRemain = 0f;
            ApplyRingColor(_ringDefaultColor);
            HideQte();
        }

        /// <summary>수리 진행도를 갱신한다.</summary>
        public void SetProgress(float progress01)
        {
            if (_progressImg != null)
            {
                _progressImg.fillAmount = progress01;
            }

            if (_progressTxt != null)
            {
                _progressTxt.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
            }
        }

        /// <summary>QTE 게이지를 표시하고 성공 구간을 배치한다.</summary>
        public void ShowQte(float zoneStart01 , float zoneEnd01 , string keyLabel)
        {
            _resultFeedbackRemain = 0f;
            ApplyRingColor(_ringDefaultColor);

            if (_qteRootObj != null)
            {
                _qteRootObj.SetActive(true);
            }

            if (_qteZoneImg != null)
            {
                _qteZoneImg.fillAmount = Mathf.Clamp01(zoneEnd01 - zoneStart01);
                _qteZoneImg.rectTransform.localRotation = Quaternion.Euler(0f , 0f , -zoneStart01 * 360f);
            }

            if (_qteKeyTxt != null)
            {
                _qteKeyTxt.text = keyLabel ?? string.Empty;
            }

            SetIndicator(0f);
        }

        /// <summary>인디케이터 위치를 갱신한다.</summary>
        public void SetIndicator(float indicator01)
        {
            if (_qteIndicatorRectTrans == null)
            {
                return;
            }

            _qteIndicatorRectTrans.localRotation = Quaternion.Euler(0f , 0f , -indicator01 * 360f);
        }

        /// <summary>QTE 게이지를 즉시 숨긴다.</summary>
        public void HideQte()
        {
            if (_qteRootObj != null)
            {
                _qteRootObj.SetActive(false);
            }
        }

        /// <summary>
        /// 판정 결과를 색으로 표시하고, 표시 시간이 지나면 게이지를 자동으로 숨긴다.
        /// </summary>
        public void PlayResultFeedback(QTE_RESULT result)
        {
            ApplyRingColor(result == QTE_RESULT.SUCCESS ? _ringSuccessColor : _ringFailureColor);

            if (_resultFeedbackDuration <= 0f)
            {
                ApplyRingColor(_ringDefaultColor);
                HideQte();
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

            ApplyRingColor(_ringDefaultColor);
            HideQte();
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
