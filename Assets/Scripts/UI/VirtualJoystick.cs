using UnityEngine;
using UnityEngine.EventSystems;

// Left-thumb stick. Writes the normalised offset into InputHub.Move.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform knob;
    public float radius = 90f;
    [Range(0.05f, 0.95f)] public float deadZone = 0.15f;

    void Awake()
    {
        if (background == null) background = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData) { Apply(eventData); }

    public void OnDrag(PointerEventData eventData) { Apply(eventData); }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        InputHub.Move = Vector2.zero;
        InputHub.TouchMoveActive = false;
    }

    void Apply(PointerEventData eventData)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out local))
            return;

        Vector2 offset = Vector2.ClampMagnitude(local / radius, 1f);
        if (knob != null) knob.anchoredPosition = offset * radius;

        float magnitude = offset.magnitude;
        Vector2 value = magnitude < deadZone ? Vector2.zero : offset;

        InputHub.Move = value;
        InputHub.TouchMoveActive = magnitude >= deadZone;
    }
}
