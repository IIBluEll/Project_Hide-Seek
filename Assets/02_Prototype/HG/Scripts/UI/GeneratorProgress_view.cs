using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.Generators
{
    /// <summary>
    /// <see cref="_progressImg"/>는 Image Type을 Filled로 두어야 fillAmount가 반영된다.
    /// </summary>
    public sealed class GeneratorProgress_view : AView
    {
        [SerializeField] private Image _progressImg;
        [SerializeField] private TMP_Text _progressTxt;

        public override void Clear()
        {
            base.Clear();

            SetProgress(0f);
        }

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
    }
}
