using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GazeCraftAutoBuildOnce
{
    private const string MarkerPath = "ProjectSettings/GazeCraftRaisedUiBuilt.marker";

    static GazeCraftAutoBuildOnce()
    {
        EditorApplication.delayCall += BuildAfterImport;
    }

    private static void BuildAfterImport()
    {
        if (Application.isPlaying || File.Exists(MarkerPath))
        {
            return;
        }

        GazeCraftSceneBuilder.BuildSampleScene();
        File.WriteAllText(MarkerPath, "built");
    }
}
