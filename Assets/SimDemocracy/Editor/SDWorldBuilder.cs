using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
using UdonSharp;
using UdonSharp.Compiler;
using VRC.SDK3.Components;

public static class SDWorldBuilder
{
    const string Base = "Assets/SimDemocracy";
    static Material navy, gold, stone, wood, green, cream, dark, pink, flag;
    static TMPro.TMP_FontAsset font;
    static Transform root, spawn;
    [Serializable] public class Entry { public string title, category, revision, timestamp, url, summary; }
    [Serializable] public class Catalog { public string imported; public Entry[] entries; }

    [MenuItem("SimDemocracy/Build Starter Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // A new scene and a unique file keep existing scene edits intact.
        // Validate inputs before replacing the current scene.
        if (AssetDatabase.LoadAssetAtPath<TextAsset>(Base + "/Data/archive_catalog.json") == null)
            throw new InvalidOperationException("Import the complete Assets/SimDemocracy folder first.");
        if (!EnsureTextResources()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(Base + "/Generated");
        Directory.CreateDirectory(Base + "/Scenes");
        AssetDatabase.Refresh();
        EnsurePrograms();
        navy = Mat("Navy", "#0A133D"); gold = Mat("Gold", "#FFE16A");
        stone = Mat("Stone", "#ABA797"); wood = Mat("Wood", "#674C3C");
        green = Mat("Leaves", "#49785B"); cream = Mat("Cream", "#F4E9D2");
        dark = Mat("Ink", "#182029"); pink = Mat("Rose", "#C96D98");
        flag = Mat("Flag", "#FFFFFF", true);
        flag.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Base + "/Textures/SimDemocracyFlag.jpeg");
        root = new GameObject("SimDemocracy - Capital v0.2").transform;
        Box(root,"Ground",new Vector3(0,-.2f,10),new Vector3(96,.4f,104),stone);
        Box(root,"Civic Square",new Vector3(0,.015f,0),new Vector3(24,.03f,28),cream);
        Box(root,"North Walk",new Vector3(0,.02f,27),new Vector3(5,.04f,48),cream);
        Box(root,"West Walk",new Vector3(-21,.02f,0),new Vector3(34,.04f,4),cream);
        Box(root,"East Walk",new Vector3(21,.02f,0),new Vector3(34,.04f,4),cream);
        spawn = new GameObject("Spawn - face the square").transform;
        spawn.SetParent(root); spawn.position = new Vector3(0,.15f,-9);
        var descriptor = new GameObject("VRCWorld").AddComponent<VRCSceneDescriptor>();
        descriptor.spawns = new Transform[] { spawn };
        descriptor.RespawnHeightY = -8;
        var camera = new GameObject("Reference Camera").AddComponent<Camera>();
        camera.backgroundColor = new Color(.065f,.08f,.15f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.farClipPlane = 160;
        camera.gameObject.SetActive(false);
        // Reference camera is optional; assign through the SDK inspector if desired.
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;
        var court = Room("Courtroom",-26,9,18,18,-90);
        var senate = Room("Legislative Chamber",0,26,22,16,0);
        var office = Room("President's Office",0,45,14,12,0);
        var library = Room("Secretary Name National Archives",26,9,20,20,90);
        var cafe = Room("The Queer Quorum - Cafe",-20,-17,20,16,180);
        var cinema = Room("Community Cinema",20,-17,20,16,180);
        var gag = Room("Suspicious Side Room",-39,-16,9,10,-90);
        Square(); Court(court); Senate(senate); Office(office); Library(library); Cafe(cafe); Cinema(cinema); Gag(gag);
        foreach (Transform room in new [] {court,senate,office,library,cafe,cinema,gag})
        {
            Vector3 floorSize=room.Find("Floor").localScale;
            var b = Button(room,"RETURN TO SQUARE",new Vector3(-floorSize.x/2+1.15f,1.1f,-floorSize.z/2+1),gold);
            var a = b.AddUdonSharpComponent<SDAction>(); a.action=6; a.destination=spawn; 
        }
        AssetDatabase.SaveAssets();
        string path=AssetDatabase.GenerateUniqueAssetPath(Base + "/Scenes/SimDemocracy-Capital-v02.unity");
        EditorSceneManager.SaveScene(scene,path);
        Selection.activeGameObject=root.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("SimDemocracy starter created: " + path + ". Run SDK validation, Udon compilation and Build & Test. This is an unverified prototype.");
    }
    static void EnsurePrograms()
    {
        foreach (string scriptPath in Directory.GetFiles(Base + "/Scripts", "*.cs"))
        {
            string normalized = scriptPath.Replace('\\','/');
            string programPath = Path.ChangeExtension(normalized,"asset");
            var program = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(programPath);
            if (program != null) continue;
            program = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            program.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(normalized);
            AssetDatabase.CreateAsset(program,programPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = true });
        if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError())
            throw new InvalidOperationException("Resolve UdonSharp compiler errors before building the scene.");
    }
    public static void BuildBatch()
    {
        try { Build(); if(font!=null) EditorApplication.Exit(0); }
        catch(Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
    static void FinishTextImport(string packageName)
    {
        AssetDatabase.importPackageCompleted -= FinishTextImport;
        ResumeAfterImport();
    }
    [InitializeOnLoadMethod]
    static void ResumeAfterImport()
    {
        if(!SessionState.GetBool("SimDemocracy.ImportingText",false)) return;
        EditorApplication.update -= ResumeTextBuild;
        EditorApplication.update += ResumeTextBuild;
    }
    static void ResumeTextBuild()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")==null) return;
        EditorApplication.update -= ResumeTextBuild;
        SessionState.SetBool("SimDemocracy.ImportingText",false);
        if(Application.isBatchMode) BuildBatch(); else Build();
    }
    static bool EnsureTextResources()
    {
        const string fontPath="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
        if(font!=null) return true;
        var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_Text).Assembly);
        AssetDatabase.importPackageCompleted -= FinishTextImport;
        AssetDatabase.importPackageCompleted += FinishTextImport;
        SessionState.SetBool("SimDemocracy.ImportingText",true);
        ResumeAfterImport();
        AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage"),false);
        return false;
    }
    static Material Mat(string name,string hex,bool textured=false)
    {
        string path=Base+"/Generated/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(textured?"Unlit/Texture":"Unlit/Color"));AssetDatabase.CreateAsset(m,path);}
        Color c; ColorUtility.TryParseHtmlString(hex,out c);if(m.HasProperty("_Color"))m.color=c;
        return m;
    }
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 scale,Material m)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic);return g;
    }
    static TMPro.TMP_Text Text(Transform parent,string message,Vector3 p,float size,Color color)
    {
        var g=new GameObject("Label - "+message.Split('\n')[0]);g.transform.SetParent(parent,false);g.transform.localPosition=p;
        var t=g.AddComponent<TMPro.TextMeshPro>();t.font=font;t.fontSize=size*14f;t.rectTransform.sizeDelta=new Vector2(100,50);t.rectTransform.pivot=new Vector2(.5f,1);t.alignment=TMPro.TextAlignmentOptions.Top;t.enableWordWrapping=false;t.overflowMode=TMPro.TextOverflowModes.Overflow;t.color=color;t.text=message;
        return t;
    }
    static void Flag(Transform p,Vector3 pos,float width)
    {
        // Quad front faces local -Z, matching labels and room entrances.
        var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="Original SimDemocracy Flag";g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=new Vector3(width,width*297f/459f,1);g.GetComponent<Renderer>().sharedMaterial=flag;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    static Transform Room(string name,float x,float z,float w,float d,float yaw)
    {
        var r=new GameObject(name).transform;r.SetParent(root,false);r.localPosition=new Vector3(x,0,z);r.localRotation=Quaternion.Euler(0,yaw,0);
        Box(r,"Floor",new Vector3(0,.05f,0),new Vector3(w,.1f,d),wood);
        Box(r,"West wall",new Vector3(-w/2,2.5f,0),new Vector3(.25f,5,d),navy);
        Box(r,"East wall",new Vector3(w/2,2.5f,0),new Vector3(.25f,5,d),navy);
        float side=(w-4)/2;
        foreach(float end in new[]{-d/2,d/2}) {
            Box(r,"Door left",new Vector3(-(w+4)/4,2.5f,end),new Vector3(side,5,.25f),navy);
            Box(r,"Door right",new Vector3((w+4)/4,2.5f,end),new Vector3(side,5,.25f),navy);
            Box(r,"Door lintel",new Vector3(0,4.35f,end),new Vector3(4,1.3f,.25f),navy);
        }
        Box(r,"Roof - hide in editor for cutaway",new Vector3(0,5.15f,0),new Vector3(w+.4f,.25f,d+.4f),navy);
        Box(r,"Entrance trim",new Vector3(0,3.75f,-d/2-.15f),new Vector3(w,.12f,.15f),gold);
        // Repeated pilasters, cornices and wall insets make the simple shells readable.
        foreach(float pillarX in new[]{-w/2+.35f,w/2-.35f})
        {
            Box(r,"Corner pilaster",new Vector3(pillarX,2.5f,-d/2-.2f),new Vector3(.5f,5,.45f),cream);
            Box(r,"Column foot",new Vector3(pillarX,.25f,-d/2-.2f),new Vector3(.75f,.5f,.7f),gold);
        }
        Box(r,"Gold cornice",new Vector3(0,4.95f,-d/2-.2f),new Vector3(w+.6f,.18f,.5f),gold);
        foreach(float sideX in new[]{-w/2+.14f,w/2-.14f})
            for(int panel=0;panel<3;panel++)
                Box(r,"Interior wall panel",new Vector3(sideX,2.4f,(panel-1)*d/4),new Vector3(.06f,2.7f,d/5),Mat("Wall inset","#233253"));
        Box(r,"Central rug",new Vector3(0,.112f,0),new Vector3(w*.38f,.02f,d*.65f),Mat("Rug","#453F58"));
        Text(r,name,new Vector3(0,4.55f,-d/2-.3f),.22f,Color.white);
        return r;
    }
    static GameObject Button(Transform p,string title,Vector3 pos,Material m)
    {
        var b=Box(p,title,pos,new Vector3(1.7f,.45f,.28f),m);
        Box(p,"Control stand",new Vector3(pos.x,(pos.y-.225f)/2,pos.z+.1f),new Vector3(.12f,pos.y-.225f,.12f),wood);
        Text(p,title,pos+new Vector3(0,.11f,-.16f),.095f,Color.black);return b;
    }
    static void Chair(Transform p,Vector3 pos,Material m,float yaw=0)
    {
        var assembly=new GameObject("Chair").transform;assembly.SetParent(p,false);assembly.localPosition=pos;assembly.localRotation=Quaternion.Euler(0,yaw,0);
        p=assembly;pos=Vector3.zero;
        Box(p,"Seat",pos+new Vector3(0,.5f,0),new Vector3(.9f,.2f,.85f),m);
        Box(p,"Back",pos+new Vector3(0,.95f,-.36f),new Vector3(.9f,.9f,.14f),m);
        Box(p,"Base",pos+new Vector3(0,.25f,0),new Vector3(.6f,.5f,.6f),wood);
        Seat(p,pos+new Vector3(0,.6f,0),pos+new Vector3(.7f,.15f,0));
    }
    static void Seat(Transform parent,Vector3 position,Vector3 exitPosition)
    {
        var seat=new GameObject("Sit here");seat.transform.SetParent(parent,false);seat.transform.localPosition=position;
        var collider=seat.AddComponent<BoxCollider>();collider.size=new Vector3(.8f,.35f,.7f);collider.isTrigger=true;
        var enter=new GameObject("Seat pose").transform;enter.SetParent(seat.transform,false);
        var exit=new GameObject("Safe exit").transform;exit.SetParent(parent,false);exit.localPosition=exitPosition;
        var station=seat.AddComponent<VRCStation>();station.seated=true;station.disableStationExit=false;
        station.stationEnterPlayerLocation=enter;station.stationExitPlayerLocation=exit;
        var behaviour=seat.AddUdonSharpComponent<SDSeat>();behaviour.station=station;
    }
    static void Desk(Transform p,Vector3 pos,float width)
    {
        Box(p,"Desk",pos+new Vector3(0,.85f,0),new Vector3(width,.22f,1.2f),wood);
        Box(p,"Desk base",pos+new Vector3(0,.4f,.2f),new Vector3(width-.2f,.8f,.5f),navy);
    }
    static void Square()
    {
        Box(root,"Monument",new Vector3(0,.5f,3),new Vector3(3,1,3),navy);
        Box(root,"Flag monument",new Vector3(0,2.2f,3),new Vector3(4,2.4f,.3f),gold);
        Flag(root,new Vector3(0,2.2f,2.82f),3.7f);
        Text(root,"SIMDEMOCRACY\nA republic. A community. A place to meet.",new Vector3(0,3.8f,2.7f),.22f,Color.black);
        Text(root,"WELCOME TO THE CAPITAL\nCafe left | Cinema right | Government ahead\nArchives east | Courtroom west",new Vector3(0,2.4f,-5),.16f,Color.black);
        foreach(float x in new[]{-7f,7f}) {
            Box(root,"Square bench",new Vector3(x,.5f,3),new Vector3(3,.4f,1),wood);
            Box(root,"Planter",new Vector3(x,.6f,7),new Vector3(1.5f,1.2f,1.5f),navy);
            Box(root,"Stylized foliage",new Vector3(x,1.6f,7),new Vector3(1.7f,1.4f,1.7f),green);
            Seat(root,new Vector3(x,.72f,3),new Vector3(x,.15f,4.3f));
        }
        foreach(float x in new[]{-10f,10f}) foreach(float z in new[]{-7f,10f}) {
            Box(root,"Lantern post",new Vector3(x,1.5f,z),new Vector3(.14f,3,.14f),dark);
            Box(root,"Warm lantern",new Vector3(x,3,z),new Vector3(.55f,.7f,.55f),gold);
            Box(root,"Lantern cap",new Vector3(x,3.4f,z),new Vector3(.75f,.12f,.75f),navy);
        }
    }
    static void Court(Transform r)
    {
        Desk(r,new Vector3(0,0,5),6);Flag(r,new Vector3(0,3.1f,7),3);
        Desk(r,new Vector3(-3,0,0),2.5f);Desk(r,new Vector3(3,0,0),2.5f);
        for(int row=0;row<2;row++)for(int col=0;col<6;col++)Chair(r,new Vector3((col<3 ? col-3 : col-2)*1.5f,0,-3-row*1.6f),cream);
        var caption=Text(r,"The court is in session",new Vector3(0,2,4.3f),.17f,Color.white);
        var g=Box(r,"Interactable gavel",new Vector3(0,1.1f,4.8f),new Vector3(.7f,.25f,.3f),gold);
        Box(r,"Gavel handle",new Vector3(0,1.08f,4.4f),new Vector3(.13f,.13f,.7f),wood);
        var audio=g.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Base+"/Data/gavel.wav");audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=1;audio.maxDistance=15;audio.volume=.4f;
        var a=g.AddUdonSharpComponent<SDGavel>();a.sound=audio;a.caption=caption;
    }
    static void Senate(Transform r)
    {
        Flag(r,new Vector3(7,3.8f,6.2f),2.5f);Desk(r,new Vector3(0,0,4.5f),3);
        Text(r,"THE FLOOR IS YOURS",new Vector3(0,2.4f,4),.18f,Color.white);
        for(int row=0;row<2;row++) for(int i=0;i<7;i++) {
            float deskX=i<3?(i-3)*2.3f:(i-2)*2f;
            Vector3 p=new Vector3(deskX,0,1-row*3-Mathf.Abs(deskX)*.15f);
            Desk(r,p,1.35f);Chair(r,p+new Vector3(0,0,-1),cream);
        }
        Session(r);
    }
    static void Session(Transform r)
    {
        Box(r,"Session display",new Vector3(0,3.4f,7.3f),new Vector3(9.5f,2.7f,.2f),dark);
        var controller=new GameObject("Shared debate session");controller.transform.SetParent(r,false);
        var session=controller.AddUdonSharpComponent<SDSession>();
        session.board=Text(r,"THE CHAMBER / INFORMAL POLL\nShould the next event be a movie night?\n\nAYE  0     NAY  0     ABSTAIN  0\nVOTING CLOSED\nRound 0 | 0/128 participants",new Vector3(0,4.6f,7.15f),.12f,Color.white);
        session.clock=Text(r,"SPEAKING TIME  3:00\nPAUSED / READY",new Vector3(-7,3.8f,6),.16f,Color.white);
        session.feedback=Text(r,"Your vote",new Vector3(0,1.7f,2.8f),.10f,Color.white);
        session.motions=new[]{"Should the next event be a movie night?","Shall we adjourn to the Queer Quorum?","Should the gavel receive its own office?"};
        
        string[] labels={"AYE","NAY","ABSTAIN","OPEN POLL","CLOSE POLL","NEXT MOTION","START CLOCK","PAUSE CLOCK","RESET CLOCK"};
        for(int i=0;i<labels.Length;i++)
        {
            Vector3 p=i<3 ? new Vector3((i-1)*2,1.15f,2.8f) : new Vector3(5+(i-3)%3*1.85f,1.2f+(i<6?.7f:0),4.7f);
            var b=Button(r,labels[i],p,i<3?gold:cream);
            var a=b.AddUdonSharpComponent<SDSessionButton>();a.session=session;a.action=i;
        }
        Text(r,"INSTANCE MASTER CONTROLS",new Vector3(6.8f,2.6f,4.7f),.10f,Color.white);
    }
    static void Office(Transform r)
    {
        Desk(r,new Vector3(0,0,1),4);Chair(r,new Vector3(0,0,2.3f),gold,180);Flag(r,new Vector3(0,3.1f,4),3);
        Text(r,"OFFICE OF THE PRESIDENT",new Vector3(0,2.3f,3.8f),.16f,Color.white);
        Text(r,"INBOX\nMore amendments",new Vector3(-1,1.3f,.4f),.09f,Color.white);
    }
    static void Library(Transform r)
    {
        var data=AssetDatabase.LoadAssetAtPath<TextAsset>(Base+"/Data/archive_catalog.json");
        Catalog catalog=data==null?null:JsonUtility.FromJson<Catalog>(data.text);
        if(catalog==null || catalog.entries==null || catalog.entries.Length==0) throw new InvalidOperationException("Archive catalog missing or empty.");
        Box(r,"Reading screen backing",new Vector3(0,2.5f,6.1f),new Vector3(8.6f,4.1f,.2f),dark);
        var text=Text(r,"Archive reader",new Vector3(-3.9f,4.2f,5.95f),.14f,Color.white);text.rectTransform.pivot=new Vector2(0,1);text.alignment=TMPro.TextAlignmentOptions.TopLeft;
        var reader=new GameObject("Local archive reader");reader.transform.SetParent(r,false);
        var ar=reader.AddUdonSharpComponent<SDArchiveReader>();ar.display=text;
        ar.titles=catalog.entries.Select(e=>e.title).ToArray();
        ar.documents=catalog.entries.Select(e=>Wrap("CATALOG SAMPLE - not the full archive\n"+e.category+"\n\n"+e.summary+"\n\nRevision: "+e.revision+"\nSource updated: "+e.timestamp+"\nImported: "+catalog.imported+"\nSource: "+e.url,66)).ToArray();
        
        string[] names={"PREV ENTRY","NEXT ENTRY","PREV PAGE","NEXT PAGE"};int[] actions={2,1,4,3};
        for(int i=0;i<4;i++){var b=Button(r,names[i],new Vector3(-3+i*2,1,5.65f),gold);var a=b.AddUdonSharpComponent<SDAction>();a.reader=ar;a.action=actions[i];}
        string[] categories={"CONSTITUTION","LEGISLATION","CASE LAW","ORDERS","TREATIES","HISTORY"};
        for(int i=0;i<6;i++) {
            float x=i<3?-7:7;float z=-4+(i%3)*3;
            Box(r,"Shelf - "+categories[i],new Vector3(x,1.5f,z),new Vector3(2,3,.65f),wood);
            Text(r,categories[i],new Vector3(x,3.35f,z-.4f),.13f,Color.white);
            for(int b=0;b<5;b++)Box(r,"Decorative volume",new Vector3(x-.72f+b*.35f,1.6f,z-.45f),new Vector3(.23f,1.5f,.35f),b%2==0?navy:gold);
        }
        for(int i=0;i<Mathf.Min(catalog.entries.Length,3);i++) {
            float x=-2.5f+i*2.5f;
            Desk(r,new Vector3(x,0,-1),1.8f);
            var book=Box(r,"Read - "+catalog.entries[i].title,new Vector3(x,1.05f,-1),new Vector3(1,.15f,.8f),gold);
            Text(r,catalog.entries[i].title,new Vector3(x,1.4f,-1.6f),.10f,Color.white);
            var a=book.AddUdonSharpComponent<SDAction>();a.reader=ar;a.entry=i;
        }
        Text(r,"Select a book, then read at the screen.\nYour selection is personal to you.",new Vector3(0,2.8f,-5),.17f,Color.white);
    }
    static string Wrap(string input,int width)
    {
        var output=new StringBuilder();
        foreach(var line in input.Split('\n')) {
            int col=0;
            foreach(var word in line.Split(' ')) {
                if(col>0 && col+1+word.Length>width){output.Append('\n');col=0;}
                if(col>0){output.Append(' ');col++;}
                string rest=word;
                while(rest.Length>width){output.Append(rest.Substring(0,width));output.Append('\n');rest=rest.Substring(width);col=0;}
                output.Append(rest);col+=rest.Length;
            }
            output.Append('\n');
        }
        return output.ToString();
    }
    static void Cafe(Transform r)
    {
        Box(r,"Coffee counter",new Vector3(0,.7f,5),new Vector3(8,1.4f,1.1f),wood);
        Text(r,"THE QUEER QUORUM\nCoffee, company & questionable motions",new Vector3(0,3.5f,5.5f),.2f,Color.white);
        string[] colors={"#E64545","#F29C38","#F3D64E","#4EAC65","#4877CD","#9854B3"};
        for(int i=0;i<6;i++)Box(r,"Pride stripe",new Vector3(-7,3.6f-i*.2f,5.8f),new Vector3(2,.2f,.08f),Mat("Pride"+i,colors[i]));
        for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++) {
            Vector3 p=new Vector3(side*5,0,-1-row*3);
            Box(r,"Sofa",p+new Vector3(0,.6f,0),new Vector3(3,.6f,1.2f),pink);
            Box(r,"Sofa back",p+new Vector3(0,1.1f,-.5f),new Vector3(3,1,.25f),pink);
            Desk(r,p+new Vector3(0,-.25f,1.5f),1.6f);
            foreach(float offset in new[]{-.85f,.85f}) Seat(r,p+new Vector3(offset,.9f,0),p+new Vector3(offset,.15f,.9f));
            Box(r,"Coffee mug",p+new Vector3(0,.85f,1.5f),new Vector3(.18f,.24f,.18f),cream);
            Box(r,"Coffee",p+new Vector3(0,.975f,1.5f),new Vector3(.14f,.01f,.14f),wood);
        }
        Text(r,"HOUSE RULES\nBe kind. Make room.\nEveryone belongs here.",new Vector3(5,2.4f,5.75f),.13f,Color.white);
        foreach(float x in new[]{-8f,8f}) {
            Box(r,"Cafe planter",new Vector3(x,.4f,2),new Vector3(.7f,.8f,.7f),cream);
            Box(r,"Cafe foliage",new Vector3(x,1.3f,2),new Vector3(1,1.2f,1),green);
        }
        Flag(r,new Vector3(6,3.1f,5.8f),2.5f);
    }
    static void Cinema(Transform r)
    {
        Box(r,"Video Screen - replace with video prefab output",new Vector3(0,3,5.8f),new Vector3(12,3.8f,.2f),dark);
        Text(r,"SIMDEM CINEMA\nMovie nights & community edits\n\nVideo player installation required\nSee README",new Vector3(0,4.2f,5.65f),.22f,Color.white);
        for(int row=0;row<3;row++)for(int col=0;col<8;col++)Chair(r,new Vector3((col<4 ? col-4 : col-3)*1.45f,0,1-row*2),navy);
    }
    static void Gag(Transform r)
    {
        Text(r,"star wars furry porn",new Vector3(0,3.1f,-5.2f),.18f,Color.white);
        var dog=new GameObject("Local Doge reveal");dog.transform.SetParent(r,false);
        var p=dog.transform;
        Box(p,"Doge head",new Vector3(0,2,1),new Vector3(1.8f,1.6f,1.3f),gold);
        Box(p,"Muzzle",new Vector3(0,1.65f,.2f),new Vector3(1.2f,.8f,.5f),cream);
        Box(p,"Nose",new Vector3(0,1.85f,-.08f),new Vector3(.25f,.2f,.1f),dark);
        foreach(float x in new[]{-.5f,.5f}) {
            Box(p,"Eye",new Vector3(x,2.2f,.3f),new Vector3(.17f,.17f,.1f),dark);
            var ear=Box(p,"Ear",new Vector3(x,3,1),new Vector3(.6f,.8f,.6f),gold);ear.transform.localRotation=Quaternion.Euler(0,0,x*30);
        }
        var bat=Box(p,"Bonk bat",new Vector3(1.6f,2,1),new Vector3(.25f,2.8f,.25f),wood);bat.transform.localRotation=Quaternion.Euler(0,0,-30);
        Text(p,"BONK\nGO TO HORNY JAIL",new Vector3(0,4,0),.22f,Color.white);
        dog.SetActive(false);
        var b=Button(r,"ENTER IF YOU DARE",new Vector3(0,1.2f,-3),gold);
        var a=b.AddUdonSharpComponent<SDAction>();a.action=5;a.reveal=dog;
    }
}


