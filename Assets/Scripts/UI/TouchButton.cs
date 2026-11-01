using UnityEngine;
using UnityEngine.EventSystems;

// Round on-screen button. Raises the matching InputHub flag.
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Kind { Jump, Interact, Inventory, Craft, Build }

    public Kind kind = Kind.Jump;

    public void OnPointerDown(PointerEventData eventData)
    {
        switch (kind)
        {
            case Kind.Jump:
                InputHub.JumpHeld = true;
                InputHub.JumpDown = true;
                break;
            case Kind.Interact: InputHub.InteractDown = true; break;
            case Kind.Inventory: InputHub.InventoryDown = true; break;
            case Kind.Craft: InputHub.CraftDown = true; break;
            case Kind.Build: InputHub.BuildDown = true; break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (kind == Kind.Jump) InputHub.JumpHeld = false;
    }
}
