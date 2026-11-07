// 职责：把 Cinemachine 第三人称 vcam 装配进 Main 场景并保存（一次性）
// 依赖：Cinemachine、CameraRig、ThirdPersonController
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CameraSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string VcamName = "CM ThirdPerson";

    [MenuItem("Wilderness/41. Setup Cinemachine Camera")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // The camera collides with anything in its filter - including the player's own
        // capsule, which collapses the arm to zero. Put the player on its own layer.
        int playerLayer = EnsureLayer("Player", 8);
        Debug.Log("[CamSetup] player layer = " + playerLayer + " (Player)");

        var cam = Camera.main;
        if (cam == null) { Debug.Log("[CamSetup] no Main Camera"); return; }

        var controller = Object.FindObjectOfType<ThirdPersonController>();
        if (controller == null) { Debug.Log("[CamSetup] no ThirdPersonController"); return; }

        SetLayerRecursive(controller.gameObject, playerLayer);

        // --- brain on the camera, hand-written camera off
        if (cam.GetComponent<CinemachineBrain>() == null) cam.gameObject.AddComponent<CinemachineBrain>();
        var old = cam.GetComponent<ThirdPersonCamera>();
        if (old != null) old.enabled = false;

        // --- vcam as a real scene object
        var vcamGo = GameObject.Find(VcamName);
        if (vcamGo == null) vcamGo = new GameObject(VcamName);

        var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
        if (vcam == null) vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();

        vcamGo.transform.position = controller.transform.position + Vector3.up * 1.6f;
        vcamGo.transform.rotation = Quaternion.Euler(16f, controller.transform.eulerAngles.y, 0f);

        vcam.Priority = 20;

        // Follow a point at head height, not the character's origin (which is at the
        // feet) - otherwise the whole arm pivots around the ankles.
        var pivot = controller.transform.Find("CameraPivot");
        if (pivot == null)
        {
            var pivotGo = new GameObject("CameraPivot");
            pivotGo.transform.SetParent(controller.transform, false);
            pivot = pivotGo.transform;
        }
        pivot.localPosition = new Vector3(0f, 1.5f, 0f);
        pivot.localRotation = Quaternion.identity;
        pivot.gameObject.layer = playerLayer;

        vcam.Follow = pivot;

        var follow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (follow == null) follow = vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        follow.CameraDistance = 4.5f;
        follow.ShoulderOffset = new Vector3(0.3f, 0f, 0f);
        follow.VerticalArmLength = 0f;
        follow.Damping = new Vector3(0.12f, 0.12f, 0.12f);
        follow.CameraCollisionFilter = ~(1 << playerLayer);
        follow.CameraRadius = 0.3f;
        follow.DampingIntoCollision = 0.05f;
        follow.DampingFromCollision = 0.4f;

        // --- driver
        var rig = cam.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
        rig.takeOverCamera = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CamSetup] saved vcam '" + VcamName + "' into " + ScenePath
                  + " follow=" + controller.name
                  + " dist=" + follow.CameraDistance);
    }

    /// <summary>Returns the layer index for a named layer, creating it if needed.</summary>
    static int EnsureLayer(string name, int preferredIndex)
    {
        for (int i = 0; i < 32; i++)
            if (LayerMask.LayerToName(i) == name) return i;

        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return preferredIndex;

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");
        if (layers == null) return preferredIndex;

        for (int i = preferredIndex; i < 32 && i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = name;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }
        return preferredIndex;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
