// 职责：编辑器菜单，修复地形图层贴图
// 依赖：无
using UnityEditor;
using UnityEngine;

// Menu: Wilderness -> 3. Fix Terrain Layers
// Re-binds the grass/dirt textures onto the terrain layers (the checkerboard
// pattern means diffuseTexture was null).
public static class TerrainFix
{
    const string GrassTex = "Assets/Textures/grass_base.png";
    const string DirtTex = "Assets/Textures/dirt_base.png";

    [MenuItem("Wilderness/3. Fix Terrain Layers")]
    public static void Fix()
    {
        var t = Object.FindObjectOfType<Terrain>();
        if (t == null) { Debug.Log("[TerrainFix] no Terrain in scene"); return; }

        var data = t.terrainData;
        var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTex);
        var dirt = AssetDatabase.LoadAssetAtPath<Texture2D>(DirtTex);
        Debug.Log("[TerrainFix] grass tex = " + (grass ? grass.name : "NULL") + ", dirt tex = " + (dirt ? dirt.name : "NULL"));

        var layers = data.terrainLayers;
        Debug.Log("[TerrainFix] existing layer count = " + layers.Length);
        for (int i = 0; i < layers.Length; i++)
        {
            var L = layers[i];
            Debug.Log("[TerrainFix] layer[" + i + "] name=" + L.name
                      + " tex=" + (L.diffuseTexture ? L.diffuseTexture.name : "NULL")
                      + " tile=" + L.tileSize);
        }

        if (layers.Length < 2)
        {
            // rebuild both layers from scratch
            var g = NewLayer(grass, "Grass", 18f);
            var d = NewLayer(dirt, "Dirt", 12f);
            data.terrainLayers = new[] { g, d };
            data.alphamapResolution = 512;
            Debug.Log("[TerrainFix] rebuilt layers from textures");
        }
        else
        {
            if (layers[0].diffuseTexture == null) { layers[0].diffuseTexture = grass; layers[0].tileSize = new Vector2(18f, 18f); EditorUtility.SetDirty(layers[0]); Debug.Log("[TerrainFix] grass bound to layer 0"); }
            if (layers.Length > 1 && layers[1].diffuseTexture == null) { layers[1].diffuseTexture = dirt; layers[1].tileSize = new Vector2(12f, 12f); EditorUtility.SetDirty(layers[1]); Debug.Log("[TerrainFix] dirt bound to layer 1"); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        t.Flush();
        Debug.Log("[TerrainFix] DONE");
    }

    static TerrainLayer NewLayer(Texture2D tex, string name, float tile)
    {
        var path = "Assets/Settings/" + name + ".terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }
        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(tile, tile);
        layer.name = name;
        EditorUtility.SetDirty(layer);
        return layer;
    }
}
