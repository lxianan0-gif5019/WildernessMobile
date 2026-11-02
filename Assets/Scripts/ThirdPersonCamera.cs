// 职责：第三人称环绕相机（距离/碰撞/平滑/抖动）
// 依赖：InputHub、GameUI.PanelOpen、GameSettings
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
    public float minPitch = -8f;
    public float maxPitch = 62f;

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
    [Tooltip("Never come closer than this, so the character cannot fill the screen.")]
    public float minDistance = 2.2f;
    [Tooltip("Metres per second the camera moves back out after an obstruction clears.")]
    public float recoverSpeed = 3.5f;
    [Tooltip("Keep the camera at least this far above the terrain surface.")]
    public float groundClearance = 0.6f;

    float yaw;
    float pitch = 18f;
    float yawShown;
    float pitchShown;
    float yawVelocity;
    float pitchVelocity;
    Vector3 focusSmoothed;
    bool hasFocus;
    float currentDistance;
    float sensitivityScale = 1f;

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

        // While a menu panel is open the cursor is free, so it must not turn the camera.
        if (!GameUI.PanelOpen && !InputHub.MenuOpen)
        {
            // The settings slider scales the base sensitivity (0.5x .. 4x).
            sensitivityScale = Mathf.Clamp(GameSettings.LookSensitivity / 0.085f, 0.35f, 4f);
            float sens = mouseSensitivity * sensitivityScale;

            yaw += InputHub.LookDelta.x * sens;
            pitch -= InputHub.LookDelta.y * sens * (GameSettings.InvertLook ? -1f : 1f);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

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
        Vector3 back = smoothedRot * Vector3.back;

        // --- collision: find the furthest unobstructed distance behind the focus point.
        float wanted = distance;
        if (Physics.SphereCast(focusSmoothed, collisionRadius, back,
                               out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(target))
                wanted = Mathf.Max(minDistance, hit.distance);
        }

        // Never closer than minDistance (the character would fill the frame), and
        // never further than the configured distance.
        wanted = Mathf.Clamp(wanted, Mathf.Min(minDistance, distance), distance);

        // Pull in immediately (avoid clipping through geometry) but ease back out,
        // so passing a tree does not snap the view.
        if (currentDistance <= 0.01f) currentDistance = wanted;
        if (wanted < currentDistance) currentDistance = wanted;
        else currentDistance = Mathf.MoveTowards(currentDistance, wanted, recoverSpeed * Time.deltaTime);

        Vector3 desired = focusSmoothed + back * currentDistance;

        // --- keep the camera above the ground on slopes
        var terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float ground = terrain.SampleHeight(desired) + terrain.transform.position.y;
            if (desired.y < ground + groundClearance)
                desired.y = ground + groundClearance;
        }
        else if (Physics.Raycast(desired + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 20f, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (desired.y < groundHit.point.y + groundClearance)
                desired.y = groundHit.point.y + groundClearance;
        }

        transform.position = desired;
        transform.rotation = smoothedRot;

        // --- hit shake (decays fast; harmless when zero)
        if (shake > 0.0001f)
        {
            shake = Mathf.Max(0f, shake - Time.deltaTime * 2.2f);
            float s = shake * shake;
            transform.position += new Vector3(
                (Random.value * 2f - 1f) * s * 0.5f,
                (Random.value * 2f - 1f) * s * 0.5f,
                0f);
        }
    }

    float shake;

    /// <summary>Brief camera shake, 0..1.</summary>
    public void Shake(float amount)
    {
        shake = Mathf.Clamp(shake + amount, 0f, 1f);
    }
}
