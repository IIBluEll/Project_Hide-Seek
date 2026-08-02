using UnityEngine;

public class InteractPresenter
{
    private InteractViewer _viewer;
    private PlayerInteractionController _model;

    public void Init(InteractViewer viewer, PlayerInteractionController model)
    {
        _viewer = viewer;
        _model = model;

        _model.OnInsightInteractEvent += _viewer.ShowInteractUI;
        _model.OnOutsightInteractionEvent += _viewer.HideInteractUI;

    }
}
