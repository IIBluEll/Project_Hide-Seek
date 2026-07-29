using UnityEngine;
using UnityEngine.UI;

public class InteractViewer : MonoBehaviour
{
    [SerializeField] private GameObject _interactUI;
    [SerializeField] private Text _text;

    public void ShowInteractUI(string prompt)
    {
        _text.text = prompt;
        _interactUI.SetActive(true);
    }
    public void HideInteractUI()
    {
        _interactUI.SetActive(false);
    }
}
