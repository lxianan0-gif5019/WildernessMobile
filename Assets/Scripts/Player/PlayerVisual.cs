// 职责：把占位胶囊换成正式角色模型（有模型就换，没有就保持胶囊）
// 依赖：InputHub（驱动移动动画参数）
using UnityEngine;

// Drop a humanoid prefab at Assets/Resources/PlayerModel.prefab and it replaces the
// capsule automatically. Nothing else in the game needs to change.
public class PlayerVisual : MonoBehaviour
{
    [Tooltip("Optional explicit prefab. Leave empty to auto-load Resources/PlayerModel.")]
    public GameObject modelPrefab;
    public string resourcePath = "PlayerModel";

    [Header("Animation")]
    [Tooltip("Animator float driven with the current move speed (0 = idle, 1 = run).")]
    public string speedParameter = "Speed";

    GameObject placeholder;
    GameObject model;
    Animator animator;

    void Start()
    {
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

        // Keep the player's layer so the camera collision filter ignores the model.
        SetLayerRecursive(model, gameObject.layer);

        animator = model.GetComponentInChildren<Animator>();
        Debug.Log("[PlayerVisual] model applied: " + prefab.name
                  + " animator=" + (animator != null ? "yes" : "no"));
    }

    void Update()
    {
        if (animator == null) return;

        // 0..1 move amount, smoothed so the blend tree does not pop.
        float speed = Mathf.Clamp01(new Vector2(InputHub.Move.x, InputHub.Move.y).magnitude);
        animator.SetFloat(speedParameter, speed, 0.12f, Time.deltaTime);
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
