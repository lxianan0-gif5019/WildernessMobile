using UnityEditor;
using UnityEngine;

// Menu: Wilderness -> 1. Create Simple Tree Prefab
// Builds a placeholder tree (cylinder trunk + sphere canopy) as a prefab that the
// Terrain "Paint Trees" tool can paint. Replace it later with real tree models.
public static class TreeBuilder
{
    [MenuItem("Wilderness/1. Create Simple Tree Prefab")]
    public static void CreateTree()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var trunkMat = new Material(shader) { name = "TreeTrunk" };
        SetColor(trunkMat, new Color(0.36f, 0.24f, 0.14f));
        SafeCreateAsset(trunkMat, "Assets/Materials/TreeTrunk.mat");

        var leafMat = new Material(shader) { name = "TreeLeaf" };
        SetColor(leafMat, new Color(0.16f, 0.34f, 0.12f));
        SafeCreateAsset(leafMat, "Assets/Materials/TreeLeaf.mat");

        var root = new GameObject("SimpleTree");

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.3f, 1.6f, 0.3f);
        trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        StripCollider(trunk);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(root.transform, false);
        canopy.transform.localScale = new Vector3(2.4f, 2.8f, 2.4f);
        canopy.transform.localPosition = new Vector3(0f, 3.9f, 0f);
        StripCollider(canopy);
        canopy.GetComponent<Renderer>().sharedMaterial = leafMat;

        // A LODGroup keeps the Terrain tree tool happy (it expects one for billboards).
        var lod = root.AddComponent<LODGroup>();
        var renderers = root.GetComponentsInChildren<Renderer>();
        lod.SetLODs(new[] { new LOD(0.02f, renderers) });
        lod.RecalculateBounds();

        const string prefabPath = "Assets/Prefabs/SimpleTree.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TreeBuilder] created " + prefabPath + " - now use Terrain > Paint Trees > Edit Trees > Add Tree");
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    static void SafeCreateAsset(Object asset, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }
}
