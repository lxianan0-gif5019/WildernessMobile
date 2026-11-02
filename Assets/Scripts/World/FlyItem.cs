// 职责：拾取时飞向背包的补间小方块
// 依赖：无
using UnityEngine;

// The little "item flies into your backpack" flourish shown when a drop is picked up.
public class FlyItem : MonoBehaviour
{
    static Material shared;

    Vector3 start;
    Vector3 end;
    float t;
    const float Duration = 0.28f;

    public static void Spawn(Vector3 from, Vector3 to, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "FlyItem";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.2f;

        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shared == null) shared = new Material(shader);
        var mat = new Material(shared) { color = color };
        r.sharedMaterial = mat;

        var f = go.AddComponent<FlyItem>();
        f.start = from;
        f.end = to;
    }

    void Update()
    {
        t += Time.deltaTime / Duration;
        float e = Mathf.Clamp01(t);
        e = e * e;                                        // ease in: slow, then snaps in
        transform.position = Vector3.Lerp(start, end, e);
        transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.02f, e);
        transform.Rotate(Vector3.up, 900f * Time.deltaTime);

        if (t >= 1f) Destroy(gameObject);
    }
}
