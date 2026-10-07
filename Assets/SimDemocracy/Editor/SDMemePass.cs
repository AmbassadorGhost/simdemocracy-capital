using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UdonSharp;
using UdonSharp.Compiler;

public static class SDMemePass
{
    const string Art = "Assets/SimDemocracy/Art-v05";
    const string Scene = "Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v05.unity";
    [Serializable] public class Post { public string author,date,url,text; public int stars; }
    [Serializable] public class Catalog { public Post[] posts; }
    static Material navy,gold,paper;
    static TMP_FontAsset font;
    static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v03/"+name+".mat");
    static Transform Group(Transform p,string name,Vector3 pos,float yaw=0)
    { var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);return t; }
    static GameObject Part(Transform p,string name,Vector3 pos,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.receiveGI=ReceiveGI.LightProbes;
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic);return g;
    }
    static TMP_Text Label(Transform p,string name,string text,Vector3 pos,float width,float height,float size,Color color)
    {
        var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;
        var t=g.AddComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.richText=false;
        t.alignment=TextAlignmentOptions.TopLeft;t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.sizeDelta=new Vector2(width,height);
        t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;return t;
    }
    static void Sign(Transform p,string name,string text,Vector3 pos,float width,float yaw=0)
    {
        var g=Group(p,name,pos,yaw);Part(g,"Frame",Vector3.zero,new Vector3(width+.08f,.53f,.1f),gold);
        Part(g,"Face",new Vector3(0,0,-.065f),new Vector3(width,.45f,.04f),navy);
        Label(g,"Name",text,new Vector3(-width/2+.12f,.15f,-.1f),width-.24f,.35f,1.45f,Color.white);
    }
    static void Poster(Transform p,string image,Vector3 pos,float width,float yaw=0)
    {
        var path=Art+"/"+image+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
        importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);float height=width*texture.height/texture.width;
        var material=new Material(Shader.Find("VRChat/Mobile/Standard Lite"));material.color=Color.white;material.mainTexture=texture;
        material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(material,Art+"/"+image+".mat");
        var g=Group(p,image+" poster",pos,yaw);Part(g,"Frame",Vector3.zero,new Vector3(width+.1f,height+.1f,.08f),gold);
        Part(g,"Supplied image",new Vector3(0,0,-.051f),new Vector3(width,height,1),material,PrimitiveType.Quad);
    }
    static void Card(Transform p,Post post,int number,Vector3 pos,float width,float height,float yaw=0)
    {
        var g=Group(p,"Starboard post "+number,pos,yaw);
        Part(g,"Paper",Vector3.zero,new Vector3(width,height,.07f),paper);
        Part(g,"Gold pin",new Vector3(0,height/2-.09f,-.052f),new Vector3(.075f,.075f,.025f),gold,PrimitiveType.Sphere);
        Label(g,"Author",post.author,new Vector3(-width/2+.14f,height/2-.21f,-.05f),width-.95f,.3f,1.25f,new Color(.09f,.13f,.23f));
        Label(g,"Stars",post.stars+" stars",new Vector3(width/2-.73f,height/2-.21f,-.05f),.65f,.3f,.95f,new Color(.32f,.23f,.06f));
        var quote=Label(g,"Quote",post.text,new Vector3(-width/2+.14f,height/2-.61f,-.05f),width-.28f,height-.77f,1.65f,new Color(.08f,.1f,.17f));
        quote.enableAutoSizing=true;quote.fontSizeMin=.85f;quote.fontSizeMax=1.65f;
        quote.ForceMeshUpdate();
        Debug.Log("MEME FIT: post "+number+" font="+quote.fontSize+" height="+quote.preferredHeight);
        if(quote.isTextOverflowing)throw new Exception("Quote does not fit: "+post.author+" / "+number);
    }
    static void Simplify()
    {
        var all=UnityEngine.Object.FindObjectsOfType<Transform>(true);
        foreach(var t in all.Where(t=>new[]{"C03","C05","C06","C07","C21","C22","C23"}.Any(id=>t.name.StartsWith(id+" - "))).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        var court=GameObject.Find("Courtroom").transform;
        Sign(court,"Duck nameplate","CLERK",new Vector3(-6,1.02f,3.4f),1.2f);
        Sign(GameObject.Find("Legislative Chamber").transform,"Reading room sign","SENATE READING ROOM",new Vector3(-6.5f,2.55f,7.55f),3.7f);
        var root=court.parent;
        Sign(root.Find("Optional grass garden"),"Grass sign","TOUCHING GRASS IS OPTIONAL",new Vector3(0,1.8f,1.15f),3.8f);
        Sign(root.Find("Danyowave arrivals mural"),"Danyowave sign","DANYOWAVE",new Vector3(0,2.6f,-.05f),3.8f);
        foreach(var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
        {
            if(t.text.StartsWith("THE QUEER QUORUM"))t.text="THE QUEER QUORUM\n\nEspresso  2 Tau\nTea  1 Tau\nBiscuit  1 Tau";
            if(t.name=="Fictional biscuit bill")t.text="THE BISCUIT BILL\n1. Bring biscuits.\n2. Share biscuits.\n3. Debate who ate the last one.";
            if(t.text.StartsWith("SIMDEMOCRACY\nA republic"))t.text="SIMDEMOCRACY";
            if(t.name=="Reader instruction copy")t.text="SELECT A BOOK";
            if(t.name=="Field guide label")t.text="CULTURAL FIELD GUIDE";
            if(t.text.StartsWith("SIMDEM CINEMA\n"))t.text="SIMDEM CINEMA\n\nINTERMISSION";
            if(t.text.StartsWith("HOUSE RULES\n"))t.gameObject.SetActive(false);
        }
    }
    [MenuItem("SimDemocracy/Create Memes v0.5 Scene")]
    public static void Build()
    {
        if(File.Exists(Scene))throw new Exception("v0.5 exists; refusing to overwrite.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene("Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v04.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);
        navy=Existing("Midnight painted timber");gold=Existing("Brushed brass");paper=Existing("Glazed porcelain");
        font=UnityEngine.Object.FindObjectsOfType<TMP_Text>().First(t=>t.font!=null).font;
        Simplify();var cafe=GameObject.Find("The Queer Quorum - Cafe").transform;
        foreach(var window in cafe.GetComponentsInChildren<Transform>().Where(t=>t.name=="Rainy evening window"&&t.localPosition.x<0))window.gameObject.SetActive(false);
        Poster(cafe,"Risistas",new Vector3(5,2.65f,-7.68f),1.8f,180);
        var reveal=UnityEngine.Object.FindObjectsOfType<SDAction>(true).Single(a=>a.action==5).reveal;
        foreach(Transform child in reveal.transform)child.gameObject.SetActive(false);
        Poster(reveal.transform,"Bonk",new Vector3(0,2.7f,3.7f),5.3f);
        var posts=JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/SimDemocracy/Data/starboard-v05.json")).posts;
        var wall=Group(cafe,"Starboard",new Vector3(-9.5f,2.55f,0),-90);
        Part(wall,"Mounting board",Vector3.zero,new Vector3(10.1f,4.15f,.14f),navy);
        Card(wall,posts[0],1,new Vector3(-2.8f,0,-.13f),4,3.9f);
        Card(wall,posts[1],2,new Vector3(.7f,.3f,-.13f),2.7f,3.3f);
        Card(wall,posts[2],3,new Vector3(3.55f,.3f,-.13f),2.7f,3.3f);
        var small=Group(cafe,"Pinned replies",new Vector3(-5,2.6f,-7.65f),180);
        Part(small,"Mounting board",Vector3.zero,new Vector3(4.65f,2.45f,.12f),navy);
        Card(small,posts[3],4,new Vector3(-1.12f,0,-.12f),2.13f,2.2f);
        Card(small,posts[4],5,new Vector3(1.12f,0,-.12f),2.13f,2.2f);
        // The longer appointment response needs its own desk-sized card in the chamber.
        Card(GameObject.Find("Legislative Chamber").transform,posts[5],6,new Vector3(6.5f,2.55f,7.65f),3.7f,1.7f);
        Polish();
        var settings=UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);AssetDatabase.CreateAsset(settings,Art+"/LightingSettings.asset");Lightmapping.lightingSettings=settings;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions{IsEditorBuild=false});
        if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("Udon compile failed.");
        SDValidateScene.Check();
        if(!Lightmapping.Bake())throw new Exception("v0.5 bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        Debug.Log("MEME PASS COMPLETE: six sourced posts, two supplied images; "+Scene);
    }
    static void Polish()
    {
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true).Where(t=>t.transform.parent.name.StartsWith("Starboard post ")))text.color=Color.black;
        var cafe=GameObject.Find("The Queer Quorum - Cafe").transform;
        foreach(Transform t in cafe)
            if(new[]{"Terracotta planter","Plant stem","Leaf cluster"}.Contains(t.name)&&t.localPosition.x<0&&t.localPosition.z<3)
                t.localPosition+=new Vector3(0,0,4);
        var post=GameObject.Find("Legislative Chamber").transform.Find("Starboard post 6");post.localPosition=new Vector3(10.72f,2.55f,4);post.localRotation=Quaternion.Euler(0,90,0);
        var room=GameObject.Find("Suspicious Side Room").transform;
        var reveal=UnityEngine.Object.FindObjectsOfType<SDAction>(true).Single(a=>a.action==5).reveal;
        reveal.transform.Find("Bonk poster").localPosition=new Vector3(0,2.7f,4.8f);
        if(room.Find("Bonk back wall")==null)
        {
            var wall=Part(room,"Bonk back wall",new Vector3(0,1.85f,5),new Vector3(4,3.7f,.25f),Existing("Warm plaster"));wall.AddComponent<BoxCollider>();
            Part(room,"Bonk back wainscot",new Vector3(0,.8f,4.82f),new Vector3(4,1.4f,.08f),Existing("Midnight painted timber"));
        }
    }
    public static void Refine()
    {
        EditorSceneManager.OpenScene(Scene);Polish();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        if(!Lightmapping.Bake())throw new Exception("Final meme lighting bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Review();
        Debug.Log("MEME REFINEMENT COMPLETE");
    }
    public static void Review()
    {
        EditorSceneManager.OpenScene(Scene);SDValidateScene.Check();
        var texts=UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
        var quotes=texts.Where(t=>t.name=="Quote").ToArray();
        if(quotes.Length!=6)throw new Exception("Expected six quote cards.");
        foreach(var t in quotes){t.ForceMeshUpdate();if(t.isTextOverflowing)throw new Exception("Quote overflow: "+t.transform.parent.name);}
        if(UnityEngine.Object.FindObjectOfType<SDArchiveReader>().documents.Length!=11)throw new Exception("Archive changed unexpectedly.");
        var reveal=UnityEngine.Object.FindObjectsOfType<SDAction>(true).Single(a=>a.action==5).reveal;
        if(reveal.transform.Find("Bonk poster")==null)throw new Exception("Bonk not wired to reveal.");
        string output=Environment.GetEnvironmentVariable("SIMDEM_REVIEW_OUTPUT");Directory.CreateDirectory(output);
        var g=new GameObject("Preview camera");var c=g.AddComponent<Camera>();c.nearClipPlane=.05f;c.farClipPlane=200;c.fieldOfView=66;
        Shot(c,"The Queer Quorum - Cafe",new Vector3(-3,2.6f,0),new Vector3(-9.5f,2.55f,0),output,"v05-Starboard.png");
        Shot(c,"The Queer Quorum - Cafe",new Vector3(2.5f,2.4f,-3.7f),new Vector3(5,2.65f,-7.68f),output,"v05-Risistas.png");
        Shot(c,"The Queer Quorum - Cafe",new Vector3(-5,2.6f,-3.3f),new Vector3(-5,2.6f,-7.65f),output,"v05-Replies.png");
        Shot(c,"Legislative Chamber",new Vector3(7.5f,2.5f,1.6f),new Vector3(10.72f,2.55f,4),output,"v05-Chamber.png");
        bool previous=reveal.activeSelf;reveal.SetActive(true);
        Shot(c,"Suspicious Side Room",new Vector3(0,2.65f,-2.8f),new Vector3(0,2.7f,3.7f),output,"v05-Bonk.png");
        reveal.SetActive(previous);UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("MEME REVIEW PASSED: quote fit, archive entries, reveal reference, scene wiring.");
    }
    static void Shot(Camera c,string room,Vector3 pos,Vector3 look,string folder,string name)
    {
        var t=GameObject.Find(room).transform;c.transform.position=t.TransformPoint(pos);c.transform.LookAt(t.TransformPoint(look));
        var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
        var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
    }
}
