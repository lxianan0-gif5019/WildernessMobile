using UnityEngine;

// Orbit-follow camera for a third-person character.
// Attaches to Main Camera; finds the player automatically when no target is set.
//
// Vertical follow is smoothed separately (and much more slowly) than horizontal
// follow, so jumping does not bounce the whole view around.
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float height = 1.6f;
    public float mouseSensitivity = 1.6f;
    public float minPitch = -15f;
    public float maxPitch = 65f;

    [Header("Look feel")]
    [Tooltip("Seconds of smoothing applied to the look direction. Higher = softer, less dizzy.")]
    public float lookSmoothing = 0.05f;

    [Header("Smoothing")]
    [Tooltip("How fast the camera tracks the player horizontally / sideways.")]
    public float followSmooth = 16f;
    [Tooltip("How fast the camera tracks the player vertically. Low values ignore jumps.")]
    public float verticalSmooth = 3.5f;

    [Header("Collision")]
    public float collisionRadius = 0.3f;
    public LayerMask collisionMask = ~0;

    float yaw;
    float pitch = 18f;
    float yawShown;
    float pitchShown;
    float yawVelocity;
    float pitchVelocity;
    Vector3 focusSmoothed;
    bool hasFocus;

    void Start()
    {
        if (target == null)
        {
            var controller = Object.FindObjectOfType<ThirdPersonController>();
            if (controller != null) target = controller.transform;
        }

        if (target != null) yaw = target.eulerAngles.y;
        yawShown = yaw;
        pitchShown = pitch;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += InputHub.LookDelta.x * mouseSensitivity;
        pitch -= InputHub.LookDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Smooth the *angles*, not the transform: this removes the snap that makes
        // fast turns feel dizzy, without adding lag to the follow position.
        yawShown = Mathf.SmoothDampAngle(yawShown, yaw, ref yawVelocity, lookSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
        pitchShown = Mathf.SmoothDampAngle(pitchShown, pitch, ref pitchVelocity, lookSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);

        // --- focus point: horizontal and vertical are smoothed at different rates
        Vector3 focusTarget = target.position + Vector3.up * height;
        if (!hasFocus) { focusSmoothed = focusTarget; hasFocus = true; }

        float th = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        float tv = 1f - Mathf.Exp(-verticalSmooth * Time.deltaTime);
        focusSmoothed.x = Mathf.Lerp(focusSmoothed.x, focusTarget.x, th);
        focusSmoothed.z = Mathf.Lerp(focusSmoothed.z, focusTarget.z, th);
        focusSmoothed.y = Mathf.Lerp(focusSmoothed.y, focusTarget.y, tv);

        // --- orientation (angles are already smoothed above, so no extra slerp)
        Quaternion smoothedRot = Quaternion.Euler(pitchShown, yawShown, 0f);

        // --- position from the smoothed orientation (keeps camera and look direction in sync)
        Vector3 desired = focusSmoothed - smoothedRot * Vector3.forward * distance;

        // --- collision: sphere cast so thin obstacles do not make it snap
        if (Physics.SphereCast(focusSmoothed, collisionRadius, (desired - focusSmoothed).normalized,
                               out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(target))
            {
                float safe = Mathf.Max(0.8f, hit.distance);
                desired = focusSmoothed - smoothedRot * Vector3.forward * safe;
            }
        }

        transform.position = desired;
        transform.rotation = smoothedRot;
    }
}
