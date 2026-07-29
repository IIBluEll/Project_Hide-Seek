using UnityEngine;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private Transform _handItemPivot;

    private GrapItem _grapItem;
    public bool GrappedItem => _grapItem != null;

    public void GrapItem(GrapItem grapItem)
    {
        if (_grapItem != null)
        {
            _grapItem.transform.parent = null;
            _grapItem.Release();
        }

        _grapItem = grapItem;
        _grapItem.transform.parent = _handItemPivot;
        _grapItem.transform.localPosition = Vector3.zero;
        _grapItem.Grapped();
    }
    public void ThrowItem()
    {

    }
}
