using System;
using UnityEngine;
using UnityEngine.UI;

public class ThrowUIViewer : MonoBehaviour
{
    [SerializeField] private GameObject _gageObject;
    [SerializeField] private Image _gageImage;

    public void ShowThrowUI()
    {
        _gageObject.SetActive(true);
    }

    public void ChargeGage(float current)
    {
        _gageImage.fillAmount = current;
    }

    public void HideThrowUI()
    {
        _gageObject.SetActive(false);
    }

    public void OnAimStateChanged(bool on)
    {
        if (on)
            ShowThrowUI();
        else
            HideThrowUI();
    }
}
