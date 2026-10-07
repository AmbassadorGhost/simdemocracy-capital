using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SDWalkthroughReview
{
    static readonly string[] Rooms={"Courtroom","Legislative Chamber","President's Office","Secretary Name National Archives","The Queer Quorum - Cafe","Community Cinema","Suspicious Side Room"};
    static readonly float[] Width={18,22,14,20,20,20,9},Depth={18,16,12,20,16,16,10};
    static string output;
    static Camera camera;
    public static void Render()
    {
        var scene=Environment.GetEnvironmentVariable("SIMDEM_AUDIT_SCENE")??"Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v05.unity";
        output=Environment.GetEnvironmentVariable("SIMDEM_REVIEW_OUTPUT");Directory.CreateDirectory(output);
        EditorSceneManager.OpenScene(scene);SDValidateScene.Check();
        var g=new GameObject("Temporary walkthrough camera");camera=g.AddComponent<Camera>();camera.nearClipPlane=.04f;camera.farClipPlane=250;camera.fieldOfView=75;
        for(int i=0;i<Rooms.Length;i++)
        {
            var t=GameObject.Find(Rooms[i]).transform;float w=Width[i],d=Depth[i];
            var views=new[]{
                Capture(t,new Vector3(0,1.65f,-d/2+.7f),new Vector3(0,1.8f,d*.25f)),
                Capture(t,new Vector3(-w*.37f,1.65f,-d*.35f),new Vector3(w*.12f,1.8f,d*.2f)),
                Capture(t,new Vector3(w*.37f,1.65f,d*.34f),new Vector3(-w*.1f,1.6f,-d*.3f)),
                Capture(t,new Vector3(-w*.37f,1.65f,d*.34f),new Vector3(w*.1f,1.7f,-d*.28f))};
            SaveSheet((i+1).ToString("00")+"-"+Rooms[i].Replace("'", ""),views);
        }
        SaveSheet("08-Square",new[]{Capture(null,new Vector3(0,1.65f,-10),new Vector3(0,2,8)),Capture(null,new Vector3(0,1.65f,15),new Vector3(0,1.7f,-7)),Capture(null,new Vector3(-11,1.65f,-5),new Vector3(6,1.7f,3)),Capture(null,new Vector3(11,1.65f,-5),new Vector3(-6,1.7f,3))});
        SaveSheet("09-Approaches",new[]{Capture(null,new Vector3(-8,1.65f,9),new Vector3(-25,2,9)),Capture(null,new Vector3(8,1.65f,9),new Vector3(25,2,9)),Capture(null,new Vector3(-20,1.65f,-5),new Vector3(-20,2,-17)),Capture(null,new Vector3(20,1.65f,-5),new Vector3(20,2,-17))});
        SaveSheet("10-Exterior",new[]{Capture(null,new Vector3(0,1.65f,36),new Vector3(0,2,45)),Capture(null,new Vector3(-31,1.65f,-16),new Vector3(-39,2,-16)),Capture(null,new Vector3(0,18,-45),new Vector3(0,0,13)),Capture(null,new Vector3(47,12,45),new Vector3(0,1,9))});
        var reveal=UnityEngine.Object.FindObjectsOfType<SDAction>(true).Single(a=>a.action==5&&a.reveal!=null&&a.reveal.name=="Local Doge reveal").reveal;reveal.SetActive(true);
        var gag=GameObject.Find(Rooms[6]).transform;
        SaveSheet("11-Bonk-Revealed",new[]{Capture(gag,new Vector3(0,1.65f,-4),new Vector3(0,2.3f,4)),Capture(gag,new Vector3(-3,1.65f,0),new Vector3(0,2.3f,4)),Capture(gag,new Vector3(3,1.65f,0),new Vector3(0,2.3f,4)),Capture(gag,new Vector3(0,1.65f,3),new Vector3(0,1.6f,-5))});
        var issues=new List<string>();
        foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader")issues.Add("Missing material/shader: "+r.name);
        foreach(var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)issues.Add("Text overflow: "+t.name);}
        File.WriteAllLines(Path.Combine(output,"checks.txt"),issues.Count==0?new[]{"No missing materials/shaders or overflowing active text detected."}:issues);
        UnityEngine.Object.DestroyImmediate(g);Debug.Log("WALKTHROUGH RENDER COMPLETE: 44 views; "+scene);
    }
    static Texture2D Capture(Transform room,Vector3 pos,Vector3 look)
    {
        camera.transform.position=room?room.TransformPoint(pos):pos;camera.transform.LookAt(room?room.TransformPoint(look):look);
        var rt=new RenderTexture(1000,625,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1000,625,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,625),0,0);tex.Apply();
        RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);return tex;
    }
    static void SaveSheet(string name,Texture2D[] views)
    {
        var sheet=new Texture2D(2000,1250,TextureFormat.RGB24,false);
        for(int i=0;i<4;i++){sheet.SetPixels((i%2)*1000,(1-i/2)*625,1000,625,views[i].GetPixels());File.WriteAllBytes(Path.Combine(output,name+"-"+(i+1)+".png"),views[i].EncodeToPNG());UnityEngine.Object.DestroyImmediate(views[i]);}
        sheet.Apply();File.WriteAllBytes(Path.Combine(output,name+"-sheet.png"),sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
    }
}
