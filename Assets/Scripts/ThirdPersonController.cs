// 职责：角色移动、跳跃、重力、脚步音
// 依赖：InputHub、PlayerStats
using UnityEngine;

// Third-person movement for a CharacterController body.
// WASD relative to the camera, Shift to sprint, Space to jump.
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.7f;
    [Tooltip("Higher = snappier turning. Applied frame-rate independently.")]
    public float turnSpeed = 12f;

    [Header("Jump")]
    public float jumpHeight = 1.4f;
    public float gravity = -22f;

    [Header("Grounding")]
    [Tooltip("Snap down onto the ground when this close, so slopes and steps do not cause floating.")]
    public float groundSnapDistance = 0.45f;

    [Header("Input feel")]
    [Tooltip("How far the stick must move before the character starts walking.")]
    public float moveDeadZone = 0.12f;
    [Tooltip("Seconds a jump press is remembered, so it fires as soon as you land.")]
    public float jumpBuffer = 0.16f;
    [Tooltip("Seconds after leaving the ground where a jump is still allowed.")]
    public float coyoteTime = 0.10f;
    [Tooltip("Turn rate used while standing still and looking around.")]
    public float idleTurnSpeed = 7f;

    CharacterController cc;
    Transform cam;
    Vector3 verticalVelocity;
    float stepTimer;
    float jumpBufferedAt = -99f;
    float leftGroundAt = -99f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main != null ? Camera.main.transform : null;

        // Terrain walking: allow climbing slopes and small steps instead of getting stuck.
        if (cc != null)
        {
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.05f;
            cc.minMoveDistance = 0f;
        }
    }

    void Start()
    {
        // Drop onto the terrain so the player never spawns under the ground.
        var terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
            transform.position = new Vector3(transform.position.x, groundY + 0.5f, transform.position.z);
        }
    }

    void Update()
    {
        if (cc == null) return;
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity.y < 0f) verticalVelocity.y = -2f;

        // --- movement direction is relative to where the camera looks
        float h = InputHub.Move.x;
        float v = InputHub.Move.y;
        Vector3 input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);

        Vector3 moveDir;
        if (cam != null)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            moveDir = fwd * input.z + right * input.x;
        }
        else moveDir = input;

        var canSprint = InputHub.SprintHeld && (PlayerStats.Instance == null || PlayerStats.Instance.CanSprint());
        float speed = moveSpeed * (canSprint ? sprintMultiplier : 1f);

        // Analogue stick: magnitude drives speed, so a light push walks slowly.
        float magnitude = Mathf.Clamp01(input.magnitude);
        float throttle = magnitude < moveDeadZone
            ? 0f
            : Mathf.InverseLerp(moveDeadZone, 1f, magnitude);

        if (moveDir.sqrMagnitude > 0.0001f && throttle > 0f)
        {
            Vector3 dir = moveDir.normalized;
            cc.Move(dir * (speed * throttle) * dt);

            // Frame-rate independent turn (exponential, unlike Slerp with dt).
            float t = 1f - Mathf.Exp(-turnSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(dir, Vector3.up), t);

            stepTimer -= dt;
            if (stepTimer <= 0f)
            {
                stepTimer = canSprint ? 0.34f : 0.55f;
                if (grounded && AudioManager.Instance != null)
                    AudioManager.Instance.Play(AudioManager.Sfx.Footstep, canSprint ? 0.55f : 0.35f, 0.15f);
            }
        }
        else if (cam != null)
        {
            // Standing still: turning the camera also turns the character, so the
            // right side of the screen controls which way you are facing.
            Vector3 flat = cam.forward; flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                float t = 1f - Mathf.Exp(-idleTurnSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                                      Quaternion.LookRotation(flat.normalized, Vector3.up), t);
            }
        }

        // --- jump with buffering + coyote time, so a slightly early press still works
        if (InputHub.JumpDown) jumpBufferedAt = Time.time;
        if (grounded) leftGroundAt = Time.time;

        bool buffered = Time.time - jumpBufferedAt <= jumpBuffer;
        bool canJump = grounded || Time.time - leftGroundAt <= coyoteTime;
        if (buffered && canJump)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferedAt = -99f;
            leftGroundAt = -99f;
        }

        verticalVelocity.y += gravity * dt;
        cc.Move(verticalVelocity * dt);

        // --- ground snap: keep the feet on sloped terrain instead of hovering
        if (!cc.isGrounded && verticalVelocity.y < 0f)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                if (transform.position.y - groundY < groundSnapDistance)
                {
                    transform.position = new Vector3(transform.position.x, groundY + 0.02f, transform.position.z);
                    verticalVelocity.y = 0f;
                }
            }
        }
    }
}
