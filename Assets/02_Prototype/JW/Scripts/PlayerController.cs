using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InteractPresenter _interactPresenter = new InteractPresenter();

    [SerializeField] private InteractViewer _viewer;
    [SerializeField] private PlayerInteractionController _interactController;

    private void Awake()
    {
        _interactPresenter.Init(_viewer, _interactController);
    }
}
