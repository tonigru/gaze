using GazeCraft;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GazeCraftSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string EyeTrackerPrefabPath = "Assets/TobiiPro/ScreenBased/Prefabs/[EyeTracker].prefab";

    [MenuItem("GazeCraft/Build Sample Scene")]
    public static void BuildSampleScene()
    {
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
