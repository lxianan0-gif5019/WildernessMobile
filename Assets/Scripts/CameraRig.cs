// 职责：驱动场景里已有的 Cinemachine vcam 旋转（位置交给 3rd Person Follow）
// 依赖：InputHub、GameUI.PanelOpen、GameSettings、Cinemachine
using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance;

    [Header("Arm (only used when the rig has to build its own vcam)")]
    public float distance = 4.6f;
    public float shoulderOffset = 0.35f;
    public float verticalArm = 0.45f;
    public Vector3 damping = new Vector3(0.12f, 0.12f, 0.12f);
    public bool takeOverCamera = true;

    [Header("Look")]
    public float minPitch = -12f;
    public float maxPitch = 62f;
    [Tooltip("Seconds of smoothing on the look angles. Removes touch jitter without adding lag.")]
    public float lookSmoothing = 0.05f;

    CinemachineVirtualCamera vcam;
    Cinemachine3rdPersonFollow follow;
    Transform player;
    Transform pivot;

    float yaw;
    float pitch = 16f;
    float yawShown;
    float pitchShown;
    float yawVelocity;
    float pitchVelocity;
    float shake;
    bool builtHere;

    void Awake()
    {
        Instance = this;

        if (!takeOverCamera)
        {
            enabled = false;
            return;
        }

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
        yawShown = yaw;
        pitchShown = pitch;

        // Prefer a vcam authored in the scene: its component pipeline is already
        // serialised and validated, which is what Cinemachine expects.
        vcam = FindSceneVcam();
        if (vcam == null) BuildVcam();

        follow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        // The arm is driven by the FOLLOW TARGET's rotation, so that target must be a
        // root object steered by us. If it is a child of the player, every turn of the
        // character swings the camera (the "W+A whips the view" bug).
        pivot = vcam.Follow;
        if (pivot == null)
        {
            var go = GameObject.Find("CameraPivot");
            if (go == null) go = new GameObject("CameraPivot");
            pivot = go.transform;
        }
        if (pivot.parent != null) pivot.SetParent(null, true);

        vcam.Follow = pivot;

        Debug.Log("[CameraRig] using vcam '" + vcam.name + "' follow=" + (vcam.Follow != null ? vcam.Follow.name : "none")
                  + " body=" + (follow != null ? "3rdPersonFollow" : "MISSING")
                  + " dist=" + (follow != null ? follow.CameraDistance.ToString("F1") : "-")
                  + " builtHere=" + builtHere);

        var comps = vcam.GetComponents<Component>();
        var names = "";
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;
            var behaviour = comps[i] as Behaviour;
            names += comps[i].GetType().Name + "(" + (behaviour == null || behaviour.enabled ? "on" : "off") + ") ";
        }
        Debug.Log("[CameraRig] vcam components: " + names);

        // Which component is actually acting as the body?
        var body = vcam.GetCinemachineComponent<CinemachineComponentBase>();
        Debug.Log("[CameraRig] active body = " + (body != null ? body.GetType().Name : "NONE"));
    }

    static CinemachineVirtualCamera FindSceneVcam()
    {
        var all = Object.FindObjectsOfType<CinemachineVirtualCamera>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null) return all[i];
        return null;
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

        vcam.Follow = player;
        vcam.InvalidateComponentPipeline();
        builtHere = true;
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

        // Smooth the angles, not the transform: kills touch jitter without adding lag.
        yawShown = Mathf.SmoothDampAngle(yawShown, yaw, ref yawVelocity, lookSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
        pitchShown = Mathf.SmoothDampAngle(pitchShown, pitch, ref pitchVelocity, lookSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);

        float s = shake * shake;
        var shakeEuler = new Vector3(
            (Random.value * 2f - 1f) * s * 3f,
            (Random.value * 2f - 1f) * s * 3f,
            0f);

        // Drive the pivot: position follows the player, rotation is ours alone.
        if (pivot != null && player != null)
        {
            pivot.position = player.position + Vector3.up * 1.5f;
            pivot.rotation = Quaternion.Euler(pitchShown + shakeEuler.x, yawShown + shakeEuler.y, 0f);
        }

#if UNITY_EDITOR
        if (Time.frameCount % 120 == 0)
        {
            var camT = Camera.main != null ? Camera.main.transform : null;
            Debug.Log("[CamDbg] playerYaw=" + (player != null ? player.eulerAngles.y.ToString("F0") : "?")
                      + " playerPos=" + (player != null ? player.position.ToString("F1") : "?")
                      + " camYaw=" + (camT != null ? camT.eulerAngles.y.ToString("F0") : "?")
                      + " camPos=" + (camT != null ? camT.position.ToString("F1") : "?")
                      + " dist=" + (follow != null ? follow.CameraDistance.ToString("F1") : "-"));
        }
#endif
    }

    public void Shake(float amount)
    {
        shake = Mathf.Clamp(shake + amount, 0f, 1f);
    }

    public void SetDistance(float metres)
    {
        distance = Mathf.Clamp(metres, 2.5f, 8f);
        if (follow != null) follow.CameraDistance = distance;
    }
}
