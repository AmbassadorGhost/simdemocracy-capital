using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;

public static class SDIntegrationCheck
{
    public static void Run()
    {
        string scenePath = AssetDatabase.FindAssets("t:Scene", new[]{"Assets/SimDemocracy/Scenes"})
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(File.GetLastWriteTimeUtc).Last();
        EditorSceneManager.OpenScene(scenePath);
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = false });
        if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new Exception("UdonSharp compilation failed.");
        SDValidateScene.Check();
        AssetDatabase.SaveAssets();
        Debug.Log("SIMDEM INTEGRATION PASS: Udon compilation and scene wiring checked.");
    }

    public static void Render()
    {
        Run();
        var cameraObject = new GameObject("Temporary review camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f,.075f,.13f);
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 250;
        var roofs = UnityEngine.Object.FindObjectsOfType<Transform>().Where(t => t.name.StartsWith("Roof -")).ToArray();
        foreach (var roof in roofs) roof.gameObject.SetActive(false);
        camera.orthographic = true;
        camera.orthographicSize = 58;
        camera.transform.position = new Vector3(76,100,-80);
        camera.transform.LookAt(new Vector3(0,0,12));
        Save(camera,"SimDemocracy-Overview.png");
        foreach (var roof in roofs) roof.gameObject.SetActive(true);
        camera.orthographic = false;
        camera.fieldOfView = 72;
        Interior(camera,"The Queer Quorum - Cafe","SimDemocracy-Cafe.png");
        Interior(camera,"Legislative Chamber","SimDemocracy-Chamber.png");
        UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("SIMDEM PREVIEWS: saved three actual Unity renders beside the project.");
    }
    static void Interior(Camera camera,string roomName,string filename)
    {
        Transform room = GameObject.Find(roomName).transform;
        camera.transform.position = room.TransformPoint(new Vector3(0,2.1f,-6));
        camera.transform.LookAt(room.TransformPoint(new Vector3(0,2f,4)));
        Save(camera,filename);
    }
    static void Save(Camera camera,string filename)
    {
        var target = new RenderTexture(1600,1000,24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var pixels = new Texture2D(1600,1000,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);
        pixels.Apply();
        File.WriteAllBytes(Path.GetFullPath("../"+filename),pixels.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(pixels);
    }
}
