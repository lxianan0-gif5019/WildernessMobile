using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Wilderness -> 20. Open Main Scene
// Unity sometimes reopens an empty Untitled scene; this gets the right one open
// in one step, and is also callable from batchmode.
public static class SceneLauncher
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Wilderness/20. Open Main Scene")]
    public static void OpenMain()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[SceneLauncher] opened " + scene.name + " roots=" + scene.rootCount);
        Selection.activeObject = null;
    }
}
