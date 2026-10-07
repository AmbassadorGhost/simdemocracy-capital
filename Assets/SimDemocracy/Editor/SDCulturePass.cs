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
using UdonSharpEditor;

public static class SDCulturePass
{
    const string Art = "Assets/SimDemocracy/Art-v04";
    const string Scene = "Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v04.unity";
    [Serializable] public class Record { public string id,title,room,era,evidence,source,display,detail,art,credit; }
    [Serializable] public class Catalog { public Record[] records; }
    static Dictionary<string,Record> records;
    static Material wood,navy,gold,paper,plaster,green,orange,black;
    static TMP_FontAsset font;
    static Transform root;
    static Material Existing(string name) { return AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v03/"+name+".mat"); }
    static Material ColorMaterial(string name,Color color)
    {
        var m=new Material(Shader.Find("VRChat/Mobile/Standard Lite"));m.color=color;
        m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.12f);
        AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;
    }
    static Transform Group(Transform parent,string name,Vector3 pos,float yaw=0)
    { var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);return g; }
    static GameObject Part(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType shape=PrimitiveType.Cube,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;
        if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI);
        return g;
    }
    static TMP_Text Text(Transform p,string name,string content,Vector3 pos,float size,float width,Color color)
    {
        var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;
        var t=g.AddComponent<TextMeshPro>();t.font=font;t.fontSize=size;t.text=content;t.color=color;
        t.alignment=TextAlignmentOptions.TopLeft;t.rectTransform.pivot=new Vector2(0,1);
        t.rectTransform.sizeDelta=new Vector2(width,2);t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;
        return t;
    }
    static void Plaque(Transform p,string id,Vector3 pos,float width=3.6f,float height=1.6f,float yaw=0)
    {
        var r=records[id];var g=Group(p,id+" - "+r.title,pos,yaw);
        Part(g,"Brass frame",Vector3.zero,new Vector3(width+.08f,height+.08f,.1f),gold);
        Part(g,"Navy interpretation panel",new Vector3(0,0,-.065f),new Vector3(width,height,.035f),navy);
        Text(g,"Title",r.title,new Vector3(-width/2+.14f,height/2-.13f,-.1f),1.65f,width-.28f,Color.white);
        Text(g,"Interpretation",r.display,new Vector3(-width/2+.14f,height/2-.47f,-.1f),1.2f,width-.28f,new Color(.92f,.86f,.73f));
        Text(g,"Evidence label",r.id+" / "+r.era+"\nSources and context in the archive reader",new Vector3(-width/2+.14f,-height/2+.36f,-.1f),.72f,width-.28f,new Color(.7f,.77f,.82f));
    }
    static void FixCourt(Transform court)
    {
        var patch=Part(court,"Court rear wall repair",new Vector3(0,1.85f,9),new Vector3(4,3.7f,.25f),plaster,PrimitiveType.Cube,true);
        var mesh=UnityEngine.Object.Instantiate(patch.GetComponent<MeshFilter>().sharedMesh);
        var scale=patch.transform.localScale;mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(v,scale)).ToArray();mesh.RecalculateBounds();
        var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i]=Vector2.Scale(uv[i],new Vector2(2,1.85f));mesh.uv=uv;
        WallUV(mesh);AssetDatabase.CreateAsset(mesh,Art+"/CourtWall.asset");
        patch.GetComponent<MeshFilter>().sharedMesh=mesh;patch.GetComponent<BoxCollider>().size=scale;patch.transform.localScale=Vector3.one;
        patch.GetComponent<MeshRenderer>().receiveGI=ReceiveGI.Lightmaps;
        Part(court,"Court rear wainscot",new Vector3(0,.8f,8.82f),new Vector3(4,1.4f,.08f),navy);
        Part(court,"Court rear dado",new Vector3(0,1.5f,8.76f),new Vector3(4,.06f,.1f),gold);
        // Dedicated clerk desk, away from the bench, gavel, gallery and central aisle.
        var clerk=Group(court,"Historical duck clerk",new Vector3(-6,0,4));
        Part(clerk,"Clerk tabletop",new Vector3(0,1,0),new Vector3(2.2f,.14f,1.1f),wood,PrimitiveType.Cube,true);
        foreach(float x in new[]{-.85f,.85f})Part(clerk,"Desk support",new Vector3(x,.5f,0),new Vector3(.13f,1,.8f),navy,PrimitiveType.Cube,true);
        Part(clerk,"Historical folder",new Vector3(.5f,1.11f,0),new Vector3(.7f,.05f,.5f),paper);
        var duck=Group(clerk,"Original duck figurine",new Vector3(-.5f,1.3f,0));
        Part(duck,"Duck body",Vector3.zero,new Vector3(.5f,.36f,.65f),gold,PrimitiveType.Sphere);
        Part(duck,"Duck head",new Vector3(0,.28f,-.2f),Vector3.one*.31f,gold,PrimitiveType.Sphere);
        Part(duck,"Duck beak",new Vector3(0,.25f,-.39f),new Vector3(.21f,.075f,.2f),orange);
        foreach(float x in new[]{-.1f,.1f})Part(duck,"Eye",new Vector3(x,.32f,-.325f),Vector3.one*.035f,black,PrimitiveType.Sphere);
        Plaque(court,"C05",new Vector3(-6,2.65f,5.15f),3.4f,1.65f);
        Physics.SyncTransforms();RaycastHit hit;
        if(!Physics.Raycast(court.TransformPoint(new Vector3(0,1.5f,7.8f)),court.forward,out hit,2)||hit.collider.gameObject!=patch)
            throw new Exception("Court rear-wall collision test failed.");
        Debug.Log("CULTURE CHECK: rear wall blocks the former hole.");
    }
    static void Chamber(Transform room)
    {
        // Along the rear-left wall, clear of all existing desks and return controls.
        var nook=Group(room,"Senate Reading Room",new Vector3(-6.5f,0,6.8f));
        Part(nook,"Reading desk",new Vector3(0,1,0),new Vector3(2.8f,.16f,1.1f),wood,PrimitiveType.Cube,true);
        foreach(float x in new[]{-1.1f,1.1f})Part(nook,"Leg",new Vector3(x,.5f,0),new Vector3(.15f,1,.9f),navy,PrimitiveType.Cube,true);
        Part(nook,"Enormous bill cover",new Vector3(0,1.17f,-.05f),new Vector3(1.8f,.2f,.9f),gold);
        Part(nook,"Bill pages",new Vector3(0,1.29f,-.05f),new Vector3(1.65f,.07f,.8f),paper);
        var title=Text(nook,"Fictional biscuit bill","THE BISCUIT BILL\n1. Bring biscuits.  2. Share biscuits.\n3. Debate who ate the last one.\nFICTIONAL WORLD PROP",new Vector3(-.75f,1.334f,-.36f),1.05f,1.5f,Color.black);
        title.transform.localRotation=Quaternion.Euler(90,0,0);
        Plaque(room,"C22",new Vector3(-6.5f,2.55f,7.55f),3.7f,1.6f);
    }
    static void Cafe(Transform room)
    {
        var rack=Group(room,"Community newspaper rack",new Vector3(7,0,3.8f));
        Part(rack,"Rack base",new Vector3(0,.55f,0),new Vector3(2.7f,1.1f,.55f),wood,PrimitiveType.Cube,true);
        Part(rack,"Paper support",new Vector3(0,1.3f,.15f),new Vector3(2.7f,1,.12f),navy);
        for(int i=0;i<2;i++)
        {
            float x=i==0?-.66f:.66f;
            Part(rack,"Recreated newspaper cover",new Vector3(x,1.3f,.04f),new Vector3(1.14f,.85f,.07f),paper);
            Text(rack,"Newspaper masthead",i==0?"THE SIMDEMOCRACY\nPOST\n21 FEB 2020":"FBN\nISSUE ONE\nJAN 2025",new Vector3(x-.5f,1.64f,-.005f),1.1f,1,Color.black);
            Text(rack,"Cover summary",i==0?"NEWS / INTERVIEWS\nCOMMUNITY OPINION":"ECONOMIC POLICY\n& TRANSPARENCY",new Vector3(x-.5f,1.19f,-.005f),.7f,1,Color.black);
        }
        Plaque(room,"C07",new Vector3(7,2.85f,4.18f),3.3f,1.7f);
        Plaque(room,"C21",new Vector3(5,2.65f,-6.8f),3.8f,1.7f,180);
        var menu=room.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.text.StartsWith("THE QUEER QUORUM"));
        if(menu!=null)menu.text="THE QUEER QUORUM\n\nCOFFEE / COMPANY / COMMON GROUND\n\n"+records["C08"].display;
    }
    static void Square()
    {
        var garden=Group(root,"Optional grass garden",new Vector3(7,0,-3));
        Part(garden,"Garden edging",new Vector3(0,.18f,0),new Vector3(3.8f,.25f,2.6f),gold,PrimitiveType.Cube,true);
        Part(garden,"Grass patch",new Vector3(0,.32f,0),new Vector3(3.6f,.12f,2.4f),green,PrimitiveType.Cube,true);
        Plaque(garden,"C06",new Vector3(0,1.8f,1.15f),3.8f,1.75f);
        foreach(float x in new[]{-1.65f,1.65f})Part(garden,"Sign post",new Vector3(x,.95f,1.2f),new Vector3(.08f,1.9f,.08f),wood);
        var mural=Group(root,"Danyowave arrivals mural",new Vector3(-7,0,-4));
        Part(mural,"Mural plinth",new Vector3(0,.25f,0),new Vector3(4,.5f,.7f),wood,PrimitiveType.Cube,true);
        Part(mural,"Mural panel",new Vector3(0,1.8f,.1f),new Vector3(4,2.8f,.15f),navy,PrimitiveType.Cube,true);
        for(int row=0;row<3;row++)for(int i=0;i<12;i++)
        {
            float x=-1.8f+i*.32f;float y=.6f+row*.22f+Mathf.Sin(i*.62f+row*.5f)*.12f;
            Part(mural,"Abstract arrival wave",new Vector3(x,y,-.015f),new Vector3(.35f,.075f,.045f),row==1?gold:paper);
        }
        for(int i=0;i<4;i++)Part(mural,"Blank visitor badge",new Vector3(-1.35f+i*.9f,1.35f,-.035f),new Vector3(.35f,.45f,.035f),paper);
        Plaque(mural,"C23",new Vector3(0,2.35f,-.05f),3.8f,1.35f);
    }
    static string Wrap(string s,int width=62)
    {
        var lines=new List<string>();foreach(string line in s.Split('\n'))
        { string remaining=line;while(remaining.Length>width){int n=remaining.LastIndexOf(' ',width);if(n<1)n=width;lines.Add(remaining.Substring(0,n));remaining=remaining.Substring(n).TrimStart();}lines.Add(remaining); }
        return string.Join("\n",lines);
    }
    static void Archive(Transform room)
    {
        Plaque(room,"C03",new Vector3(0,3.6f,-9.2f),7.2f,1.6f,180);
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();
        if(reader==null)throw new Exception("Archive reader missing.");
        int originalCount=reader.documents.Length;
        reader.titles=reader.titles.Concat(records.Values.Select(r=>r.id+" / "+r.title)).ToArray();
        reader.documents=reader.documents.Concat(records.Values.Select(r=>Wrap(r.evidence+"\n"+r.era+"\n\n"+r.display+"\n\n"+r.detail+"\n\nSource: "+r.source+"\nArt: "+r.art))).ToArray();
        var volume=Part(room,"Read cultural field guide",new Vector3(0,1.1f,-4),new Vector3(1.6f,.2f,1.1f),gold,PrimitiveType.Cube,true);
        var action=volume.AddUdonSharpComponent<SDAction>();action.action=0;action.reader=reader;action.entry=originalCount;
        Text(room,"Field guide label","CULTURAL FIELD GUIDE\nSelect, then use NEXT ENTRY / NEXT PAGE",new Vector3(-2,1.9f,-4.7f),1.45f,4,Color.white);
        EditorUtility.SetDirty(reader);EditorUtility.SetDirty(action);
    }
    [MenuItem("SimDemocracy/Create Cultural v0.4 Scene")]
    public static void Build()
    {
        if(File.Exists(Scene))throw new Exception("v0.4 already exists; refusing to overwrite it.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene("Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v03.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        records=JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/SimDemocracy/Data/culture-v04.json")).records.ToDictionary(r=>r.id);
        wood=Existing("Walnut");navy=Existing("Midnight painted timber");gold=Existing("Brushed brass");paper=Existing("Glazed porcelain");plaster=Existing("Warm plaster");green=Existing("Foliage");
        orange=ColorMaterial("Duck beak",new Color(.9f,.37f,.08f));black=ColorMaterial("Duck eyes",new Color(.025f,.025f,.025f));
        font=UnityEngine.Object.FindObjectsOfType<TMP_Text>().First(t=>t.font!=null).font;
        var court=GameObject.Find("Courtroom").transform;root=court.parent;
        FixCourt(court);Chamber(GameObject.Find("Legislative Chamber").transform);Cafe(GameObject.Find("The Queer Quorum - Cafe").transform);Square();Archive(GameObject.Find("Secretary Name National Archives").transform);
        Polish();
        var settings=UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);AssetDatabase.CreateAsset(settings,Art+"/LightingSettings.asset");Lightmapping.lightingSettings=settings;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild=false });
        if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("Udon compile failed.");
        SDValidateScene.Check();
        if(!Lightmapping.Bake())throw new Exception("v0.4 bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Review();Debug.Log("CULTURE PASS COMPLETE: "+Scene+"; records="+records.Count+"; lightmaps="+LightmapSettings.lightmaps.Length);
    }
    static void WallUV(Mesh mesh)
    {
        var uv=new Vector2[24];var vertices=mesh.vertices;var normals=mesh.normals;var size=mesh.bounds.size;
        for(int i=0;i<24;i++)
        {
            var p=vertices[i]-mesh.bounds.min;p=new Vector3(p.x/size.x,p.y/size.y,p.z/size.z);var n=normals[i];
            int face;float u,v;
            if(Mathf.Abs(n.y)>.5f){face=n.y>0?0:1;u=p.x;v=p.z;}
            else if(Mathf.Abs(n.x)>.5f){face=n.x>0?2:3;u=p.z;v=p.y;}
            else{face=n.z>0?4:5;u=p.x;v=p.y;}
            uv[i]=new Vector2((face%3)/3f+.04f+u*(1f/3-.08f),(face/3)*.5f+.04f+v*.42f);
        }
        mesh.uv2=uv;
    }
    public static void Refine()
    {
        EditorSceneManager.OpenScene(Scene);
        wood=Existing("Walnut");navy=Existing("Midnight painted timber");gold=Existing("Brushed brass");
        root=GameObject.Find("Courtroom").transform.parent;Polish();
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/CourtWall.asset");WallUV(mesh);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
        if(!Lightmapping.Bake())throw new Exception("Wall lighting refinement failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        Debug.Log("CULTURE REFINEMENT COMPLETE");
    }
    static void Polish()
    {
        var texts=UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);font=texts.First(t=>t.font!=null).font;
        var bill=texts.First(t=>t.name=="Fictional biscuit bill");bill.transform.localPosition=new Vector3(-.75f,1.334f,.30f);bill.fontSize=.82f;
        foreach(var t in texts.Where(t=>t.name=="Newspaper masthead"))t.fontSize=.8f;
        foreach(var t in texts.Where(t=>t.name=="Cover summary")){var p=t.transform.localPosition;p.y=1.13f;t.transform.localPosition=p;}
        foreach(string id in new[]{"C05","C07"})
        {
            var board=UnityEngine.Object.FindObjectsOfType<Transform>().First(t=>t.name.StartsWith(id+" - "));
            if(board.Find("Left board support")==null)
                foreach(float side in new[]{-1f,1f})Part(board,side<0?"Left board support":"Right board support",new Vector3(side*1.4f,-board.localPosition.y/2,.07f),new Vector3(.09f,board.localPosition.y,.09f),wood);
        }
        var welcome=texts.First(t=>t.text.StartsWith("WELCOME TO THE CAPITAL"));
        welcome.text="WELCOME TO THE CAPITAL\nCafe left / Cinema right / Government ahead\nArchives east / Courtroom west";welcome.fontSize=1.7f;welcome.color=Color.white;
        welcome.rectTransform.sizeDelta=new Vector2(5.3f,1.3f);welcome.enableWordWrapping=true;welcome.transform.localPosition=new Vector3(0,2.48f,-4.97f);
        var main=texts.First(t=>t.text.StartsWith("SIMDEMOCRACY\nA republic"));
        main.text="SIMDEMOCRACY\nA republic. A community. A place to meet.";main.fontSize=1.8f;main.color=Color.white;
        main.rectTransform.sizeDelta=new Vector2(5.3f,1);main.transform.localPosition=new Vector3(0,4,2.65f);
        if(root.Find("Welcome board backing")==null)
        {
            Part(root,"Welcome board backing",new Vector3(0,1.96f,-4.82f),new Vector3(5.6f,1.35f,.18f),navy,PrimitiveType.Cube,true);
            foreach(float x in new[]{-2.5f,2.5f})Part(root,"Welcome board leg",new Vector3(x,.7f,-4.82f),new Vector3(.1f,1.4f,.1f),wood);
            Part(root,"Capital title backing",new Vector3(0,3.65f,2.8f),new Vector3(5.6f,1,.15f),navy);
        }
        // Mount wayfinding on the existing monument, leaving the arrival route open.
        root.Find("Welcome board backing").localPosition=new Vector3(0,1.25f,2.6f);
        welcome.transform.localPosition=new Vector3(0,1.78f,2.45f);
        foreach(var leg in root.Cast<Transform>().Where(t=>t.name=="Welcome board leg"))leg.localPosition=new Vector3(leg.localPosition.x,.45f,2.6f);
        var archive=GameObject.Find("Secretary Name National Archives").transform;
        var guide=archive.Find("Read cultural field guide");guide.localPosition=new Vector3(-3,1.1f,-4);
        var guideLabel=texts.First(t=>t.name=="Field guide label");guideLabel.transform.localPosition=new Vector3(-4.8f,1.95f,-4.7f);guideLabel.fontSize=1.15f;guideLabel.rectTransform.sizeDelta=new Vector2(3.6f,.8f);
        if(archive.Find("Field guide table")==null)
        {
            Part(archive,"Field guide table",new Vector3(-3,.89f,-4),new Vector3(2.2f,.16f,1.2f),wood,PrimitiveType.Cube,true);
            foreach(float x in new[]{-3.9f,-2.1f})Part(archive,"Field guide table leg",new Vector3(x,.44f,-4),new Vector3(.12f,.88f,.9f),navy,PrimitiveType.Cube,true);
            Part(archive,"Field guide label backing",new Vector3(-3,1.65f,-4.56f),new Vector3(3.9f,.85f,.12f),navy);
            Part(archive,"Reader instruction backing",new Vector3(0,2.48f,-4.85f),new Vector3(6,.85f,.1f),navy);
        }
        // Use small, fixed top-left labels rather than the legacy oversized text rectangles.
        welcome.gameObject.SetActive(false);
        if(root.Find("Wayfinding copy")==null)Text(root,"Wayfinding copy","WELCOME TO THE CAPITAL\nCafe left / Cinema right / Government ahead\nArchives east / Courtroom west",new Vector3(-2.45f,1.7f,2.44f),1.6f,4.9f,Color.white);
        var instruction=texts.First(t=>t.text.StartsWith("Select a book, then read"));instruction.gameObject.SetActive(false);
        var instructionBacking=archive.Find("Reader instruction backing");instructionBacking.localPosition=new Vector3(0,4.75f,5.8f);instructionBacking.localScale=new Vector3(8.6f,.5f,.1f);
        if(archive.Find("Reader instruction copy")==null)Text(archive,"Reader instruction copy","Select a book, then read here. Your selection is personal to you.",new Vector3(-4.05f,4.9f,5.72f),1.3f,8.1f,Color.white);
        var guideBacking=archive.Find("Field guide label backing");guideBacking.localPosition=new Vector3(-3,1.5f,-3.4f);guideBacking.localScale=new Vector3(2.5f,.5f,.1f);
        guideLabel.transform.localPosition=new Vector3(-4.1f,1.7f,-3.48f);guideLabel.fontSize=.85f;guideLabel.rectTransform.sizeDelta=new Vector2(2.2f,.5f);
    }
    public static void Review()
    {
        EditorSceneManager.OpenScene(Scene);SDValidateScene.Check();
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();
        if(reader.documents.Length!=11||reader.titles.Length!=11||!reader.documents.Any(d=>d.Contains("Ducks_Constitution")))throw new Exception("Cultural archive serialization check failed.");
        Debug.Log("CULTURE CHECK: eleven archive entries retained after scene reload.");
        string output=Environment.GetEnvironmentVariable("SIMDEM_REVIEW_OUTPUT");if(string.IsNullOrEmpty(output))throw new Exception("Set SIMDEM_REVIEW_OUTPUT for preview output.");Directory.CreateDirectory(output);
        var go=new GameObject("Review camera");var c=go.AddComponent<Camera>();c.nearClipPlane=.05f;c.farClipPlane=220;c.fieldOfView=72;
        Shot(c,"Courtroom",new Vector3(0,2.4f,-7),new Vector3(-1,2,6),output,"v04-Courtroom.png");
        Shot(c,"Courtroom",new Vector3(-4.7f,2.2f,1.6f),new Vector3(-6,1.8f,4.7f),output,"v04-Duck-Clerk.png");
        Shot(c,"Legislative Chamber",new Vector3(-3,2.4f,3.4f),new Vector3(-6.5f,1.8f,7),output,"v04-Reading-Room.png");
        Shot(c,"The Queer Quorum - Cafe",new Vector3(0,2.2f,-3.5f),new Vector3(3,2.1f,5),output,"v04-Cafe.png");
        Shot(c,"The Queer Quorum - Cafe",new Vector3(2,2.1f,-3),new Vector3(5,2,-6.8f),output,"v04-Risistas.png");
        Shot(c,"Secretary Name National Archives",new Vector3(0,2.3f,-8.8f),new Vector3(-1,2,3),output,"v04-Archives.png");
        Shot(c,"Courtroom",Vector3.zero,Vector3.zero,output,"v04-Square.png",true);
        UnityEngine.Object.DestroyImmediate(go);
    }
    static void Shot(Camera c,string roomName,Vector3 pos,Vector3 look,string folder,string name,bool square=false)
    {
        var room=GameObject.Find(roomName).transform;c.transform.position=square?new Vector3(0,3.2f,-13):room.TransformPoint(pos);c.transform.LookAt(square?new Vector3(0,1.7f,-2):room.TransformPoint(look));
        var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name),tex.EncodeToPNG());
        RenderTexture.active=null;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
    }
}
