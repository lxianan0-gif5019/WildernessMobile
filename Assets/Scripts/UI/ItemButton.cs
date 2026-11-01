using UnityEngine;
using UnityEngine.EventSystems;

// One clickable row inside the inventory panel.
public class ItemButton : MonoBehaviour, IPointerDownHandler
{
    public string itemId;
    public GameUI owner;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null) owner.OnItemClicked(itemId);
    }
}
