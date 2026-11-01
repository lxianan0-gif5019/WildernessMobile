using UnityEngine;
using UnityEngine.EventSystems;

// Right-hand area: dragging a finger turns the camera.
public class TouchLookArea : MonoBehaviour, IDragHandler
{
    [Tooltip("Degrees of rotation per pixel of drag.")]
    public float sensitivity = 0.12f;

    public void OnDrag(PointerEventData eventData)
    {
        InputHub.LookDelta += eventData.delta * sensitivity;
    }
}
