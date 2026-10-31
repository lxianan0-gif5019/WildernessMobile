using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-shot project setup for WildernessMobile.
// Run headless:  Unity.exe -projectPath <p> -batchmode -quit -executeMethod ProjectSetup.Run
public static class ProjectSetup
{
    const string SettingsDir = "Assets/Settings";

    public static void Run()
    {
        try
        {
            Log("=== ProjectSetup START ===");
            CreatePipeline();
            ConfigureQuality();
            ConfigurePlayer();
            SwitchToAndroid();
            Log("=== ProjectSetup DONE ===");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Log("FATAL: " + e);
            EditorApplication.Exit(1);
        }
    }

    static void CreatePipeline()
    {
        if (!AssetDatabase.IsValidFolder(SettingsDir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.name = "URP-Renderer";
        AssetDatabase.CreateAsset(rendererData, SettingsDir + "/URP-Renderer.asset");
        Log("renderer data created");

        var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
        pipeline.name = "URP-Mobile";
        AssetDatabase.CreateAsset(pipeline, SettingsDir + "/URP-Mobile.asset");
        Log("pipeline asset created");

        // Mobile-friendly baseline for a 780M-class iGPU / mid-range Android phone.
        var so = new SerializedObject(pipeline);
        SetInt(so, "m_MSAA", 2);
        SetFloat(so, "m_RenderScale", 1.0f);
        SetBool(so, "m_SupportsHDR", false);
        SetFloat(so, "m_ShadowDistance", 30f);
        SetInt(so, "m_ShadowCascadeCount", 1);
        SetInt(so, "m_MainLightShadowmapResolution", 1024);
        SetBool(so, "m_SoftShadowsSupported", false);
        so.ApplyModifiedPropertiesWithoutUndo();
        Log("pipeline tuned (MSAA 2x, HDR off, 1 cascade, 30m shadows)");

        GraphicsSettings.defaultRenderPipeline = pipeline;
        Log("GraphicsSettings.defaultRenderPipeline assigned");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void ConfigureQuality()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SettingsDir + "/URP-Mobile.asset");
        if (pipeline == null) { Log("WARN: pipeline asset not found for quality setup"); return; }

        int original = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 0;
        }
        QualitySettings.SetQualityLevel(original, false);
        Log("quality levels assigned (" + QualitySettings.names.Length + ")");
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Wilderness";
        PlayerSettings.productName = "WildernessMobile";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.wilderness.mobile");

        PlayerSettings.colorSpace = ColorSpace.Linear;

        // Landscape only
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // Android specifics (IL2CPP + ARM64 is mandatory for modern devices / Play Store)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;

        Log("player settings configured (IL2CPP, ARM64, landscape, SDK 22/33)");
    }

    static void SwitchToAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            Log("already on Android");
            return;
        }
        bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        Log("SwitchActiveBuildTarget -> " + ok + " (now " + EditorUserBuildSettings.activeBuildTarget + ")");
        if (!ok) throw new Exception("SwitchActiveBuildTarget failed - is Android Build Support installed?");
    }

    // ---- serialized helpers -------------------------------------------------
    static void SetBool(SerializedObject so, string prop, bool value)
    {
        var p = so.FindProperty(prop);
        if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
        else Log("WARN: bool prop missing " + prop);
    }
    static void SetInt(SerializedObject so, string prop, int value)
    {
        var p = so.FindProperty(prop);
        if (p != null && p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
        else Log("WARN: int prop missing " + prop);
    }
    static void SetFloat(SerializedObject so, string prop, float value)
    {
        var p = so.FindProperty(prop);
        if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        else Log("WARN: float prop missing " + prop);
    }
    static void Log(string msg) { Debug.Log("[ProjectSetup] " + msg); }
}
