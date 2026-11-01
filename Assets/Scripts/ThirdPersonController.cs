using UnityEngine;

// Third-person movement for a CharacterController body.
// WASD relative to the camera, Shift to sprint, Space to jump.
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.7f;
    public float turnSpeed = 12f;

    [Header("Jump")]
    public float jumpHeight = 1.4f;
    public float gravity = -22f;

    CharacterController cc;
    Transform cam;
    Vector3 verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main != null ? Camera.main.transform : null;
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
        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity.y < 0f) verticalVelocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);

        Vector3 moveDir = Vector3.zero;
        if (cam != null)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            moveDir = fwd * input.z + right * input.x;
        }
        else
        {
            moveDir = input;
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
            cc.Move(moveDir.normalized * speed * Time.deltaTime);

            Quaternion look = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        if (grounded && Input.GetButtonDown("Jump"))
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity.y += gravity * Time.deltaTime;
        cc.Move(verticalVelocity * Time.deltaTime);
    }
}
