// 职责：编辑器菜单，生成树/石/灌木/铁矿/水池
// 依赖：ResourceNode、WaterSource
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
        var leafMat = Mat("HarvestLeaf", new Color(0.42f, 0.68f, 0.20f));
        var stoneMat = Mat("HarvestStone", new Color(0.52f, 0.52f, 0.55f));
        var bushMat = Mat("HarvestBush", new Color(0.28f, 0.45f, 0.18f));

        int trees = SpawnTrees(root.transform, terrain, centre, woodMat, leafMat, 60);
        int rocks = SpawnRocks(root.transform, terrain, centre, stoneMat, 30);
        int bushes = SpawnBushes(root.transform, terrain, centre, bushMat, 25);
        int iron = SpawnIron(root.transform, terrain, centre, Mat("HarvestIron", new Color(0.30f, 0.26f, 0.24f)), 12);
        SpawnWaterPool(root.transform, terrain, centre, Mat("WaterPool", new Color(0.18f, 0.45f, 0.75f)));

        Debug.Log("[ResourceSpawner] trees=" + trees + " rocks=" + rocks + " bushes=" + bushes + " iron=" + iron + " water=1");
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
            var pos = SampleGround(terrain, centre, rnd, 6f, 70f);
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
            node.itemId = ItemIds.Wood;
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
            node.itemId = ItemIds.Stone;
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
            node.itemId = ItemIds.Fiber;
            node.displayName = "Bush";
            node.hitsToHarvest = 1;
            node.amountPerHit = 3;
            node.extraItemId = ItemIds.Berry;
            node.extraAmount = 2;
        }
        return count;
    }

    // Iron veins: darker, huger rocks that need a stone pick.
    static int SpawnIron(Transform root, Terrain terrain, Vector3 centre, Material mat, int count)
    {
        var rnd = new System.Random(4207);
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float radius = 30f + (float)rnd.NextDouble() * 60f;
            float x = centre.x + Mathf.Cos(angle) * radius;
            float z = centre.z + Mathf.Sin(angle) * radius;
            float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : 0f;

            var go = new GameObject("IronVein");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(x, y, z);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(2.4f, 1.8f, 2.0f);
            body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 90f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(2.4f, 1.8f, 2.0f);

            var node = go.AddComponent<ResourceNode>();
            node.itemId = ItemIds.IronOre;
            node.displayName = "Iron Vein";
            node.hitsToHarvest = 6;
            node.amountPerHit = 1;
            node.requiredTool = ItemIds.StonePick;
            spawned++;
        }
        return spawned;
    }

    // A small pool close to the player so the thirst bar is survivable.
    static void SpawnWaterPool(Transform root, Terrain terrain, Vector3 centre, Material water)
    {
        Vector3 pos = centre + new Vector3(12f, 0f, 6f);
        if (terrain != null) pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;

        var go = new GameObject("WaterPool");
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(go.transform, false);
        body.transform.localScale = new Vector3(9f, 0.12f, 9f);
        body.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = water;

        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.1f, 0f);
        box.size = new Vector3(9f, 0.4f, 9f);

        var src = go.AddComponent<WaterSource>();
        src.displayName = "Water";
        src.refillAmount = 40f;
    }
}
