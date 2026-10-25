using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

// 꼬리별 Unity 웹 빌드 → ../public/play (Cloudflare가 웹 버전과 같은 주소의 /play 로 서빙)
// 실행: Unity -batchmode -quit -projectPath unity -executeMethod WebBuild.Build
public static class WebBuild
{
    const string Scene = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/웹 빌드")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(Scene)) EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene), Scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        AddAlwaysIncluded("Sprites/Default", "Unlit/Transparent", "UI/Default");
        PlayerSettings.productName = "꼬리별";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;   // 두 번째부터 빨리 열리게
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
        var out_ = Path.GetFullPath("../public/play");
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = out_, target = BuildTarget.WebGL });
        var msg = $"result={r.summary.result} size={r.summary.totalSize / 1024 / 1024}MB errors={r.summary.totalErrors}";
        foreach (var st in r.steps) foreach (var m in st.messages) if (m.type == LogType.Error) msg += "\n  " + m.content;
        File.WriteAllText("Logs/webbuild.txt", msg);
        Debug.Log("[WebBuild] " + msg);
    }

    static void AddAlwaysIncluded(params string[] names)
    {
        var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var list = gs.FindProperty("m_AlwaysIncludedShaders");
        foreach (var n in names)
        {
            var sh = Shader.Find(n); if (!sh) continue;
            bool has = false; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
            if (has) continue;
            list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
        }
        gs.ApplyModifiedProperties();
    }
}
