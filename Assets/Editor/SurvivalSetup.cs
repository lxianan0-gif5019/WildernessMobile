using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot scene wiring for the survival stats update.
// Menu: Wilderness -> 19. Setup Survival   (also callable from batchmode)
public static class SurvivalSetup
{
    [MenuItem("Wilderness/19. Setup Survival")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[Setup] no Player found"); return; }

        if (player.GetComponent<PlayerStats>() == null)
        {
            player.AddComponent<PlayerStats>();
            Debug.Log("[Setup] PlayerStats added to Player");
        }
        else Debug.Log("[Setup] PlayerStats already present");

        // regenerate resources so bushes drop berries and the water pool exists
        ResourceSpawner.Spawn();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[Setup] SURVIVAL SETUP OK");
    }
}
