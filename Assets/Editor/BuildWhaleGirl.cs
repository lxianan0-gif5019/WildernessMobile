// 职责：程序化生成一个低多边形"鲸鱼娘"角色预制体到 Resources/PlayerModel.prefab
// 依赖：PlayerVisual（运行时自动替换胶囊）
using UnityEditor;
using UnityEngine;

// Menu: Wilderness -> 42. Build Whale Girl Character
// Builds a stylised whale-girl out of primitives: blue/white palette, fin ears and a
// tail. Original geometry, so it is safe to ship. Drops it where PlayerVisual looks.
public static class BuildWhaleGirl
{
    const string ResourceDir = "Assets/Resources";
    const string PrefabPath = "Assets/Resources/PlayerModel.prefab";
    const string MatDir = "Assets/Materials";

    static Color skin = new Color(0.96f, 0.97f, 1.00f);      // pale white
    static Color accent = new Color(0.25f, 0.78f, 0.88f);    // cyan
    static Color hair = new Color(0.12f, 0.44f, 0.66f);      // deep blue
    static Color tail = new Color(0.20f, 0.62f, 0.80f);      // mid blue

    [MenuItem("Wilderness/42. Build Whale Girl Character")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(ResourceDir)) AssetDatabase.CreateFolder("Assets", "Resources");

        var root = new GameObject("PlayerModel");

        var skinMat = Mat("WhaleSkin", skin);
        var accentMat = Mat("WhaleAccent", accent);
        var hairMat = Mat("WhaleHair", hair);
        var tailMat = Mat("WhaleTail", tail);

        // --- body (feet at y = 0, head near 1.7 so it matches the capsule height)
        Part(root, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.52f, 0.42f, 0.36f), skinMat);

        Part(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.34f, 0.34f, 0.34f), skinMat);

        // hair cap, slightly behind and above the head
        Part(root, "Hair", PrimitiveType.Sphere, new Vector3(0f, 1.68f, -0.03f), new Vector3(0.38f, 0.32f, 0.38f), hairMat);

        // fin ears
        var earL = Part(root, "FinEarL", PrimitiveType.Cube, new Vector3(-0.21f, 1.66f, -0.02f), new Vector3(0.05f, 0.22f, 0.16f), accentMat);
        earL.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        var earR = Part(root, "FinEarR", PrimitiveType.Cube, new Vector3(0.21f, 1.66f, -0.02f), new Vector3(0.05f, 0.22f, 0.16f), accentMat);
        earR.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);

        // arms
        var armL = Part(root, "ArmL", PrimitiveType.Capsule, new Vector3(-0.33f, 1.12f, 0f), new Vector3(0.13f, 0.30f, 0.13f), skinMat);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        var armR = Part(root, "ArmR", PrimitiveType.Capsule, new Vector3(0.33f, 1.12f, 0f), new Vector3(0.13f, 0.30f, 0.13f), skinMat);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

        // legs
        Part(root, "LegL", PrimitiveType.Capsule, new Vector3(-0.14f, 0.42f, 0f), new Vector3(0.16f, 0.34f, 0.16f), skinMat);
        Part(root, "LegR", PrimitiveType.Capsule, new Vector3(0.14f, 0.42f, 0f), new Vector3(0.16f, 0.34f, 0.16f), skinMat);

        // --- whale tail: three tapering segments sweeping back and down
        var t1 = Part(root, "Tail1", PrimitiveType.Capsule, new Vector3(0f, 0.92f, -0.28f), new Vector3(0.20f, 0.22f, 0.20f), tailMat);
        t1.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        var t2 = Part(root, "Tail2", PrimitiveType.Capsule, new Vector3(0f, 0.72f, -0.52f), new Vector3(0.16f, 0.20f, 0.16f), tailMat);
        t2.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
        var t3 = Part(root, "Tail3", PrimitiveType.Capsule, new Vector3(0f, 0.56f, -0.74f), new Vector3(0.12f, 0.18f, 0.12f), tailMat);
        t3.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);

        // tail fluke: two flattened blades
        var flukeL = Part(root, "FlukeL", PrimitiveType.Cube, new Vector3(-0.16f, 0.50f, -0.90f), new Vector3(0.28f, 0.03f, 0.16f), accentMat);
        flukeL.transform.localRotation = Quaternion.Euler(10f, 22f, 0f);
        var flukeR = Part(root, "FlukeR", PrimitiveType.Cube, new Vector3(0.16f, 0.50f, -0.90f), new Vector3(0.28f, 0.03f, 0.16f), accentMat);
        flukeR.transform.localRotation = Quaternion.Euler(10f, -22f, 0f);

        // dorsal fin on the back
        var fin = Part(root, "DorsalFin", PrimitiveType.Cube, new Vector3(0f, 1.22f, -0.22f), new Vector3(0.06f, 0.20f, 0.22f), accentMat);
        fin.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        // eyes
        Part(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.11f, 1.63f, 0.14f), new Vector3(0.055f, 0.07f, 0.03f), Mat("WhaleEye", new Color(0.05f, 0.08f, 0.12f)));
        Part(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.11f, 1.63f, 0.14f), new Vector3(0.055f, 0.07f, 0.03f), Mat("WhaleEye", new Color(0.05f, 0.08f, 0.12f)));

        // --- save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[WhaleGirl] prefab saved -> " + PrefabPath
                  + " (parts=" + 15 + ") size=" + (prefab != null));
    }

    static GameObject Part(GameObject parent, string name, PrimitiveType type,
                           Vector3 localPos, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material Mat(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets", "Materials");
        var path = MatDir + "/" + name + ".mat";

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.shader = shader;
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
