// 职责：把占位胶囊换成正式角色模型（有模型就换，没有就保持胶囊）
// 依赖：InputHub（驱动移动动画参数）
using UnityEngine;

// Drop a humanoid prefab at Assets/Resources/PlayerModel.prefab and it replaces the
// capsule automatically. Nothing else in the game needs to change.
public class PlayerVisual : MonoBehaviour
{
    public static PlayerVisual Instance;
    [Tooltip("Optional explicit prefab. Leave empty to auto-load Resources/PlayerModel.")]
    public GameObject modelPrefab;
    public string resourcePath = "PlayerModel";

    [Header("Animation")]
    [Tooltip("Animator float driven with the current move speed (0 = idle, 1 = run).")]
    public string speedParameter = "Speed";

    [Header("Fitting")]
    [Tooltip("The model is rescaled to this height, whatever units the source file used.")]
    public float targetHeight = 1.65f;
    [Tooltip("Rotate the model if it does not face +Z in Unity.")]
    public float modelYaw = 0f;
    [Tooltip("Explicit model rotation in degrees. Overrides auto-detection when set.")]
    public Vector3 modelRotation = new Vector3(0f, 0f, 90f);
    [Tooltip("Guess whether the source is Z-up. Off for Tripo/glTF models, which are Y-up.")]
    public bool autoDetectUp = false;

    GameObject placeholder;
    GameObject model;
    Animator animator;

    void Start()
    {
        Instance = this;
        var prefab = modelPrefab != null ? modelPrefab : Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.Log("[PlayerVisual] no model found (Resources/" + resourcePath + "), keeping the capsule");
            return;
        }

        placeholder = transform.Find("Body") != null ? transform.Find("Body").gameObject : null;
        if (placeholder != null) placeholder.SetActive(false);

        model = Instantiate(prefab, transform);
        model.name = "PlayerModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Skinned meshes ignore their own transform for bounds, so measure the actual
        // vertices and pick whichever orientation makes the character tallest.
        model.transform.localRotation = PickUpright(model) * Quaternion.Euler(modelRotation.x, modelRotation.y + modelYaw, modelRotation.z);

        // FBX/glTF often arrive 100x too big or too small; normalise instead of guessing.
        FitToHeight(model, targetHeight);

        // Keep the player's layer so the camera collision filter ignores the model.
        SetLayerRecursive(model, gameObject.layer);

        animator = model.GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            var ctrl = Resources.Load<RuntimeAnimatorController>("WhaleGirl");
            if (ctrl != null) animator.runtimeAnimatorController = ctrl;
        }

        Debug.Log("[PlayerVisual] model applied: " + prefab.name
                  + " animator=" + (animator != null ? "yes" : "no")
                  + " controller=" + (animator != null && animator.runtimeAnimatorController != null ? "yes" : "no")
                  + " height=" + targetHeight + "m");
    }

    /// <summary>Plays the chop/swing animation once.</summary>
    public void TriggerSlash()
    {
        if (animator != null) animator.SetTrigger("Slash");
    }

    /// <summary>Plays the jump animation once.</summary>
    public void TriggerJump()
    {
        if (animator != null) animator.SetTrigger("Jump");
    }

    void Update()
    {
        if (animator == null) return;

        // 0..1 move amount, smoothed so the blend tree does not pop.
        float speed = Mathf.Clamp01(new Vector2(InputHub.Move.x, InputHub.Move.y).magnitude);
        animator.SetFloat(speedParameter, speed, 0.12f, Time.deltaTime);
    }

    static void SetLayerRecursive(GameObject go, int layer)    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    /// <summary>Scales the model to the wanted height and puts its feet on the origin.</summary>
    void FitToHeight(GameObject go, float height)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = BoundsOf(renderers);
        if (b.size.y < 0.0001f) return;

        go.transform.localScale *= height / b.size.y;

        b = BoundsOf(renderers);
        float drop = b.min.y - transform.position.y;
        go.transform.localPosition -= new Vector3(0f, drop, 0f);

        Debug.Log("[PlayerVisual] fitted size=" + b.size.ToString("F2"));
    }

    static Bounds BoundsOf(Renderer[] renderers)
    {
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    /// <summary>
    /// Reads the actual vertices and returns the rotation that makes the model tallest
    /// along the world Y axis. Works for skinned meshes, whose renderer bounds lie.
    /// </summary>
    static Quaternion PickUpright(GameObject model)
    {
        Vector3 pivot = model.transform.position;

        var points = new System.Collections.Generic.List<Vector3>();
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var m = mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices) points.Add(m.MultiplyPoint3x4(v));
        }
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh == null) continue;
            var m = smr.transform.localToWorldMatrix;
            foreach (var v in smr.sharedMesh.vertices) points.Add(m.MultiplyPoint3x4(v));
        }

        if (points.Count < 8) return Quaternion.identity;

        var candidates = new[]
        {
            Quaternion.identity,
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, 180f),
            Quaternion.Euler(0f, 0f, 270f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
        };

        Quaternion best = Quaternion.identity;
        float bestHeight = -1f;

        foreach (var q in candidates)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                float y = (q * (points[i] - pivot)).y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            float h = maxY - minY;
            if (h > bestHeight) { bestHeight = h; best = q; }
        }

        Debug.Log("[PlayerVisual] upright pick=" + best.eulerAngles.ToString("F0") + " height=" + bestHeight.ToString("F2"));
        return best;
    }
}
