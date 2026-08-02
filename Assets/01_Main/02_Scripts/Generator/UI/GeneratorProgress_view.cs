using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HideSeek.Generators
{
    public sealed class GeneratorProgress_view : AView
    {
        [SerializeField] private Slider _progressSlider;

        public override void Clear()
        {
            base.Clear();
            SetProgress(0f);
        }

        public void SetProgress(float progress01)
        {
            if (_progressSlider != null)
                _progressSlider.value = progress01;
        }
    }
}
