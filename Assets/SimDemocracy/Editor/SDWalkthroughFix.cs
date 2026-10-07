using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UdonSharp;
using UdonSharp.Compiler;

public static class SDWalkthroughFix
{
    const string Art="Assets/SimDemocracy/Art-v06";
    const string Scene="Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v06.unity";
    static int meshNumber;
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v03/"+name+".mat");
    static Transform Room(string name)=>GameObject.Find(name).transform;
    static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;
        var mesh=UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;
        for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Scale(vertices[i],size);mesh.vertices=vertices;mesh.RecalculateBounds();
        var uv=new Vector2[vertices.Length];var uv2=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            var n=normals[i];var v=vertices[i];var q=v-mesh.bounds.min;q=new Vector3(q.x/size.x,q.y/size.y,q.z/size.z);int face;float x,y;
            if(Mathf.Abs(n.y)>.5f){face=n.y>0?0:1;x=q.x;y=q.z;uv[i]=new Vector2(v.x,v.z)*.5f;}
            else if(Mathf.Abs(n.x)>.5f){face=n.x>0?2:3;x=q.z;y=q.y;uv[i]=new Vector2(v.z,v.y)*.5f;}
            else{face=n.z>0?4:5;x=q.x;y=q.y;uv[i]=new Vector2(v.x,v.y)*.5f;}
            uv2[i]=new Vector2((face%3)/3f+.04f+x*(1f/3-.08f),(face/3)*.5f+.04f+y*.42f);
        }
        mesh.uv=uv;mesh.uv2=uv2;AssetDatabase.CreateAsset(mesh,Art+"/Mesh-"+(meshNumber++)+".asset");g.GetComponent<MeshFilter>().sharedMesh=mesh;
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.receiveGI=ReceiveGI.Lightmaps;
        if(solid)g.GetComponent<BoxCollider>().size=size;else UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI);return g;
    }
    static void CloseBack(string name,float width,float depth)
    {
        var room=Room(name);Box(room,"Rear wall closure",new Vector3(0,1.85f,depth/2),new Vector3(4,3.7f,.25f),Mat("Warm plaster"),true);Trim(room,width,depth);
    }
    static void Trim(Transform r,float w,float d)
    {
        Box(r,"Continuous rear wainscot",new Vector3(0,.8f,d/2-.18f),new Vector3(w,1.4f,.08f),Mat("Midnight painted timber"));
        Box(r,"Rear dado rail",new Vector3(0,1.5f,d/2-.24f),new Vector3(w,.06f,.1f),Mat("Brushed brass"));
        Box(r,"Rear skirting",new Vector3(0,.22f,d/2-.22f),new Vector3(w,.22f,.12f),Mat("Walnut"));
    }
    static void Returns()
    {
        foreach(var action in UnityEngine.Object.FindObjectsOfType<SDAction>().Where(a=>a.action==6))
        {
            var button=action.transform;var parent=button.parent;var origin=button.localPosition;
            var label=parent.GetComponentsInChildren<TMP_Text>().Single(t=>t.text=="RETURN TO SQUARE");
            var stand=parent.Cast<Transform>().Where(t=>t.name=="Control stand").OrderBy(t=>Vector3.Distance(t.localPosition,new Vector3(origin.x,0,origin.z+.1f))).First();
            var mount=new GameObject("Return control facing room").transform;mount.SetParent(parent,false);mount.localPosition=origin;
            button.SetParent(mount,true);label.transform.SetParent(mount,true);stand.SetParent(mount,true);mount.localRotation=Quaternion.Euler(0,180,0);
        }
    }
    static void TextMaterials()
    {
        var copies=new Dictionary<Material,Material>();
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
        {
            var source=text.fontSharedMaterial;if(source==null)continue;
            if(!copies.TryGetValue(source,out var copy)){copy=new Material(source);copy.SetFloat("_CullMode",2);AssetDatabase.CreateAsset(copy,Art+"/Text-front-"+copies.Count+".mat");copies.Add(source,copy);}
            text.fontSharedMaterial=copy;
        }
    }
    static void Furnishings()
    {
        var court=Room("Courtroom");court.Find("Court rear wainscot").gameObject.SetActive(false);court.Find("Court rear dado").gameObject.SetActive(false);Trim(court,18,18);
        court.Find("Original SimDemocracy Flag").localPosition=new Vector3(0,3.2f,8.64f);
        var caption=UnityEngine.Object.FindObjectOfType<SDGavel>().caption;caption.transform.localPosition=new Vector3(0,2.04f,8.49f);caption.fontSize=2;
        Box(court,"Court caption backing",new Vector3(0,1.89f,8.56f),new Vector3(4.8f,.5f,.08f),Mat("Midnight painted timber"));
        var office=Room("President's Office");office.Find("Original SimDemocracy Flag").localPosition=new Vector3(0,3.3f,5.78f);
        var officeTitle=office.GetComponentsInChildren<TMP_Text>().Single(t=>t.text=="OFFICE OF THE PRESIDENT");officeTitle.transform.localPosition=new Vector3(0,2.05f,5.69f);
        Box(office,"Office title backing",new Vector3(0,1.9f,5.76f),new Vector3(4.7f,.45f,.08f),Mat("Midnight painted timber"));
        var inbox=office.GetComponentsInChildren<TMP_Text>().Single(t=>t.text.StartsWith("INBOX"));inbox.transform.localPosition=new Vector3(-1,1.04f,.7f);inbox.transform.localRotation=Quaternion.Euler(90,0,0);inbox.fontSize=1;
        Box(office,"Inbox paper",new Vector3(-1,1.005f,.4f),new Vector3(1.35f,.055f,.8f),Mat("Glazed porcelain"));
        var chair=office.Find("Chair");foreach(float x in new[]{-1.1f,1.1f}){var guest=UnityEngine.Object.Instantiate(chair.gameObject,office).transform;guest.name="Guest chair";guest.localPosition=new Vector3(x,0,-1.3f);guest.localRotation=Quaternion.identity;}
        var cafe=Room("The Queer Quorum - Cafe");cafe.Find("Original SimDemocracy Flag").localPosition=new Vector3(6,3.1f,7.7f);
        foreach(var stripe in cafe.Cast<Transform>().Where(t=>t.name=="Pride stripe")){var p=stripe.localPosition;p.z=7.7f;stripe.localPosition=p;}
        var cinema=Room("Community Cinema");var screen=cinema.Find("Video Screen - replace with video prefab output");screen.localPosition=new Vector3(0,3,7.7f);
        cinema.GetComponentsInChildren<TMP_Text>().Single(t=>t.text.StartsWith("SIMDEM CINEMA")).transform.localPosition=new Vector3(0,4.2f,7.55f);
        var gag=Room("Suspicious Side Room");gag.Find("Bonk back wainscot").gameObject.SetActive(false);Trim(gag,9,10);
        foreach(var room in new[]{cinema,gag})foreach(var light in room.GetComponentsInChildren<Light>()){if(light.name=="Warm room fill")light.intensity=1.5f;if(light.name=="Rear room fill")light.intensity=1.1f;}
        var archive=Room("Secretary Name National Archives");archive.Find("Reading screen backing").localPosition=new Vector3(0,2.5f,9.7f);
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();reader.display.transform.localPosition=new Vector3(-3.9f,4.2f,9.55f);reader.display.rectTransform.sizeDelta=new Vector2(7.8f,3.6f);reader.display.enableWordWrapping=true;reader.display.fontSize=1.75f;
        archive.Find("Reader instruction backing").localPosition=new Vector3(0,4.75f,9.7f);archive.Find("Reader instruction copy").localPosition=new Vector3(-4.05f,4.9f,9.62f);
        foreach(var label in archive.GetComponentsInChildren<TMP_Text>().Where(t=>new[]{"CONSTITUTION","LEGISLATION","CASE LAW","ORDERS","TREATIES","HISTORY"}.Contains(t.text)))
            Box(archive,"Shelf category backing",label.transform.localPosition+new Vector3(0,-.12f,.08f),new Vector3(2.4f,.42f,.08f),Mat("Midnight painted timber"));
        var root=court.parent;var flag=root.Find("Original SimDemocracy Flag");var reverse=UnityEngine.Object.Instantiate(flag.gameObject,root).transform;reverse.name="Monument reverse flag";reverse.localPosition=new Vector3(0,2.2f,3.18f);reverse.localRotation=Quaternion.Euler(0,180,0);
    }
    static void Paths()
    {
        var root=Room("Courtroom").parent;var paving=root.Find("Civic Square").GetComponent<Renderer>().sharedMaterial;
        root.Find("West Walk").gameObject.SetActive(false);root.Find("East Walk").gameObject.SetActive(false);
        Box(root,"Court approach",new Vector3(-14.5f,.04f,9),new Vector3(5,.08f,4),paving,true);
        Box(root,"Archive approach",new Vector3(14,.04f,9),new Vector3(4,.08f,4),paving,true);
        Box(root,"Cafe approach",new Vector3(-16,.04f,-7.4f),new Vector3(8,.08f,3.2f),paving,true);
        Box(root,"Cafe threshold",new Vector3(-20,.04f,-7.4f),new Vector3(4,.08f,3.2f),paving,true);
        Box(root,"Cinema approach",new Vector3(16,.04f,-7.4f),new Vector3(8,.08f,3.2f),paving,true);
        Box(root,"Cinema threshold",new Vector3(20,.04f,-7.4f),new Vector3(4,.08f,3.2f),paving,true);
        Box(root,"Side room lane",new Vector3(-27.5f,.04f,-6.5f),new Vector3(11,.08f,3),paving,true);
        Box(root,"Side room approach",new Vector3(-32.5f,.04f,-12),new Vector3(3,.08f,11),paving,true);
        Box(root,"Side room threshold",new Vector3(-33,.04f,-16),new Vector3(2,.08f,3),paving,true);
    }
    public static void Build()
    {
        if(File.Exists(Scene))throw new Exception("v06 already exists.");
        EditorSceneManager.OpenScene("Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v05.unity");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        CloseBack("President's Office",14,12);CloseBack("Secretary Name National Archives",20,20);CloseBack("Community Cinema",20,16);
        Returns();Furnishings();Paths();TextMaterials();
        var lighting=UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);AssetDatabase.CreateAsset(lighting,Art+"/LightingSettings.asset");Lightmapping.lightingSettings=lighting;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions{IsEditorBuild=false});if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("Udon failed.");
        SDValidateScene.Check();CheckRoutes();
        if(!Lightmapping.Bake())throw new Exception("Walkthrough bake failed.");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Environment.SetEnvironmentVariable("SIMDEM_AUDIT_SCENE",Scene);SDWalkthroughReview.Render();Debug.Log("WALKTHROUGH FIX COMPLETE");
    }
    public static void CheckRoutes()
    {
        Physics.SyncTransforms();
        foreach(var pair in new[]{Tuple.Create("Courtroom",18f),Tuple.Create("President's Office",12f),Tuple.Create("Secretary Name National Archives",20f),Tuple.Create("Community Cinema",16f),Tuple.Create("Suspicious Side Room",10f)})
        {
            var r=Room(pair.Item1);if(!Physics.Raycast(r.TransformPoint(new Vector3(0,1,pair.Item2/2-.5f)),r.forward,1,~0,QueryTriggerInteraction.Ignore))throw new Exception("Rear closure missing: "+pair.Item1);
        }
        foreach(var a in UnityEngine.Object.FindObjectsOfType<SDAction>().Where(a=>a.action==6))
            if(Vector3.Dot(-a.transform.forward,a.transform.parent.parent.forward)<.95f)throw new Exception("Return faces away from room: "+a.name);
        Debug.Log("ROUTE CHECK: five rear closures and seven return controls passed.");
    }
    public static void Finish()
    {
        EditorSceneManager.OpenScene(Scene);
        var button=UnityEngine.Object.FindObjectsOfType<SDAction>(true).Single(a=>a.action==5);
        var label=button.transform.parent.GetComponentsInChildren<TMP_Text>().Single(t=>t.text=="ENTER IF YOU DARE"||t.text=="PRESS ME");label.text="PRESS ME";
        label.color=Color.white;label.fontSize=1.8f;
        button.reveal.transform.Find("Bonk poster").localPosition=new Vector3(0,3.05f,4.7f);
        button.gameObject.name="PRESS ME";button.reveal.SetActive(false);
        button.Interact();if(!button.reveal.activeSelf)throw new Exception("First Bonk press failed.");
        button.Interact();if(button.reveal.activeSelf)throw new Exception("Second Bonk press failed.");
        Debug.Log("BONK CHECK: first press reveals, second press hides; saved hidden.");
        var inbox=Room("President's Office").GetComponentsInChildren<TMP_Text>().Single(t=>t.text.StartsWith("INBOX"));inbox.rectTransform.sizeDelta=new Vector2(1.2f,.65f);inbox.rectTransform.pivot=new Vector2(.5f,.5f);inbox.alignment=TextAlignmentOptions.Center;inbox.color=Color.black;inbox.transform.localRotation=Quaternion.Euler(90,0,0);inbox.rectTransform.anchoredPosition3D=new Vector3(-1,1.04f,.4f);EditorUtility.SetDirty(inbox.rectTransform);
        var office=Room("President's Office");var title=office.GetComponentsInChildren<TMP_Text>().Single(t=>t.text=="OFFICE OF THE PRESIDENT");title.rectTransform.sizeDelta=new Vector2(4.5f,.4f);title.rectTransform.pivot=new Vector2(.5f,.5f);title.alignment=TextAlignmentOptions.Center;title.rectTransform.anchoredPosition3D=new Vector3(0,1.9f,5.69f);EditorUtility.SetDirty(title.rectTransform);
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();int pages=0;
        for(int i=0;i<reader.documents.Length;i++)
        {
            reader.Select(i);int count=(reader.documents[i].Split('\n').Length+reader.linesPerPage-1)/reader.linesPerPage;
            for(int page=0;page<count;page++){reader.display.ForceMeshUpdate();if(reader.display.isTextOverflowing)throw new Exception("Archive overflow: "+i+" page "+page);pages++;reader.NextPage();}
        }
        reader.Select(0);Debug.Log("ARCHIVE CHECK: "+pages+" pages fit.");
        Physics.SyncTransforms();
        foreach(var pair in new[]{Tuple.Create("Courtroom",18f),Tuple.Create("Legislative Chamber",16f),Tuple.Create("President's Office",12f),Tuple.Create("Secretary Name National Archives",20f),Tuple.Create("The Queer Quorum - Cafe",16f),Tuple.Create("Community Cinema",16f),Tuple.Create("Suspicious Side Room",10f)})
        {
            var r=Room(pair.Item1);var start=new Vector3(0,.45f,-pair.Item2/2-.8f);
            if(Physics.CapsuleCast(r.TransformPoint(start),r.TransformPoint(start+Vector3.up*1.1f),.25f,r.forward,out var hit,1.6f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Entrance blocked: "+pair.Item1+" by "+hit.collider.name);
        }
        Debug.Log("ENTRY CHECK: seven doorways clear for a 0.5 m wide, 1.6 m tall capsule.");
        SDValidateScene.Check();CheckRoutes();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Environment.SetEnvironmentVariable("SIMDEM_AUDIT_SCENE",Scene);SDWalkthroughReview.Render();Debug.Log("WALKTHROUGH FINAL CHECK COMPLETE");
    }
}
