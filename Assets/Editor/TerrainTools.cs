using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the whole forest in one shot.
//   Menu:  Wilderness -> 2. Build Forest
//   Batch: -executeMethod TerrainTools.BuildForest
public static class TerrainTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string TreePath = "Assets/Prefabs/SimpleTree.prefab";
    const string GrassLayer = "Assets/Scenes/NewLayer.terrainlayer";
    const string DirtLayer = "Assets/Scenes/NewLayer 1.terrainlayer";
    const string GrassTex = "Assets/Textures/grass_base.png";

    [MenuItem("Wilderness/2. Build Forest")]
    public static void BuildForest()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[TerrainTools] scene opened");

            CleanupTestObjects();
            var terrain = GetOrCreateTerrain();
            var data = terrain.terrainData;
            Debug.Log("[TerrainTools] terrain size = " + data.size + " heights = " + data.heightmapResolution);

            GenerateHills(data);
            Debug.Log("[TerrainTools] hills generated");

            ApplyLayers(data);
            Debug.Log("[TerrainTools] layers applied: " + data.terrainLayers.Length);

            PaintGrass(data);
            Debug.Log("[TerrainTools] grass coverage set");

            ScatterTrees(terrain, 420);
            Debug.Log("[TerrainTools] trees placed: " + data.treeInstanceCount);

            PlaceCamera(terrain);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[TerrainTools] BUILD FOREST OK");
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.Log("[TerrainTools] FAILED: " + e);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    // -------------------------------------------------------------- scene tidy
    static void CleanupTestObjects()
    {
        foreach (var name in new[] { "Ground", "ReferenceCube" })
        {
            var go = GameObject.Find(name);
            if (go != null) { UnityEngine.Object.DestroyImmediate(go); Debug.Log("[TerrainTools] removed " + name); }
        }
        // neighbour terrains created by accident
        foreach (var t in UnityEngine.Object.FindObjectsOfType<Terrain>())
        {
            if (t.gameObject.name.StartsWith("Terrain_"))
            {
                UnityEngine.Object.DestroyImmediate(t.gameObject);
                Debug.Log("[TerrainTools] removed neighbour " + t.name);
            }
        }
    }

    static Terrain GetOrCreateTerrain()
    {
        var t = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (t == null)
        {
            var go = Terrain.CreateTerrainGameObject(new TerrainData());
            go.name = "Terrain";
            t = go.GetComponent<Terrain>();
        }

        var data = t.terrainData;
        // Sanity: a huge terrain produces a meaningless heightmap (metres per texel).
        // 600 x 600 m with a 513 heightmap = ~1.17 m per texel, right for a walkable forest.
        data.heightmapResolution = 513;
        data.alphamapResolution = 512;
        data.baseMapResolution = 1024;
        data.size = new Vector3(600f, 90f, 600f);
        Debug.Log("[TerrainTools] normalised terrain size = " + data.size);
        return t;
    }

    static bool IsFlat(TerrainData data)
    {
        var h = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
        for (int y = 0; y < h.GetLength(0); y += 8)
            for (int x = 0; x < h.GetLength(1); x += 8)
                if (h[y, x] > 0.002f) return false;
        return true;
    }

    static void GenerateHills(TerrainData data)
    {
        int res = data.heightmapResolution;
        var heights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / res, v = (float)y / res;
                float n = Mathf.PerlinNoise(u * 3.1f + 11.3f, v * 3.1f + 7.7f) * 0.55f
                        + Mathf.PerlinNoise(u * 7.7f + 3.1f, v * 7.7f + 19.2f) * 0.30f
                        + Mathf.PerlinNoise(u * 15.3f + 41f, v * 15.3f + 5f) * 0.15f;
                heights[y, x] = Mathf.Clamp01(n * 0.85f);
            }
        }
        data.SetHeights(0, 0, heights);
    }

    static void ApplyLayers(TerrainData data)
    {
        var grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassLayer);
        var dirt = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DirtLayer);

        if (grass == null)
        {
            grass = MakeLayer(GrassTex, "Assets/Settings/Grass.terrainlayer", 18f);
        }
        var layers = new List<TerrainLayer> { grass };
        if (dirt != null) layers.Add(dirt);
        else
        {
            var dirtTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/dirt_base.png");
            if (dirtTex != null) layers.Add(MakeLayer("Assets/Textures/dirt_base.png", "Assets/Settings/Dirt.terrainlayer", 12f));
        }
        data.terrainLayers = layers.ToArray();
    }

    static TerrainLayer MakeLayer(string texPath, string assetPath, float tile)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(tile, tile) };
        AssetDatabase.CreateAsset(layer, assetPath);
        return layer;
    }

    static void PaintGrass(TerrainData data)
    {
        int res = data.alphamapResolution;
        int layers = Mathf.Max(1, data.terrainLayers.Length);
        var map = new float[res, res, layers];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / res, v = (float)y / res;
                // steep slopes get dirt, flat ground gets grass
                Vector3 normal = data.GetInterpolatedNormal(u, v);
                float flatness = Mathf.Clamp01((normal.y - 0.72f) / 0.25f);
                float dirtAmount = layers > 1 ? (1f - flatness) : 0f;
                float noise = Mathf.PerlinNoise(u * 20f, v * 20f) * 0.15f;
                float grassAmount = Mathf.Clamp01(1f - dirtAmount + noise - 0.05f);
                map[y, x, 0] = grassAmount;
                if (layers > 1) map[y, x, 1] = 1f - grassAmount;
            }
        }
        data.SetAlphamaps(0, 0, map);
    }

    static void PlaceCamera(Terrain terrain)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var y = terrain.transform.position;
        Vector3 centre = y + new Vector3(terrain.terrainData.size.x * 0.5f, 0f, terrain.terrainData.size.z * 0.5f);
        float ground = terrain.terrainData.GetInterpolatedHeight(0.3f, 0.3f) + y.y;
        cam.transform.position = new Vector3(centre.x - 120f, ground + 45f, centre.z - 120f);
        cam.transform.LookAt(new Vector3(centre.x, ground + 8f, centre.z));
        Debug.Log("[TerrainTools] camera placed at " + cam.transform.position);
    }

    static void ScatterTrees(Terrain terrain, int count)
    {
        var data = terrain.terrainData;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePath);
        if (prefab == null) { Debug.Log("[TerrainTools] WARN tree prefab missing"); return; }

        data.treePrototypes = new[] { new TreePrototype { prefab = prefab, bendFactor = 0.1f } };
        data.RefreshPrototypes();

        var instances = new List<TreeInstance>();
        var rnd = new System.Random(20261101);
        float sx = data.size.x, sz = data.size.z;

        int guard = 0;
        while (instances.Count < count && guard < count * 40)
        {
            guard++;
            float u = (float)rnd.NextDouble();
            float v = (float)rnd.NextDouble();
            Vector3 normal = data.GetInterpolatedNormal(u, v);
            if (normal.y < 0.78f) continue;              // no trees on cliffs
            float height01 = data.GetInterpolatedHeight(u, v) / data.size.y;
            if (height01 > 0.82f) continue;              // no trees on peaks

            instances.Add(new TreeInstance
            {
                prototypeIndex = 0,
                position = new Vector3(u, height01, v),
                widthScale = 0.8f + (float)rnd.NextDouble() * 0.6f,
                heightScale = 0.85f + (float)rnd.NextDouble() * 0.7f,
                rotation = (float)rnd.NextDouble() * Mathf.PI * 2f,
                color = Color.white,
                lightmapColor = Color.white
            });
        }

        data.treeInstances = instances.ToArray();
        terrain.Flush();
    }
}
