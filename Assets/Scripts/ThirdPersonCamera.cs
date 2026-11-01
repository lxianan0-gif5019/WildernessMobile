using UnityEngine;

// Orbit-follow camera for a third-person character.
// Attaches to Main Camera; finds the player automatically when no target is set.
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float height = 1.6f;
    public float mouseSensitivity = 3f;
    public float minPitch = -15f;
    public float maxPitch = 65f;
    public float followSmooth = 14f;
    public float collisionRadius = 0.3f;

    float yaw;
    float pitch = 18f;

    void Start()
    {
        if (target == null)
        {
            var controller = Object.FindObjectOfType<ThirdPersonController>();
            if (controller != null) target = controller.transform;
        }

        if (target != null) yaw = target.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 desired = focus - rotation * Vector3.forward * distance;

        // Pull the camera in when something blocks the view (terrain, trees).
        if (Physics.Linecast(focus, desired, out RaycastHit hit))
            desired = hit.point + hit.normal * collisionRadius;

        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
    }
}
