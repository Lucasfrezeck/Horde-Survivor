using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class FillArenaTerrainTexture
{
    private const string AppliedPreference = "HordeSurvivor.ArenaTerrainExpandedAndTextured.v3";
    private const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    private const string SandTexturePath = "Assets/Environment/Textures/sand_01_diff_2k.png";
    private const string SandLayerPath = "Assets/Environment/Textures/NewLayer.terrainlayer";

    static FillArenaTerrainTexture()
    {
        if (EditorPrefs.GetBool(AppliedPreference))
        {
            return;
        }

        EditorApplication.delayCall += ApplyToLoadedArena;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && !EditorPrefs.GetBool(AppliedPreference))
        {
            EditorApplication.delayCall += ApplyToLoadedArena;
        }
    }

    private static void ApplyToLoadedArena()
    {
        if (EditorPrefs.GetBool(AppliedPreference) || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene arenaScene = SceneManager.GetSceneByPath(ArenaScenePath);
        bool openedForTextureFill = !arenaScene.IsValid() || !arenaScene.isLoaded;
        if (openedForTextureFill)
        {
            arenaScene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Additive);
        }

        try
        {
            Terrain terrain = arenaScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
                .FirstOrDefault(candidate => candidate.name == "Terrain_Desert");
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            TerrainData terrainData = terrain.terrainData;
            Texture2D sandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SandTexturePath);
            TerrainLayer sandLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SandLayerPath);
            if (sandTexture == null || sandLayer == null || sandLayer.diffuseTexture != sandTexture)
            {
                Debug.LogError("Arena terrain texture fill skipped: the sand texture or its Terrain Layer could not be loaded.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(new Object[] { terrainData, terrain.transform }, "Expand and Texture Arena Terrain");
            Vector3 expandedSize = terrainData.size;
            expandedSize.x *= 2f;
            expandedSize.z *= 2f;
            terrainData.size = expandedSize;

            Vector3 terrainPosition = terrain.transform.position;
            terrain.transform.localScale = Vector3.one;
            terrain.transform.position = new Vector3(-expandedSize.x * 0.5f, terrainPosition.y, -expandedSize.z * 0.5f);

            int width = terrainData.alphamapWidth;
            int height = terrainData.alphamapHeight;
            float[,,] alphamaps = new float[height, width, 1];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    alphamaps[y, x, 0] = 1f;
                }
            }

            terrainData.terrainLayers = new[] { sandLayer };
            terrainData.SetAlphamaps(0, 0, alphamaps);
            EditorUtility.SetDirty(terrainData);
            EditorSceneManager.MarkSceneDirty(arenaScene);
            AssetDatabase.SaveAssets();
            bool sceneSaved = EditorSceneManager.SaveScene(arenaScene);
            if (!sceneSaved)
            {
                Debug.LogError("Arena terrain was updated, but Arena.unity could not be saved.");
                return;
            }

            EditorPrefs.SetBool(AppliedPreference, true);

            Debug.Log("Expanded the Arena terrain and applied sand_01_diff_2k across its full surface.");
        }
        finally
        {
            if (openedForTextureFill && arenaScene.IsValid() && arenaScene.isLoaded)
            {
                EditorSceneManager.CloseScene(arenaScene, true);
            }
        }
    }
}
