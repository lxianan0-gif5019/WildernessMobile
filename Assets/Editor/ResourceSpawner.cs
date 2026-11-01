using UnityEngine;
using UnityEditor;

// Menu: Wilderness -> 7. Spawn Harvestables
// Scatters harvestable trees, rocks and bushes around the player. These are real
// GameObjects (unlike Terrain tree instances) so they can carry ResourceNode.
public static class ResourceSpawner
{
    const string RootName = "Harvestables";

    [MenuItem("Wilderness/7. Spawn Harvestables")]
    public static void Spawn()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject(RootName);
        var terrain = Terrain.activeTerrain;
        var player = GameObject.FindWithTag("Player");
        Vector3 centre = player != null ? player.transform.position : new Vector3(300f, 0f, 300f);

        var woodMat = Mat("HarvestWood", new Color(0.36f, 0.24f, 0.14f));
        var leafMat = Mat("HarvestLeaf", new Color(0.16f, 0.34f, 0.12f));
        var stoneMat = Mat("HarvestStone", new Color(0.52f, 0.52f, 0.55f));
        var bushMat = Mat("HarvestBush", new Color(0.28f, 0.45f, 0.18f));

        int trees = SpawnTrees(root.transform, terrain, centre, woodMat, leafMat, 45);
        int rocks = SpawnRocks(root.transform, terrain, centre, stoneMat, 30);
        int bushes = SpawnBushes(root.transform, terrain, centre, bushMat, 25);

        Debug.Log("[ResourceSpawner] trees=" + trees + " rocks=" + rocks + " bushes=" + bushes);
    }

    static Material Mat(string name, Color c)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Vector3 SampleGround(Terrain terrain, Vector3 centre, System.Random rnd, float minR, float maxR)
    {
        float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
        float radius = minR + (float)rnd.NextDouble() * (maxR - minR);
        float x = centre.x + Mathf.Cos(angle) * radius;
        float z = centre.z + Mathf.Sin(angle) * radius;

        float y = 0f;
        if (terrain != null)
            y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        return new Vector3(x, y, z);
    }

    static int SpawnTrees(Transform root, Terrain terrain, Vector3 centre, Material wood, Material leaf, int count)
    {
        var rnd = new System.Random(4242);
        for (int i = 0; i < count; i++)
        {
            var pos = SampleGround(terrain, centre, rnd, 8f, 110f);
            var go = new GameObject("Tree");
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            Object.DestroyImmediate(trunk.GetComponent<Collider>());
            trunk.transform.SetParent(go.transform, false);
            trunk.transform.localScale = new Vector3(0.35f, 1.6f, 0.35f);
            trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            trunk.GetComponent<Renderer>().sharedMaterial = wood;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            Object.DestroyImmediate(canopy.GetComponent<Collider>());
            canopy.transform.SetParent(go.transform, false);
            canopy.transform.localScale = new Vector3(2.6f, 3f, 2.6f);
            canopy.transform.localPosition = new Vector3(0f, 4f, 0f);
            canopy.GetComponent<Renderer>().sharedMaterial = leaf;

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.height = 4f;
            col.radius = 0.5f;

            var node = go.AddComponent<ResourceNode>();
            node.itemId = "wood";
            node.displayName = "Tree";
            node.hitsToHarvest = 3;
            node.amountPerHit = 2;
        }
        return count;
    }

    static int SpawnRocks(Transform root, Terrain terrain, Vector3 centre, Material stone, int count)
    {
        var rnd = new System.Random(777);
        for (int i = 0; i < count; i++)
        {
            var pos = SampleGround(terrain, centre, rnd, 6f, 110f);
            var go = new GameObject("Rock");
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            float s = 0.8f + (float)rnd.NextDouble() * 0.7f;
            body.transform.localScale = new Vector3(s, s * 0.8f, s);
            body.transform.localPosition = new Vector3(0f, s * 0.4f, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = stone;

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, s * 0.4f, 0f);
            col.size = new Vector3(s, s * 0.8f, s);

            var node = go.AddComponent<ResourceNode>();
            node.itemId = "stone";
            node.displayName = "Rock";
            node.hitsToHarvest = 4;
            node.amountPerHit = 1;
        }
        return count;
    }

    static int SpawnBushes(Transform root, Terrain terrain, Vector3 centre, Material bush, int count)
    {
        var rnd = new System.Random(999);
        for (int i = 0; i < count; i++)
        {
            var pos = SampleGround(terrain, centre, rnd, 5f, 100f);
            var go = new GameObject("Bush");
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(1.6f, 1.1f, 1.6f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = bush;

            var col = go.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.radius = 0.8f;

            var node = go.AddComponent<ResourceNode>();
            node.itemId = "fiber";
            node.displayName = "Bush";
            node.hitsToHarvest = 1;
            node.amountPerHit = 3;
        }
        return count;
    }
}
