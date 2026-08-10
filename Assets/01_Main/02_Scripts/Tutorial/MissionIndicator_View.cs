using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionIndicator_View : MonoBehaviour, IMissionIndicator
{
    [SerializeField] private GameObject _rootObj;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Toggle _checkToggle;
    [SerializeField] private TMP_Text _missionTxt;
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _countTxt;

    public void ShowIndicator(MissionIndicatorData missionIndicatorData)
    {
        if (missionIndicatorData == null)
        {
            HideIndicator();
            return;
        }

        if (_rootObj != null)
            _rootObj.SetActive(true);

        SetCompleted(false);
        SetMissionText(missionIndicatorData.MissionText);
        SetIcon(missionIndicatorData.IconSprite);

        if (missionIndicatorData.HasCount)
            UpdateCount(0, missionIndicatorData.TargetCount);
        else if (_countTxt != null)
            _countTxt.gameObject.SetActive(false);

        RebuildLayout();
    }

    private void RebuildLayout()
    {
        _missionTxt?.ForceMeshUpdate();
        _countTxt?.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();

        if (_rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            if (_rectTransform.parent is RectTransform parentRectTrans)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTrans);
        }

        Canvas.ForceUpdateCanvases();
    }

    public void UpdateCount(int currentCount, int targetCount)
    {
        if (_countTxt == null)
            return;

        int safeTargetCount = Mathf.Max(1, targetCount);
        int safeCurrentCount = Mathf.Clamp(currentCount, 0, safeTargetCount);

        _countTxt.gameObject.SetActive(true);
        _countTxt.text = $"{safeCurrentCount}/{safeTargetCount}";
    }
    public void SetCompleted(bool isCompleted)
    {
        if (_checkToggle != null)
            _checkToggle.isOn = isCompleted;
    }
    public void HideIndicator()
    {
        if (_rootObj != null)
            _rootObj.SetActive(false);
        else
            gameObject.SetActive(false);
    }
    private void SetMissionText(string missionText)
    {
        if (_missionTxt != null)
            _missionTxt.text = missionText;
    }
    private void SetIcon(Sprite iconSprite)
    {
        if (_iconImg == null)
            return;

        bool hasIcon = iconSprite != null;
        _iconImg.gameObject.SetActive(hasIcon);
        _iconImg.sprite = iconSprite;
    }
}
