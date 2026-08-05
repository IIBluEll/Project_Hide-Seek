using HideSeek.Generators;
using UnityEngine;

public class PlayerQTEInputSource : MonoBehaviour, IInputSource
{
    [SerializeField] private PlayerInputReader _inputReader;

    private bool _wasPressed;


    private void OnEnable()
    {
        _inputReader.OnJumpEvent += OnQteActioned;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.OnJumpEvent -= OnQteActioned;
    }

    public bool IsQteKeyDown()
    {
        bool result = _wasPressed;
        _wasPressed = false;
        return result;
    }

    public string GetQteKeyLabel()
    {
        return "Space";
    }

    private void OnQteActioned(bool isPressed)
    {
        if (isPressed)
            _wasPressed = true;
    }
}
