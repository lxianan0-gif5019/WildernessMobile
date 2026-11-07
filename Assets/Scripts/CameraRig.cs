// 职责：用 Cinemachine 装配第三人称相机，并把输入/抖动接进去
// 依赖：InputHub、GameUI.PanelOpen、GameSettings、Cinemachine
using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance;

    [Header("Arm")]
    public float distance = 4.6f;
    [Tooltip("Set true once the runtime vcam placement is fixed; the hand-written camera is used otherwise.")]
    public bool takeOverCamera = false;
    public float shoulderOffset = 0.35f;
    public float verticalArm = 0.45f;
    public Vector3 damping = new Vector3(0.12f, 0.12f, 0.12f);

    [Header("Look")]
    public float minPitch = -12f;
    public float maxPitch = 62f;

    CinemachineVirtualCamera vcam;
    Cinemachine3rdPersonFollow follow;
    Transform player;

    float yaw;
    float pitch = 16f;
    float shake;

    void Awake()
    {
        Instance = this;

        if (!takeOverCamera)
        {
            enabled = false;
            return;
        }

        // The old hand-written camera must not fight Cinemachine.
        var old = GetComponent<ThirdPersonCamera>();
        if (old != null) old.enabled = false;

        if (GetComponent<CinemachineBrain>() == null) gameObject.AddComponent<CinemachineBrain>();
    }

    void Start()
    {
        var controller = Object.FindObjectOfType<ThirdPersonController>();
        if (controller != null) player = controller.transform;
        if (player == null) return;

        yaw = player.eulerAngles.y;
        BuildVcam();
    }

    void BuildVcam()
    {
        var go = new GameObject("CM ThirdPerson");
        go.transform.position = player.position + Vector3.up * 1.6f;
        go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 20;

        follow = vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        follow.CameraDistance = distance;
        follow.ShoulderOffset = new Vector3(shoulderOffset, 0f, 0f);
        follow.VerticalArmLength = verticalArm;
        follow.Damping = damping;
        follow.CameraCollisionFilter = ~0;
        follow.CameraRadius = 0.3f;
        follow.DampingIntoCollision = 0.05f;
        follow.DampingFromCollision = 0.4f;

        // Follow must be set after the body exists, then the cached pipeline rebuilt,
        // otherwise Cinemachine keeps the vcam at its spawn transform.
        vcam.Follow = player;
        vcam.InvalidateComponentPipeline();

        Debug.Log("[CameraRig] Cinemachine 3rd person arm ready, distance=" + distance);
    }

    void Update()
    {
        if (vcam == null) return;

        if (!GameUI.PanelOpen && !InputHub.MenuOpen)
        {
            float scale = Mathf.Clamp(GameSettings.LookSensitivity / 0.085f, 0.35f, 4f);
            float sens = 1.6f * scale;

            yaw += InputHub.LookDelta.x * sens;
            pitch -= InputHub.LookDelta.y * sens * (GameSettings.InvertLook ? -1f : 1f);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (shake > 0.0001f) shake = Mathf.Max(0f, shake - Time.deltaTime * 2.2f);

        float s = shake * shake;
        var shakeEuler = new Vector3(
            (Random.value * 2f - 1f) * s * 3f,
            (Random.value * 2f - 1f) * s * 3f,
            0f);

        // Cinemachine positions the camera from this rotation; we only steer it.
        vcam.transform.rotation = Quaternion.Euler(pitch + shakeEuler.x, yaw + shakeEuler.y, 0f);

        if (Time.frameCount % 120 == 0)
        {
            var camT = Camera.main != null ? Camera.main.transform : null;
            var brain = GetComponent<CinemachineBrain>();
            Debug.Log("[CamDbg] player=" + (player != null ? player.position.ToString("F1") : "?")
                      + " vcam=" + vcam.transform.position.ToString("F1")
                      + " cam=" + (camT != null ? camT.position.ToString("F1") : "?")
                      + " dist=" + follow.CameraDistance
                      + " active=" + (brain != null && brain.ActiveVirtualCamera != null ? brain.ActiveVirtualCamera.Name : "none")
                      + " rot=" + vcam.transform.eulerAngles.ToString("F0"));
        }
    }

    public void Shake(float amount)
    {
        shake = Mathf.Clamp(shake + amount, 0f, 1f);
    }

    /// <summary>Used by settings sliders so distance changes apply live.</summary>
    public void SetDistance(float metres)
    {
        distance = Mathf.Clamp(metres, 2.5f, 8f);
        if (follow != null) follow.CameraDistance = distance;
    }
}
