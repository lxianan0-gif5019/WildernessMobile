using System;
using System.Collections.Generic;
using UnityEngine;

// Owns all touch input for the game UI: joystick, look drag and buttons.
// Reads Input.touches directly instead of going through EventSystem, because
// EventSystem's touch handling is unreliable on device (multi-touch especially).
// Runs before gameplay and before GameUI so the flags it sets are consumed this frame.
[DefaultExecutionOrder(-50)]
public class TouchRouter : MonoBehaviour
{
    public static TouchRouter Instance;

    [Header("Joystick")]
    public RectTransform stickArea;
    public RectTransform stickKnob;
    public float stickRadius = 90f;
    [Range(0.05f, 0.95f)] public float stickDeadZone = 0.15f;

    [Header("Look")]
    public float lookSensitivity = 0.085f;

    class Btn
    {
        public RectTransform rect;
        public Action action;
    }

    readonly List<Btn> buttons = new List<Btn>();

    int stickFinger = -1;
    int lookFinger = -1;
    Vector2 lastLookPos;
    bool mouseStick;
    bool mouseLook;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterButton(RectTransform rect, Action action)
    {
        if (rect == null || action == null) return;
        buttons.Add(new Btn { rect = rect, action = action });
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                switch (t.phase)
                {
                    case TouchPhase.Began: Begin(t.fingerId, t.position); break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary: Move(t.fingerId, t.position); break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled: End(t.fingerId); break;
                }
            }
        }
        else
        {
            Mouse();
        }
    }

    // ------------------------------------------------------------------ touch
    [Header("Stick zone (fraction of screen)")]
    [Tooltip("Any touch starting inside this bottom-left region is treated as the joystick, never as camera look.")]
    public float stickZoneWidth = 0.45f;
    public float stickZoneHeight = 0.60f;

    void Begin(int id, Vector2 pos)
    {
        // Bottom-left of the screen belongs to the joystick, no matter where exactly
        // the thumb lands. This is what stops the stick from also turning the camera.
        if ((stickFinger < 0 || stickFinger == id) && (InStickZone(pos) || Contains(stickArea, pos)))
        {
            stickFinger = id;
            ApplyStick(pos);
            return;
        }

        var hit = HitButton(pos);
        if (hit != null)
        {
            hit.action();
            if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.UiClick, 0.7f);
            return;
        }

        if (lookFinger < 0)
        {
            lookFinger = id;
            lastLookPos = pos;
        }
    }

    bool InStickZone(Vector2 screenPos)
    {
        return screenPos.x <= Screen.width * stickZoneWidth
            && screenPos.y <= Screen.height * stickZoneHeight;
    }

    void Move(int id, Vector2 pos)
    {
        if (id == stickFinger) { ApplyStick(pos); return; }

        if (id == lookFinger)
        {
            Vector2 delta = pos - lastLookPos;
            lastLookPos = pos;
            if (!GameUI.PanelOpen) InputHub.LookDelta += delta * lookSensitivity;
        }
    }

    void End(int id)
    {
        if (id == stickFinger) ReleaseStick();
        if (id == lookFinger) lookFinger = -1;
    }

    void ApplyStick(Vector2 screenPos)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(stickArea, screenPos, null, out local))
            return;

        Vector2 offset = Vector2.ClampMagnitude(local / stickRadius, 1f);
        if (stickKnob != null) stickKnob.anchoredPosition = offset * stickRadius;

        float magnitude = offset.magnitude;
        InputHub.Move = magnitude < stickDeadZone ? Vector2.zero : offset;
        InputHub.TouchMoveActive = magnitude >= stickDeadZone;
        // Pushing the stick all the way out sprints (there is no shift key on a phone).
        InputHub.SprintHeld = magnitude >= 0.95f;
    }

    void ReleaseStick()
    {
        stickFinger = -1;
        if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
        InputHub.Move = Vector2.zero;
        InputHub.TouchMoveActive = false;
        InputHub.SprintHeld = false;
    }

    // ------------------------------------------------------------------ mouse
    void Mouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Contains(stickArea, Input.mousePosition)) { mouseStick = true; ApplyStick(Input.mousePosition); return; }

            var hit = HitButton(Input.mousePosition);
            if (hit != null) { hit.action(); return; }

            mouseLook = true;
            lastLookPos = Input.mousePosition;
            return;
        }

        if (mouseStick)
        {
            if (Input.GetMouseButton(0)) ApplyStick(Input.mousePosition);
            else { mouseStick = false; ReleaseStick(); }
            return;
        }

        if (mouseLook)
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastLookPos;
                lastLookPos = Input.mousePosition;
                if (!GameUI.PanelOpen) InputHub.LookDelta += delta * lookSensitivity;
            }
            else mouseLook = false;
        }
    }

    // ---------------------------------------------------------------- helpers
    Btn HitButton(Vector2 screenPos)
    {
        for (int i = buttons.Count - 1; i >= 0; i--)
        {
            var b = buttons[i];
            if (b.rect == null) continue;
            if (!b.rect.gameObject.activeInHierarchy) continue;
            if (Contains(b.rect, screenPos)) return b;
        }
        return null;
    }

    static bool Contains(RectTransform rect, Vector2 screenPos)
    {
        if (rect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
    }
}
