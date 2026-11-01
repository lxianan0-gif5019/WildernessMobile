using UnityEngine;
using UnityEditor;

// Menu: Wilderness -> 6. Build Player Body
// Gives the Player a visible capsule child (no collider, the CharacterController
// already handles collision) so the third-person view has something to look at.
public static class BuildPlayerBody
{
    [MenuItem("Wilderness/6. Build Player Body")]
    public static void Build()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go.name == "Player") { player = go; break; }
        }
        if (player == null) { Debug.Log("[BuildPlayerBody] Player not found"); return; }

        var old = player.transform.Find("Body");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        var col = body.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        body.transform.localRotation = Quaternion.identity;

        // A bright colour so the character reads clearly against the forest.
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "PlayerBody" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.90f, 0.32f, 0.18f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.90f, 0.32f, 0.18f));
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, "Assets/Materials/PlayerBody.mat");
        body.GetComponent<Renderer>().sharedMaterial = mat;

        EditorUtility.SetDirty(player);
        Debug.Log("[BuildPlayerBody] body created under " + player.name);
    }
}
