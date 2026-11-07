// 职责：配置 Tripo 角色 FBX 的导入设置（人形骨骼 + 导入动画）
// 依赖：Assets/Resources/PlayerModel.fbx
using UnityEditor;
using UnityEngine;

public static class ConfigureCharacterModel
{
    const string ModelPath = "Assets/Resources/PlayerModel.fbx";

    [MenuItem("Wilderness/47. Configure Character Import")]
    public static void Configure()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) { Debug.LogError("[Model] no ModelImporter at " + ModelPath); return; }

        importer.animationType = ModelImporterAnimationType.Humanoid;
        importer.importAnimation = true;
        importer.importBlendShapes = true;
        importer.importNormals = ModelImporterNormals.Import;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();

        Debug.Log("[Model] import configured: Humanoid, animations on");
    }
}
