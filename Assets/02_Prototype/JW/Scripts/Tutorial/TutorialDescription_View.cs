using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialDescription_View : MonoBehaviour, IDescription
{
    [SerializeField] private GameObject _obj;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _descriptionImage;
    [SerializeField] private Button _confirmButton;

    public event Action OnClickConfirm;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(OnClickConfirmButton);
    }

    public void ShowDescription(DescriptionData data)
    {
        _nameText.text = data.Name;
        _descriptionText.text = data.Description;
        _descriptionImage.sprite = data.Sprite;

        _obj.SetActive(true);
    }

    public void OnClickConfirmButton()
    {
        OnClickConfirm?.Invoke();
        _obj.SetActive(false);
    }
}
