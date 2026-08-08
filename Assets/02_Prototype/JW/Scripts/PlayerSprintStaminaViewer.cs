using UnityEngine;
using UnityEngine.UI;

public interface IPlayerStatViewer
{
    void UpdateSprintStamina(float staminaNomalize);
}

public class PlayerSprintStaminaViewer : MonoBehaviour, IPlayerStatViewer
{
    [SerializeField] private GameObject _staminaUI;
    [SerializeField] private Image _staminaImage;

    public void UpdateSprintStamina(float staminaNomalize)
    {
        _staminaImage.fillAmount = staminaNomalize;

        if (_staminaImage.fillAmount == 1)
            _staminaUI.SetActive(false);
        else
            _staminaUI.SetActive(true);
    }
}
