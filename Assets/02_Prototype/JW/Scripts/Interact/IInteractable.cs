using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract(PlayerInteractionController playerInteractor);
    void Interact(PlayerInteractionController playerInteractor);
}
