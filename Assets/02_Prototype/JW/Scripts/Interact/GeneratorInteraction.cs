using HideSeek.Generators;
using UnityEngine;

public class GeneratorInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private Generator _generator;
    private PlayerInteractionController _controller;

    public string InteractionPrompt => "발전기 돌리기";

    private void Awake()
    {
        _generator.Completed += OnCompleteGenerator;
    }

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return _generator.State != GENERATOR_STATE.COMPLETED;
    }

    public void OnCompleteGenerator()
    {
        Debug.Log("Complete");
        if(_controller != null)
        {
            _controller.SetActionState(PLAYER_ACTION_STATE.IDLE);
            _controller = null;
        }
    }

    public void InteractAct(PlayerInteractionController playerInteractor)
    {
        _generator.TryBeginRepair();
        playerInteractor.SetActionState(PLAYER_ACTION_STATE.REPAIRING_GENERATOR);
        _controller = playerInteractor;
        Debug.Log(_controller);
    }

    public void InteractRelease(PlayerInteractionController playerInteractionController)
    {
        _generator.CancelRepair();
        playerInteractionController.SetActionState(PLAYER_ACTION_STATE.IDLE);
        _controller = null;
    }
}
