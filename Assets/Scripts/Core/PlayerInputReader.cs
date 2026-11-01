using UnityEngine;

// Keyboard + mouse into InputHub. Runs before everything else so one-shot
// flags set here are visible to gameplay this frame and cleared next frame.
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    void Update()
    {
        InputHub.ClearOneShots();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 keyboard = new Vector2(h, v);

        if (keyboard.sqrMagnitude > 0.01f)
            InputHub.Move = Vector2.ClampMagnitude(keyboard, 1f);
        else if (!InputHub.TouchMoveActive)
            InputHub.Move = Vector2.zero;

        // Mouse look is only meaningful while the cursor is locked.
        if (Cursor.lockState == CursorLockMode.Locked)
            InputHub.LookDelta += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        InputHub.JumpHeld = Input.GetButton("Jump");
        if (Input.GetButtonDown("Jump")) InputHub.JumpDown = true;

        if (Input.GetKeyDown(KeyCode.E)) InputHub.InteractDown = true;
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) InputHub.InventoryDown = true;
        if (Input.GetKeyDown(KeyCode.C)) InputHub.CraftDown = true;
        if (Input.GetKeyDown(KeyCode.B)) InputHub.BuildDown = true;
    }
}
