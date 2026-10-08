using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ModelsPlatformBuilder
{
    private const string BuiltKey = "GoldenRing.ModelsPlatform.Built.v6";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BoardPath = "Assets/ImportedModels/bio_motherboard.fbx";
    private const string CombinedPath = "Assets/ImportedModels/models_platform_showcase.fbx";
    private static readonly string[] ModelPaths =
    {
        BoardPath,
        "Assets/ImportedModels/vials.fbx",
        "Assets/ImportedModels/planet_fragment.fbx",
        "Assets/ImportedModels/small_rack.fbx",
        "Assets/ImportedModels/tubaretka.fbx"
    };

    static ModelsPlatformBuilder()
    {
        EditorApplication.delayCall += TryBuild;
        EditorApplication.projectChanged += TryBuild;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Tools/Group Models in Room")]
    public static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool(BuiltKey, false)) return;

        var sources = new Dictionary<string, GameObject>();
        foreach (var path in ModelPaths)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) return;
            sources[path] = source;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var instances = new Dictionary<string, GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (path == CombinedPath)
                Object.DestroyImmediate(root);
            else if (sources.ContainsKey(path))
            {
                instances[path] = root;
                root.name = path == BoardPath ? "Материнская плата" : System.IO.Path.GetFileNameWithoutExtension(path);
            }
        }

        foreach (var path in ModelPaths)
        {
            if (instances.ContainsKey(path)) continue;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(sources[path], scene);
            model.name = path == BoardPath ? "Материнская плата" : System.IO.Path.GetFileNameWithoutExtension(path);
            model.transform.position = Vector3.zero;
            instances[path] = model;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.SetActiveScene(scene);
        EditorPrefs.SetBool(BuiltKey, true);
        Debug.Log("Четыре модели и материнская плата собраны в отдельные группы; портал сохранён: " + ScenePath);
    }
}
