using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialDescription : MonoBehaviour, IDescription
{
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _descriptionImage;
    [SerializeField] private Button _confirmButton;

    public event Action OnClickConfirm;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(OnClickConfirmButton);
    }

    public void ShowDescription(string description, Sprite descriptionSprite)
    {
        _descriptionText.text = description;
        _descriptionImage.sprite = descriptionSprite;
    }

    public void OnClickConfirmButton()
    {
        OnClickConfirm?.Invoke();
        this.gameObject.SetActive(false);
    }
}
