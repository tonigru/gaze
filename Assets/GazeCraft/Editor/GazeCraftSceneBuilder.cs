using GazeCraft;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GazeCraftSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string EyeTrackerPrefabPath = "Assets/TobiiPro/ScreenBased/Prefabs/[EyeTracker].prefab";
    private const string ArtPath = "Assets/GazeCraft/Resources/GazeCraftArt";

    [MenuItem("GazeCraft/Build Sample Scene")]
    public static void BuildSampleScene()
    {
        ConfigureArtImporters();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClearScene();

        var bootstrapObject = new GameObject("GazeCraft Bootstrap");
        var bootstrap = bootstrapObject.AddComponent<GazeCraftBootstrap>();
        bootstrap.BuildRuntimeScene();

        EnsureEyeTrackerPrefab();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("GazeCraft sample scene rebuilt.");
    }

    private static void ConfigureArtImporters()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static void ClearScene()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureEyeTrackerPrefab()
    {
        if (Object.FindAnyObjectByType<Tobii.Research.Unity.EyeTracker>() != null)
        {
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EyeTrackerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Could not find Tobii EyeTracker prefab at " + EyeTrackerPrefabPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "[EyeTracker]";
    }
}
