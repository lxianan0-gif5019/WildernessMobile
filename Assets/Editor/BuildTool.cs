using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Headless helpers for WildernessMobile:
//   -executeMethod BuildTool.Prepare   (tune pipeline + create test scene)
//   -executeMethod BuildTool.BuildApk  (build a development APK)
public static class BuildTool
{
    const string SettingsDir = "Assets/Settings";
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string ApkPath = "Builds/WildernessMobile.apk";

    // ---------------------------------------------------------------- Prepare
    public static void Prepare()
    {
        try
        {
            TunePipeline();
            CreateScene();
            Debug.Log("[BuildTool] PREPARE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.Log("[BuildTool] PREPARE FAILED: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void TuneOnly()
    {
        try { TunePipeline(); Debug.Log("[BuildTool] TUNE OK"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.Log("[BuildTool] TUNE FAILED: " + e); EditorApplication.Exit(1); }
    }

    static void TunePipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SettingsDir + "/URP-Mobile.asset");
        if (pipeline == null) { Debug.Log("[BuildTool] WARN: pipeline asset missing"); return; }

        var so = new SerializedObject(pipeline);
        SetRaw(so, "m_MSAA", 1);                              // 0=off 1=2x 2=4x 3=8x
        SetEnumByName(so, "m_MainLightShadowmapResolution", "1024");
        SetInt(so, "m_ShadowCascadeCount", 1);
        SetFloat(so, "m_ShadowDistance", 30f);
        SetBool(so, "m_SoftShadowsSupported", false);
        SetBool(so, "m_SupportsHDR", false);
        so.ApplyModifiedPropertiesWithoutUndo();
        so.Update();
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildTool] readback MSAA=" + ReadRaw(so, "m_MSAA")
                  + " shadowRes=" + ReadRaw(so, "m_MainLightShadowmapResolution")
                  + " cascades=" + ReadRaw(so, "m_ShadowCascadeCount")
                  + " hdr=" + (so.FindProperty("m_SupportsHDR")?.boolValue ?? false));
    }

    static void CreateScene()
    {
        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "GroundMat" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.20f, 0.30f, 0.16f));
        else mat.color = new Color(0.20f, 0.30f, 0.16f);
        AssetDatabase.CreateAsset(mat, SettingsDir + "/GroundMat.mat");
        ground.GetComponent<Renderer>().sharedMaterial = mat;

        // A reference cube so the camera has something to look at
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ReferenceCube";
        cube.transform.position = new Vector3(0f, 1f, 4f);

        // Camera framing
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 3f, -6f);
            cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildTool] scene created: " + ScenePath);
    }

    // ------------------------------------------------------------------- Build
    public static void BuildApk()
    {
        try
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            Debug.Log("[BuildTool] BUILD START -> " + ApkPath);
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[BuildTool] BUILD RESULT: " + s.result
                      + " | errors=" + s.totalErrors
                      + " | warnings=" + s.totalWarnings
                      + " | size=" + s.totalSize
                      + " | time=" + s.totalTime);
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.Log("[BuildTool] BUILD EXCEPTION: " + e);
            EditorApplication.Exit(1);
        }
    }

    // Same build, but callable from the editor menu without quitting Unity.
    [MenuItem("Wilderness/16. Build APK")]
    public static void BuildApkFromMenu()
    {
        try
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            Debug.Log("[BuildTool] APK build started");
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[BuildTool] APK result: " + s.result + " | errors=" + s.totalErrors
                      + " | size=" + s.totalSize + " | time=" + s.totalTime);
        }
        catch (Exception e)
        {
            Debug.Log("[BuildTool] APK build failed: " + e);
        }
    }

    // ------------------------------------------------------------- helpers
    static void SetBool(SerializedObject so, string p, bool v)
    { var x = so.FindProperty(p); if (x != null) x.boolValue = v; else Debug.Log("[BuildTool] WARN missing " + p); }
    static void SetInt(SerializedObject so, string p, int v)
    {
        var x = so.FindProperty(p);
        if (x == null) { Debug.Log("[BuildTool] WARN missing " + p); return; }
        if (x.propertyType == SerializedPropertyType.Enum) x.enumValueIndex = v; else x.intValue = v;
    }
    static void SetEnum(SerializedObject so, string p, int v)
    {
        var x = so.FindProperty(p);
        if (x == null) { Debug.Log("[BuildTool] WARN missing " + p); return; }
        x.enumValueIndex = v;
        Debug.Log("[BuildTool] set " + p + " enumIndex=" + v + " (" + x.enumDisplayNames[x.enumValueIndex] + ")");
    }
    static void SetFloat(SerializedObject so, string p, float v)
    { var x = so.FindProperty(p); if (x != null) x.floatValue = v; else Debug.Log("[BuildTool] WARN missing " + p); }

    // Sets an int- or enum-backed property by its raw numeric value.
    static void SetRaw(SerializedObject so, string p, int v)
    {
        var x = so.FindProperty(p);
        if (x == null) { Debug.Log("[BuildTool] WARN missing " + p); return; }
        if (x.propertyType == SerializedPropertyType.Enum) x.enumValueIndex = v;
        else x.intValue = v;
        Debug.Log("[BuildTool] set " + p + " = " + v + " (type " + x.propertyType + ")");
    }

    static int ReadRaw(SerializedObject so, string p)
    {
        var x = so.FindProperty(p);
        if (x == null) return -1;
        return x.propertyType == SerializedPropertyType.Enum ? x.enumValueIndex : x.intValue;
    }

    // Picks the enum entry whose display name matches (so we never guess an index).
    static void SetEnumByName(SerializedObject so, string p, string wanted)
    {
        var x = so.FindProperty(p);
        if (x == null) { Debug.Log("[BuildTool] WARN missing " + p); return; }
        var names = x.enumDisplayNames;
        Debug.Log("[BuildTool] " + p + " options: " + string.Join(",", names));
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Replace(" ", "") == wanted)
            {
                x.enumValueIndex = i;
                Debug.Log("[BuildTool] set " + p + " -> " + names[i] + " (index " + i + ")");
                return;
            }
        }
        Debug.Log("[BuildTool] WARN no match for " + wanted + " in " + p);
    }
}
