using UnityEngine;
using UnityEngine.EventSystems;

// Dismiss on release so the entire pointer gesture is consumed by the UI shield.
public class BuildingCardDismissSurface : MonoBehaviour, IPointerClickHandler
{
    [System.NonSerialized] public BinaPopup owner;
    public void OnPointerClick(PointerEventData data)
    {
        if(data.button==PointerEventData.InputButton.Left && data.pointerCurrentRaycast.gameObject==gameObject && owner!=null)
            owner.DisariTiklandi();
    }
}
