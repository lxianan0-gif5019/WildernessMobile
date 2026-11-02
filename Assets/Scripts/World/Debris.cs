using System.Collections.Generic;
using UnityEngine;

// Cheap code-made particles: a few small cubes that fly out and fade.
// No ParticleSystem, no assets, pooled.
public class Debris : MonoBehaviour
{
    static readonly Stack<Debris> pool = new Stack<Debris>();
    static Transform root;

    Vector3 velocity;
    float life;
    float maxLife;
    float gravity;
    Renderer[] parts;
    Material mat;

    public static void Burst(Vector3 position, Color color, int count = 6, float speed = 3f)
    {
        for (int i = 0; i < count; i++)
        {
            var d = pool.Count > 0 ? pool.Pop() : Create();
            d.gameObject.SetActive(true);
            d.transform.position = position + Random.insideUnitSphere * 0.15f;
            d.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
            d.velocity = (Random.insideUnitSphere + Vector3.up * 0.8f).normalized * Random.Range(speed * 0.5f, speed);
            d.maxLife = Random.Range(0.4f, 0.75f);
            d.life = d.maxLife;
            d.gravity = 9f;
            d.SetColor(color);
        }
    }

    static Debris Create()
    {
        if (root == null)
        {
            var go = GameObject.Find("_DebrisPool");
            if (go == null) go = new GameObject("_DebrisPool");
            root = go.transform;
        }

        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "Debris";
        Destroy(obj.GetComponent<Collider>());
        obj.transform.SetParent(root, false);
        obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var d = obj.AddComponent<Debris>();
        d.parts = obj.GetComponentsInChildren<Renderer>();
        return d;
    }

    void SetColor(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (mat == null) mat = new Material(shader);
        mat.color = color;
        if (parts != null)
            foreach (var r in parts) r.sharedMaterial = mat;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Release(); return; }

        velocity += Vector3.down * gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(Vector3.one * 220f * Time.deltaTime);

        float t = life / maxLife;
        transform.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.12f, t);
    }

    void Release()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
