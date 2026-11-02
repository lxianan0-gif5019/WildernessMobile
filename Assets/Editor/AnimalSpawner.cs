// 职责：编辑器菜单，生成野生动物
// 依赖：Animal
using UnityEngine;
using UnityEditor;

// Menu: Wilderness -> 21. Spawn Animals
// Scatters rabbits, deer and boars around the player. Simple placeholder bodies
// (body capsule + head) with a CapsuleCollider so the player can aim at them.
public static class AnimalSpawner
{
    const string RootName = "Wildlife";

    [MenuItem("Wilderness/21. Spawn Animals")]
    public static void Spawn()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject(RootName);
        Terrain terrain = Terrain.activeTerrain;
        var player = GameObject.FindWithTag("Player");
        Vector3 centre = player != null ? player.transform.position : new Vector3(300f, 0f, 300f);

        int rabbits = SpawnKind(root.transform, terrain, centre, Animal.Kind.Rabbit, 10, 12f, 70f, new Color(0.86f, 0.86f, 0.88f), 0.55f);
        int deer = SpawnKind(root.transform, terrain, centre, Animal.Kind.Deer, 12, 20f, 90f, new Color(0.55f, 0.38f, 0.24f), 1.05f);
        int boars = SpawnKind(root.transform, terrain, centre, Animal.Kind.Boar, 6, 25f, 90f, new Color(0.35f, 0.26f, 0.20f), 0.95f);

        Debug.Log("[AnimalSpawner] rabbits=" + rabbits + " deer=" + deer + " boars=" + boars);
    }

    static int SpawnKind(Transform root, Terrain terrain, Vector3 centre, Animal.Kind kind,
                         int count, float minR, float maxR, Color color, float scale)
    {
        var rnd = new System.Random(1000 + (int)kind * 97);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "Animal_" + kind };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        for (int i = 0; i < count; i++)
        {
            float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float radius = minR + (float)rnd.NextDouble() * (maxR - minR);
            float x = centre.x + Mathf.Cos(angle) * radius;
            float z = centre.z + Mathf.Sin(angle) * radius;
            float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : 0f;

            var go = new GameObject(kind.ToString());
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);

            // body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.6f * scale, 0.9f * scale, 0.6f * scale);
            body.transform.localPosition = new Vector3(0f, 0.55f * scale, 0f);
            body.GetComponent<Renderer>().sharedMaterial = mat;

            // head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, false);
            head.transform.localScale = Vector3.one * 0.45f * scale;
            head.transform.localPosition = new Vector3(0f, 0.75f * scale, 0.55f * scale);
            head.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.6f * scale, 0f);
            col.height = 1.2f * scale;
            col.radius = 0.45f * scale;

            var animal = go.AddComponent<Animal>();
            animal.kind = kind;
            animal.fightsBack = kind == Animal.Kind.Boar;
            switch (kind)
            {
                case Animal.Kind.Rabbit:
                    animal.displayName = "Rabbit";
                    animal.maxHealth = 20f;
                    animal.walkSpeed = 2.2f;
                    animal.runSpeed = 7f;
                    animal.fleeRange = 14f;
                    animal.dropAmount = 1;
                    break;
                case Animal.Kind.Deer:
                    animal.displayName = "Deer";
                    animal.maxHealth = 45f;
                    animal.walkSpeed = 1.8f;
                    animal.runSpeed = 6.5f;
                    animal.fleeRange = 12f;
                    animal.dropAmount = 2;
                    break;
                case Animal.Kind.Boar:
                    animal.displayName = "Boar";
                    animal.maxHealth = 70f;
                    animal.walkSpeed = 1.5f;
                    animal.runSpeed = 5.2f;
                    animal.fleeRange = 6f;
                    animal.dropAmount = 3;
                    animal.attackDamage = 9f;
                    break;
            }
        }
        return count;
    }
}
