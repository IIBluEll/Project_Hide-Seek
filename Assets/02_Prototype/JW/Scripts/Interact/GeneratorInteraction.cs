using HideSeek.Generators;
using UnityEngine;

public class GeneratorInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private Generator _generator;

    public string InteractionPrompt => "발전기 돌리기";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return _generator.State != GENERATOR_STATE.COMPLETED;
    }

    public void InteractAct(PlayerInteractionController playerInteractor)
    {
        _generator.TryBeginRepair();
        playerInteractor.BeginGeneratorRepair();
    }

    public void InteractRelease(PlayerInteractionController playerInteractionController)
    {
        _generator.CancelRepair();
        playerInteractionController.EndGeneratorRepair();
    }
}
