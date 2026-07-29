using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _pivot;

    public string InteractionPrompt => "숨기";

    public bool CanInteract(PlayerInteractionController playerInteractor)
    {
        return true;
    }

    public void Interact(PlayerInteractionController playerInteractor)
    {
        //엎드리기 애니메이션 실행
        //위치 이동
        //캐릭터 컨트롤러 콜라이더 줄이기
    }
}
