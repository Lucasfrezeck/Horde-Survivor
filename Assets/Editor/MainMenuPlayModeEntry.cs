using UnityEditor;
using UnityEditor.SceneManagement;

// Match the built game's entry scene when Play is pressed from any open scene.
[InitializeOnLoad]
internal static class MainMenuPlayModeEntry
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    static MainMenuPlayModeEntry()
    {
        EditorApplication.delayCall += ConfigureStartScene;
    }

    private static void ConfigureStartScene()
    {
        SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (menuScene != null)
        {
            EditorSceneManager.playModeStartScene = menuScene;
        }
    }
}
