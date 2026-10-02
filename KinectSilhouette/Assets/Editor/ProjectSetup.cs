using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System.IO;

public static class ProjectSetup
{
    [MenuItem("Silhouette/Create or reset scene")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        new GameObject("Silhouette Screen").AddComponent<SilhouetteScreen>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Projection.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Projection.unity",true) };
        PlayerSettings.companyName = "NAISPO";
        PlayerSettings.productName = "Kinect Silhouette";
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Silhouette/Build Windows app")]
    public static void Build()
    {
        Setup();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[]{"Assets/Scenes/Projection.unity"},
            locationPathName = "Build/KinectSilhouette.exe", target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed");
        foreach (var name in new[]{"directml.dll","onnxruntime.dll","onnxruntime_providers_shared.dll"})
            File.Copy(Path.Combine("Assets/Plugins/x86_64",name), Path.Combine("Build",name),true);
    }
}
