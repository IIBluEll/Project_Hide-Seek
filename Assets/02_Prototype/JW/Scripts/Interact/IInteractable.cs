using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract(PlayerInteractionController playerInteractor);
    void InteractAct(PlayerInteractionController playerInteractor);
    void InteractRelease(PlayerInteractionController playerInteractionController);
}
