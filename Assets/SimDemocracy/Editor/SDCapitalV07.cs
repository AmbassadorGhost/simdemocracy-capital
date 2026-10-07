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
using VRC.SDK3.Components;
using VRC.Udon;

// v0.7: working cinema, dusk, fountain and timeline, presidents wall, desk drawers,
// witness stand, pickup mugs, optional cafe mirror and the full Constitution.
public static class SDCapitalV07
{
    const string Art="Assets/SimDemocracy/Art-v07";
    const string Source="Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v06.unity";
    public const string Scene="Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v07.unity";
    const string Samples="Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/";
    static readonly string[] RoomNames={"Courtroom","Legislative Chamber","President's Office","Secretary Name National Archives","The Queer Quorum - Cafe","Community Cinema","Suspicious Side Room"};
    static int meshNumber;
    static TMP_FontAsset font;
    static Material textMaterial;
    static Material dusk,rainy,felt,water,biscuit;
    static Transform root;

    [Serializable] public class ConstitutionPart{public string title,text;}
    [Serializable] public class Constitution{public string source,timestamp;public int revision;public ConstitutionPart[] parts;}
    [Serializable] public class Presidency{public int n;public string party,name;}
    [Serializable] public class Presidents{public string source,gap;public Presidency[] presidencies;}

    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v03/"+name+".mat");
    static Transform Room(string name)=>GameObject.Find(name).transform;
    static Color Hex(string h){ColorUtility.TryParseHtmlString(h,out var c);return c;}

    // Lightmapped box with its own mesh asset (same approach as the v0.6 walkthrough fix).
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
    // Small probe-lit detail.
    static GameObject Part(Transform p,string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=false,bool isStatic=true)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;
        if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.receiveGI=ReceiveGI.LightProbes;
        GameObjectUtility.SetStaticEditorFlags(g,isStatic?StaticEditorFlags.BatchingStatic:0);return g;
    }
    static Transform Group(Transform p,string name,Vector3 pos,float yaw=0)
    {var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
    static TMP_Text Label(Transform p,string name,string text,Vector3 pos,Vector2 size,float fontSize,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;
        var t=g.AddComponent<TextMeshPro>();t.font=font;if(textMaterial!=null)t.fontSharedMaterial=textMaterial;
        t.text=text;t.fontSize=fontSize;t.color=color;t.richText=false;t.alignment=align;
        t.rectTransform.pivot=new Vector2(.5f,.5f);t.rectTransform.sizeDelta=size;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;
        return t;
    }
    static Material Surface(string name,Color color,Texture texture=null,float gloss=0)
    {
        var m=new Material(Shader.Find("VRChat/Mobile/Standard Lite"));m.color=color;if(texture!=null)m.mainTexture=texture;
        m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",gloss);AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;
    }
    static void BakedLight(Transform parent,string name,Vector3 pos,Color color,float intensity,float range)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;
    }
    static void Seat(Transform parent,Vector3 position,Quaternion rotation,Vector3 exitPosition)
    {
        var seat=new GameObject("Sit here");seat.transform.SetParent(parent,false);seat.transform.localPosition=position;seat.transform.localRotation=rotation;
        var collider=seat.AddComponent<BoxCollider>();collider.size=new Vector3(.8f,.35f,.7f);collider.isTrigger=true;
        var enter=new GameObject("Seat pose").transform;enter.SetParent(seat.transform,false);
        var exit=new GameObject("Safe exit").transform;exit.SetParent(parent,false);exit.localPosition=exitPosition;exit.localRotation=rotation;
        var station=seat.AddComponent<VRCStation>();station.seated=true;station.disableStationExit=false;
        station.stationEnterPlayerLocation=enter;station.stationExitPlayerLocation=exit;
        var behaviour=seat.AddUdonSharpComponent<SDSeat>();behaviour.station=station;
    }
    static void InteractText<T>(T behaviour,string text) where T:UdonSharpBehaviour
    {
        var backing=UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);if(backing!=null){backing.interactText=text;EditorUtility.SetDirty(backing);}
    }
    static Bounds LocalBounds(Transform space,Bounds world)
    {
        var b=new Bounds(space.InverseTransformPoint(world.center),Vector3.zero);
        for(int i=0;i<8;i++)b.Encapsulate(space.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
        return b;
    }

    static void EnsurePrograms()
    {
        foreach(var name in new[]{"SDCinema","SDCinemaButton"})
        {
            string cs="Assets/SimDemocracy/Scripts/"+name+".cs",asset="Assets/SimDemocracy/Scripts/"+name+".asset";
            if(AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(asset)!=null)continue;
            var program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(cs);
            AssetDatabase.CreateAsset(program,asset);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions{IsEditorBuild=true});
        if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("UdonSharp compile failed.");
    }

    static Texture2D WindowTexture(string name,bool rain)
    {
        const int w=256,h=512;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);var px=new Color[w*h];var rng=new System.Random(rain?11:7);
        Color top=Hex("#1A2148"),mid=Hex("#59508A"),low=Hex("#D9876F"),horizon=Hex("#F4C17E"),far=Hex("#463E5E"),near=Hex("#2A2638");
        var ridgeFar=new float[w];var ridgeNear=new float[w];
        for(int x=0;x<w;x++){float u=x/(float)w;ridgeFar[x]=.17f+.03f*Mathf.Sin(u*8.7f+1.1f)+.015f*Mathf.Sin(u*27f);ridgeNear[x]=.1f+.025f*Mathf.Sin(u*5.3f+2.2f)+.012f*Mathf.Sin(u*33f+.5f);}
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float v=y/(float)h;Color c=v>.55f?Color.Lerp(mid,top,(v-.55f)/.45f):v>.25f?Color.Lerp(low,mid,(v-.25f)/.3f):Color.Lerp(horizon,low,(v-.12f)/.13f);
            if(v<ridgeFar[x])c=far;if(v<ridgeNear[x])c=near;px[y*w+x]=c;
        }
        for(int i=0;i<45;i++){int x=rng.Next(w),y=(int)(h*(.68f+.3f*(float)rng.NextDouble()));px[y*w+x]=Color.Lerp(px[y*w+x],Color.white,.75f);}
        if(rain)for(int i=0;i<170;i++){int x=rng.Next(w),y0=rng.Next(h),len=10+rng.Next(30);for(int k=0;k<len&&y0-k>=0;k++){int i2=(y0-k)*w+x;px[i2]=Color.Lerp(px[i2],Color.white,.28f*(1-k/(float)len));}}
        tex.SetPixels(px);tex.Apply();string path=Art+"/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=512;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Unlit(string name,Texture texture)
    {var m=new Material(Shader.Find("Unlit/Texture"));m.mainTexture=texture;AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;}

    static void Materials()
    {
        dusk=Unlit("Dusk window",WindowTexture("Dusk-window",false));
        rainy=Unlit("Rainy dusk window",WindowTexture("Dusk-window-rain",true));
        felt=Surface("Acoustic felt",Hex("#3A4258"),AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SimDemocracy/Art-v03/Linen-weave.png"));
        water=Surface("Fountain water",Hex("#3F6F8C"),null,.9f);
        biscuit=Surface("Biscuit",Hex("#D2A05E"));
    }

    static Color HorizonHaze=>Color.Lerp(Hex("#8C7484"),Hex("#D9989C"),.55f);

    // Painted equirectangular dusk: indigo zenith, mauve, orange horizon, sun glow on the light's bearing.
    static Texture2D SkyTexture(Vector3 lightForward)
    {
        const int w=2048,h=1024;var px=new Color[w*h];var rng=new System.Random(5);
        var flat=new Vector3(-lightForward.x,0,-lightForward.z).normalized;var sun=(flat*Mathf.Cos(4*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(4*Mathf.Deg2Rad)).normalized;
        Color zenith=Hex("#1C2150"),high=Hex("#3B4A8A"),mid=Hex("#9A7EA8"),ground=Hex("#35303D"),glowColor=Hex("#FFD39A"),hazeAtHorizon=HorizonHaze;
        Color warmAway=Hex("#D9989C"),warmToward=Hex("#F7C47C"),lowAway=Hex("#C98A98"),lowToward=Hex("#EE9E72"),disc=Hex("#FFF4DC");
        for(int y=0;y<h;y++)
        {
            float lat=(1-(y+.5f)/h)*Mathf.PI,dy=Mathf.Cos(lat),s=Mathf.Sin(lat),e=Mathf.Asin(dy)*Mathf.Rad2Deg;
            for(int x=0;x<w;x++)
            {
                float lon=(.5f-(x+.5f)/w)*2*Mathf.PI;var d=new Vector3(s*Mathf.Cos(lon),dy,s*Mathf.Sin(lon));
                float toward=Mathf.Pow(Mathf.Max(0,Vector3.Dot(new Vector3(d.x,0,d.z).normalized,flat)),2);
                Color warm=Color.Lerp(warmAway,warmToward,toward),low=Color.Lerp(lowAway,lowToward,toward);
                // At and below the horizon the sky equals the fog colour, so distant fogged ground meets it without a seam.
                Color c=e<0?Color.Lerp(hazeAtHorizon,ground,Mathf.Clamp01(-e/20f))
                    :e<3?Color.Lerp(hazeAtHorizon,warm,e/3)
                    :e<12?Color.Lerp(warm,low,(e-3)/9)
                    :e<30?Color.Lerp(low,mid,(e-12)/18)
                    :e<60?Color.Lerp(mid,high,(e-30)/30)
                    :Color.Lerp(high,zenith,(e-60)/30);
                float ang=Mathf.Acos(Mathf.Clamp(Vector3.Dot(d,sun),-1,1));
                c+=glowColor*(Mathf.Exp(-ang/.2f)*.45f+Mathf.Exp(-ang/.05f)*.5f);
                if(ang<.032f)c=Color.Lerp(c,disc,Mathf.InverseLerp(.032f,.024f,ang));
                px[y*w+x]=c;
            }
        }
        for(int i=0;i<260;i++)
        {
            int x=rng.Next(w),y=(int)(h*(.72f+.27f*(float)rng.NextDouble()));float b=.35f+.45f*(float)rng.NextDouble();
            for(int k=0;k<4;k++){int i2=Mathf.Min(h-1,y+k/2)*w+Mathf.Min(w-1,x+k%2);px[i2]=Color.Lerp(px[i2],Color.white,b);}
        }
        var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.SetPixels(px);tex.Apply();
        string path=Art+"/Dusk-sky.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
        importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Dusk()
    {
        var sun=GameObject.Find("Late afternoon sun");sun.name="Dusk sun";sun.transform.rotation=Quaternion.Euler(14,115,0);
        var light=sun.GetComponent<Light>();light.color=Hex("#FFB47E");light.intensity=1.05f;RenderSettings.sun=light;
        var sky=new Material(Shader.Find("Skybox/Panoramic"));sky.SetTexture("_MainTex",SkyTexture(sun.transform.forward));
        sky.SetFloat("_Mapping",1);sky.SetFloat("_ImageType",0);sky.SetFloat("_MirrorOnBack",0);sky.SetFloat("_Layout",0);sky.SetFloat("_Exposure",1);sky.SetFloat("_Rotation",0);
        sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");sky.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
        AssetDatabase.CreateAsset(sky,Art+"/Dusk sky.mat");RenderSettings.skybox=sky;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("#6F70A2");RenderSettings.ambientEquatorColor=Hex("#C29084");RenderSettings.ambientGroundColor=Hex("#403A45");RenderSettings.ambientIntensity=.85f;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=HorizonHaze;RenderSettings.fogStartDistance=38;RenderSettings.fogEndDistance=150;
        foreach(var lantern in root.Cast<Transform>().Where(t=>t.name=="Warm lantern").ToArray())
            BakedLight(root,"Lantern light",lantern.localPosition-Vector3.up*.2f,Hex("#FFC27A"),2.4f,9);
    }

    static bool Covered(Transform room,Renderer panel,Bounds p)
    {
        float side=Mathf.Sign(p.center.x),plane=p.center.x;
        foreach(var r in room.GetComponentsInChildren<Renderer>())
        {
            if(r==panel)continue;var b=LocalBounds(room,r.bounds);
            if(side*(plane-b.center.x)<=0)continue;
            float wallEdge=side>0?b.max.x:b.min.x;float depth=side*(plane-wallEdge);
            if(depth<-.1f||depth>.7f)continue;
            if(b.max.y<1.75f||b.min.y>3.65f)continue;
            if(Mathf.Min(b.max.z,p.max.z)-Mathf.Max(b.min.z,p.min.z)<.25f)continue;
            return true;
        }
        return false;
    }
    static Transform WallMount(Transform room,string name,float side,float z,float halfWidth)
    {
        // Group origin sits on the inner wall face; local -Z faces into the room.
        return Group(room,name,new Vector3(side*(halfWidth-.125f),0,z),side*90);
    }
    static void Window(Transform g,float width,Material glass)
    {
        const float h=1.9f,y=2.7f;
        var q=Part(g,"Dusk glass",new Vector3(0,y,-.012f),new Vector3(width,h,1),glass,PrimitiveType.Quad);q.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        var walnut=Mat("Walnut");var brass=Mat("Brushed brass");
        Part(g,"Window head",new Vector3(0,y+h/2+.06f,-.06f),new Vector3(width+.24f,.12f,.12f),walnut);
        Part(g,"Window sill",new Vector3(0,y-h/2-.04f,-.1f),new Vector3(width+.36f,.08f,.2f),Mat("Warm plaster"));
        foreach(float s in new[]{-1f,1f})Part(g,"Window jamb",new Vector3(s*(width/2+.06f),y,-.06f),new Vector3(.12f,h,.12f),walnut);
        Part(g,"Window mullion",new Vector3(0,y,-.035f),new Vector3(.05f,h,.05f),brass);
        Part(g,"Window transom",new Vector3(0,y+h*.18f,-.035f),new Vector3(width,.05f,.05f),brass);
    }
    static void WindowsAndPanels()
    {
        int windows=0,panels=0,kept=0;
        foreach(var name in RoomNames)
        {
            var room=Room(name);float half=LocalBounds(room,room.Find("Floor").GetComponent<Renderer>().bounds).size.x/2;
            bool dark=name=="Community Cinema"||name=="Suspicious Side Room";
            foreach(var panel in room.Cast<Transform>().Where(t=>t.name=="Interior wall panel"&&t.gameObject.activeSelf).ToArray())
            {
                var renderer=panel.GetComponent<Renderer>();var b=LocalBounds(room,renderer.bounds);
                if(Covered(room,renderer,b)){kept++;continue;}
                panel.gameObject.SetActive(false);float side=Mathf.Sign(b.center.x);
                if(dark)
                {
                    var g=WallMount(room,"Acoustic panel",side,b.center.z,half);float w=b.size.z*.8f;
                    Box(g,"Felt",new Vector3(0,2.6f,-.04f),new Vector3(w,2.1f,.06f),felt);
                    Part(g,"Felt trim",new Vector3(0,3.68f,-.05f),new Vector3(w+.08f,.05f,.08f),Mat("Walnut"));
                    panels++;
                }
                else
                {
                    var g=WallMount(room,"Dusk window",side,b.center.z,half);float w=Mathf.Min(b.size.z*.85f,2.8f);
                    Window(g,w,name=="The Queer Quorum - Cafe"?rainy:dusk);
                    BakedLight(g,"Window glow",new Vector3(0,2.7f,-1.1f),Hex("#C7A4BF"),.55f,4.5f);
                    windows++;
                }
            }
        }
        // The cafe's original window gets the matching rainy dusk glass.
        var cafe=Room("The Queer Quorum - Cafe");
        foreach(var w in cafe.Cast<Transform>().Where(t=>t.name=="Rainy evening window"&&t.gameObject.activeSelf))
        {
            var old=w.Find("Window glass");old.gameObject.SetActive(false);
            var q=Part(w,"Rainy dusk glass",new Vector3(0,0,-.035f),new Vector3(5.5f,2.5f,1),rainy,PrimitiveType.Quad);q.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        Debug.Log("V07 WINDOWS: "+windows+" dusk windows, "+panels+" acoustic panels, "+kept+" panels left behind existing displays.");
    }

    static void PresidentsWall()
    {
        var office=Room("President's Office");
        foreach(var panel in office.Cast<Transform>().Where(t=>t.name=="Interior wall panel"&&LocalBounds(office,t.GetComponent<Renderer>().bounds).center.x<0).ToArray())panel.gameObject.SetActive(false);
        var data=JsonUtility.FromJson<Presidents>(File.ReadAllText("Assets/SimDemocracy/Data/presidents-2026-03.json"));
        var lines=new List<string>();
        foreach(var p in data.presidencies){lines.Add(p.n+"  "+p.name+"  <color=#A89874>"+p.party+"</color>");if(p.n==56)lines.Add("57-132  <color=#A89874>to be added</color>");}
        var g=WallMount(office,"Presidents wall",-1,0,7);
        Box(g,"Honours board",new Vector3(0,2.85f,-.04f),new Vector3(8.4f,2.4f,.06f),Mat("Midnight painted timber"));
        var brass=Mat("Brushed brass");
        foreach(float y in new[]{1.64f,4.06f})Part(g,"Board rail",new Vector3(0,y,-.06f),new Vector3(8.5f,.05f,.06f),brass);
        foreach(float x in new[]{-4.23f,4.23f})Part(g,"Board rail",new Vector3(x,2.85f,-.06f),new Vector3(.05f,2.47f,.06f),brass);
        Label(g,"Board title","PRESIDENTS OF SIMDEMOCRACY",new Vector3(0,3.82f,-.08f),new Vector2(8,.3f),2.1f,Hex("#E8C97E"));
        int perColumn=(lines.Count+3)/4;
        for(int c=0;c<4;c++)
        {
            var column=Label(g,"Presidents column "+(c+1),string.Join("\n",lines.Skip(c*perColumn).Take(perColumn)),new Vector3(-3.15f+c*2.1f,2.68f,-.08f),new Vector2(1.95f,1.86f),.78f,Hex("#EDE3CC"),TextAlignmentOptions.TopLeft);
            column.richText=true;column.enableWordWrapping=false;column.enableAutoSizing=true;column.fontSizeMin=.45f;column.fontSizeMax=.78f;
        }
        BakedLight(office,"Board wash",new Vector3(-5.6f,3.8f,0),Hex("#FFE2B8"),1.1f,5);
    }

    static void Drawers()
    {
        var office=Room("President's Office");var bases=office.Cast<Transform>().Where(t=>t.name=="Desk base").ToArray();var deskBase=bases.OrderBy(t=>t.localPosition.sqrMagnitude).First();
        var b=LocalBounds(office,deskBase.GetComponent<Renderer>().bounds);float face=b.max.z;
        var walnut=Mat("Walnut");var brass=Mat("Brushed brass");var paper=Mat("Glazed porcelain");
        var duckBody=GameObject.Find("Duck body").GetComponent<Renderer>().sharedMaterial;
        var beak=AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v04/Duck beak.mat");var eyes=AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v04/Duck eyes.mat");
        // Pedestal drawers either side of the president's knees; trays pull out toward the chair.
        var specs=new[]{Tuple.Create(-1.45f,"MORE AMENDMENTS"),Tuple.Create(-.6f,"BACKUP CLERK"),Tuple.Create(1.45f,"THE LAST BISCUIT")};
        const float y=.6f;
        for(int i=0;i<specs.Length;i++)
        {
            float x=specs[i].Item1;
            var front=Part(office,"Desk drawer",new Vector3(x,y,face+.025f),new Vector3(.78f,.24f,.05f),walnut,PrimitiveType.Cube,true);
            Part(office,"Drawer pull",new Vector3(x,y,face+.065f),new Vector3(.22f,.04f,.03f),brass);
            var contents=Group(office,"Drawer contents - "+specs[i].Item2,new Vector3(x,y,face+.21f));
            Part(contents,"Open drawer",Vector3.zero,new Vector3(.74f,.18f,.38f),walnut,PrimitiveType.Cube,false,false);
            if(i==0)for(int k=0;k<4;k++){var sheet=Part(contents,"Amendment",new Vector3(0,.1f+k*.018f,.04f),new Vector3(.42f,.014f,.3f),paper,PrimitiveType.Cube,false,false);sheet.transform.localRotation=Quaternion.Euler(0,k*7-10,0);}
            if(i==1)
            {
                var duck=Group(contents,"Backup clerk",new Vector3(0,.09f,.05f),180);
                Part(duck,"Duck body",new Vector3(0,.08f,0),new Vector3(.17f,.13f,.22f),duckBody,PrimitiveType.Sphere,false,false);
                Part(duck,"Duck head",new Vector3(0,.19f,-.07f),new Vector3(.11f,.11f,.11f),duckBody,PrimitiveType.Sphere,false,false);
                Part(duck,"Duck beak",new Vector3(0,.18f,-.135f),new Vector3(.06f,.025f,.06f),beak,PrimitiveType.Sphere,false,false);
                foreach(float s in new[]{-1f,1f})Part(duck,"Duck eye",new Vector3(s*.038f,.21f,-.11f),new Vector3(.022f,.022f,.022f),eyes,PrimitiveType.Sphere,false,false);
            }
            if(i==2)Part(contents,"Biscuit",new Vector3(.06f,.1f,.06f),new Vector3(.15f,.012f,.15f),biscuit,PrimitiveType.Cylinder,false,false);
            // Labels float just past the desktop edge at a common height, facing the chair.
            var label=Label(contents,"Drawer label",specs[i].Item2,new Vector3(0,.52f,.14f),new Vector2(.9f,.14f),.55f,Hex("#F4E9D2"));
            label.transform.localRotation=Quaternion.Euler(0,180,0);
            contents.gameObject.SetActive(false);
            var action=front.AddUdonSharpComponent<SDAction>();action.action=5;action.reveal=contents.gameObject;InteractText(action,"Open drawer");
        }
    }

    static void WitnessStand()
    {
        var court=Room("Courtroom");var g=Group(court,"Witness stand",new Vector3(5.3f,0,3.6f),-149);
        var walnut=Mat("Walnut");var navy=Mat("Midnight painted timber");
        Box(g,"Witness platform",new Vector3(0,.1f,0),new Vector3(1.7f,.2f,1.7f),walnut,true);
        Part(g,"Witness front",new Vector3(0,.72f,.8f),new Vector3(1.7f,1.04f,.08f),navy,PrimitiveType.Cube,true);
        foreach(float s in new[]{-1f,1f})Part(g,"Witness side",new Vector3(s*.81f,.72f,.25f),new Vector3(.08f,1.04f,1.1f),navy,PrimitiveType.Cube,true);
        Part(g,"Witness rail",new Vector3(0,1.27f,.8f),new Vector3(1.78f,.06f,.14f),Mat("Brushed brass"));
        var chair=UnityEngine.Object.Instantiate(court.Cast<Transform>().First(t=>t.name=="Chair").gameObject,g).transform;
        chair.name="Witness chair";chair.localPosition=new Vector3(0,.2f,-.2f);chair.localRotation=Quaternion.identity;
        var sign=Label(g,"Witness sign","WITNESS",new Vector3(0,1.05f,.85f),new Vector2(1.4f,.2f),1.2f,Hex("#E8C97E"));sign.transform.localRotation=Quaternion.Euler(0,180,0);
    }

    static void Fountain()
    {
        var g=Group(root,"Fountain",new Vector3(0,0,9.5f));const float R=3f;
        var stone=Mat("Limestone paving");var brass=Mat("Brushed brass");var navy=Mat("Midnight painted timber");
        for(int i=0;i<16;i++)
        {
            float a=i*22.5f,rad=a*Mathf.Deg2Rad;var p=new Vector3(R*Mathf.Sin(rad),0,R*Mathf.Cos(rad));
            var seg=Box(g,"Fountain rim",p+Vector3.up*.25f,new Vector3(1.25f,.5f,.4f),stone,true);seg.transform.localRotation=Quaternion.Euler(0,a,0);
            var cap=Part(g,"Fountain cap",p+Vector3.up*.525f,new Vector3(1.27f,.05f,.46f),brass);cap.transform.localRotation=Quaternion.Euler(0,a,0);
        }
        Part(g,"Fountain water",new Vector3(0,.38f,0),new Vector3(5.7f,.02f,5.7f),water,PrimitiveType.Cylinder);
        Box(g,"Fountain pedestal",new Vector3(0,.45f,0),new Vector3(1.1f,.9f,1.1f),navy,true);
        Part(g,"Fountain column",new Vector3(0,1.15f,0),new Vector3(.38f,.3f,.38f),stone,PrimitiveType.Cylinder);
        Part(g,"Fountain bowl",new Vector3(0,1.47f,0),new Vector3(1.9f,.08f,1.9f),brass,PrimitiveType.Cylinder);
        Part(g,"Fountain bowl water",new Vector3(0,1.53f,0),new Vector3(1.7f,.02f,1.7f),water,PrimitiveType.Cylinder);
        // A duck presides. No plaque.
        var duckBody=GameObject.Find("Duck body").GetComponent<Renderer>().sharedMaterial;
        var beak=AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v04/Duck beak.mat");var eyes=AssetDatabase.LoadAssetAtPath<Material>("Assets/SimDemocracy/Art-v04/Duck eyes.mat");
        var duck=Group(g,"Fountain duck",new Vector3(0,1.55f,0),180);
        Part(duck,"Duck body",new Vector3(0,.12f,0),new Vector3(.36f,.26f,.46f),duckBody,PrimitiveType.Sphere);
        Part(duck,"Duck head",new Vector3(0,.36f,-.14f),new Vector3(.24f,.24f,.24f),duckBody,PrimitiveType.Sphere);
        Part(duck,"Duck beak",new Vector3(0,.34f,-.28f),new Vector3(.12f,.05f,.12f),beak,PrimitiveType.Sphere);
        foreach(float s in new[]{-1f,1f})Part(duck,"Duck eye",new Vector3(s*.08f,.41f,-.23f),new Vector3(.04f,.04f,.04f),eyes,PrimitiveType.Sphere);
        for(int i=0;i<8;i++)
        {
            float a=22.5f+i*45f,rad=a*Mathf.Deg2Rad;var outward=new Vector3(Mathf.Sin(rad),0,Mathf.Cos(rad));
            Seat(g,outward*R+Vector3.up*.6f,Quaternion.Euler(0,a,0),outward*(R+1.05f)+Vector3.up*.15f);
        }
        BakedLight(g,"Fountain glow",new Vector3(0,2.6f,0),Hex("#FFD7A0"),1.4f,7);
    }

    static void Timeline()
    {
        var g=Group(root,"Community timeline",new Vector3(-6.2f,0,9.6f),-90);
        var walnut=Mat("Walnut");
        foreach(float s in new[]{-1f,1f})Part(g,"Noticeboard leg",new Vector3(s*1.25f,.85f,.04f),new Vector3(.1f,1.7f,.1f),walnut,PrimitiveType.Cube,true);
        Box(g,"Noticeboard",new Vector3(0,1.85f,0),new Vector3(2.8f,1.9f,.08f),Mat("Midnight painted timber"),true);
        Part(g,"Noticeboard cap",new Vector3(0,2.83f,0),new Vector3(2.95f,.08f,.16f),walnut);
        Label(g,"Timeline title","TIMELINE",new Vector3(0,2.6f,-.05f),new Vector2(2.6f,.3f),2f,Hex("#E8C97E"));
        Label(g,"Timeline entries",
            "2019   Founded\n2019   Painted Wolf Day\n2020   The SimDemocracy Post goes to press\n2025   The Danyowave\n2025   FBN, issue one\n2025   Ducks Constitution repealed\n2026   Touching grass ruled optional\nnow    You are here",
            new Vector3(0,1.7f,-.05f),new Vector2(2.5f,1.45f),.95f,Hex("#EDE3CC"),TextAlignmentOptions.TopLeft);
    }

    static void Mugs()
    {
        var cafe=Room("The Queer Quorum - Cafe");var ceramic=Mat("Glazed porcelain");int count=0;
        foreach(var cup in cafe.Cast<Transform>().Where(t=>t.name=="Coffee cup").ToArray())
        {
            var surface=cafe.Cast<Transform>().Where(t=>t.name=="Coffee surface").OrderBy(t=>Vector3.Distance(t.localPosition,cup.localPosition)).First();
            var mug=new GameObject("Coffee mug pickup");mug.transform.SetParent(cafe,false);mug.transform.localPosition=cup.localPosition;
            cup.SetParent(mug.transform,true);surface.SetParent(mug.transform,true);
            Part(mug.transform,"Mug handle",new Vector3(.14f,0,0),new Vector3(.05f,.14f,.035f),ceramic,PrimitiveType.Cube,false,false);
            foreach(var t in mug.GetComponentsInChildren<Transform>()){GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);var r=t.GetComponent<MeshRenderer>();if(r!=null){r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;}}
            var box=mug.AddComponent<BoxCollider>();box.size=new Vector3(.3f,.25f,.24f);box.center=new Vector3(.03f,0,0);
            var body=mug.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=.3f;
            var pickup=mug.AddComponent<VRCPickup>();pickup.AutoHold=VRC.SDKBase.VRC_Pickup.AutoHoldMode.Yes;pickup.InteractionText="Coffee";
            mug.AddComponent<VRCObjectSync>();count++;
        }
        if(count!=4)throw new Exception("Expected four cafe cups, found "+count);
    }

    static void Mirror()
    {
        var cafe=Room("The Queer Quorum - Cafe");
        foreach(var panel in cafe.Cast<Transform>().Where(t=>t.name=="Interior wall panel"))
        {var b=LocalBounds(cafe,panel.GetComponent<Renderer>().bounds);if(b.center.x>0&&b.center.z<-2)panel.gameObject.SetActive(false);}
        var g=WallMount(cafe,"Optional mirror",1,-4.5f,10);
        var walnut=Mat("Walnut");
        // Stands proud of the wainscot and dado rail (which reach .145 m off the wall).
        Box(g,"Mirror backing",new Vector3(0,1.65f,-.17f),new Vector3(1.75f,2.75f,.04f),Mat("Midnight painted timber"));
        foreach(float s in new[]{-1f,1f})Part(g,"Mirror frame",new Vector3(s*.89f,1.65f,-.21f),new Vector3(.08f,2.8f,.08f),walnut);
        foreach(float y in new[]{.27f,3.03f})Part(g,"Mirror frame",new Vector3(0,y,-.21f),new Vector3(1.86f,.08f,.08f),walnut);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Samples+"VRCMirror.prefab");
        var mirror=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(mirror,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        mirror.name="Cafe mirror";mirror.transform.SetParent(g,false);mirror.transform.localPosition=new Vector3(0,1.65f,-.195f);mirror.transform.localRotation=Quaternion.identity;mirror.transform.localScale=new Vector3(1.7f,2.7f,1);
        foreach(var c in mirror.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var reflection=mirror.GetComponent<VRCMirrorReflection>();var so=new SerializedObject(reflection);
        so.FindProperty("m_ReflectLayers").intValue=(1<<9)|(1<<18);so.ApplyModifiedPropertiesWithoutUndo();
        mirror.SetActive(false);
        var toggle=Part(g,"Mirror switch",new Vector3(1.25f,1.25f,-.2f),new Vector3(.3f,.18f,.06f),Mat("Brushed brass"),PrimitiveType.Cube,true);
        Label(g,"Mirror switch label","MIRROR",new Vector3(1.25f,1.45f,-.235f),new Vector2(.6f,.14f),.7f,Hex("#F4E9D2"));
        var action=toggle.AddUdonSharpComponent<SDAction>();action.action=5;action.reveal=mirror;InteractText(action,"Toggle mirror");
    }

    static SDCinema Cinema()
    {
        var room=Room("Community Cinema");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Samples+"VideoPlayers/UdonSyncPlayer (Unity).prefab");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        go.name="Cinema video player";go.transform.SetParent(room,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
        foreach(var ub in go.GetComponents<UdonBehaviour>())UnityEngine.Object.DestroyImmediate(ub);
        var player=go.GetComponent<VRC.SDK3.Video.Components.VRCUnityVideoPlayer>();
        var field=go.GetComponentInChildren<VRCUrlInputField>(true);var canvas=go.GetComponentInChildren<Canvas>(true);var audio=go.GetComponentInChildren<AudioSource>(true);
        var screen=go.transform.Find("Screen");
        if(player==null||field==null||canvas==null||audio==null||screen==null)throw new Exception("Sample video player layout changed.");

        var masking=room.Find("Video Screen - replace with video prefab output");masking.name="Screen masking";
        screen.localPosition=new Vector3(0,2.95f,7.58f);screen.localRotation=Quaternion.identity;screen.localScale=new Vector3(6.6f,3.7125f,1);
        var idle=new Texture2D(4,4);var dark=Enumerable.Repeat(Hex("#11161E"),16).ToArray();idle.SetPixels(dark);idle.Apply();
        File.WriteAllBytes(Art+"/Screen-idle.png",idle.EncodeToPNG());UnityEngine.Object.DestroyImmediate(idle);AssetDatabase.ImportAsset(Art+"/Screen-idle.png");
        var screenRenderer=screen.GetComponent<MeshRenderer>();screenRenderer.sharedMaterial=Unlit("Cinema screen",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Screen-idle.png"));
        screenRenderer.shadowCastingMode=ShadowCastingMode.Off;screenRenderer.receiveShadows=false;screenRenderer.lightProbeUsage=LightProbeUsage.Off;

        audio.transform.localPosition=new Vector3(0,3,7.2f);audio.playOnAwake=false;audio.volume=.6f;audio.spatialBlend=1;
        var spatial=audio.GetComponent<VRCSpatialAudioSource>();if(spatial==null)spatial=audio.gameObject.AddComponent<VRCSpatialAudioSource>();
        var sa=new SerializedObject(spatial);sa.FindProperty("Gain").floatValue=6;sa.FindProperty("Near").floatValue=10;sa.FindProperty("Far").floatValue=26;
        sa.FindProperty("VolumetricRadius").floatValue=6;sa.FindProperty("EnableSpatialization").boolValue=true;sa.FindProperty("UseAudioSourceVolumeCurve").boolValue=false;sa.ApplyModifiedPropertiesWithoutUndo();

        var ps=new SerializedObject(player);
        ps.FindProperty("autoPlay").boolValue=false;ps.FindProperty("loop").boolValue=false;ps.FindProperty("renderMode").intValue=1;
        ps.FindProperty("targetMaterialRenderer").objectReferenceValue=screenRenderer;ps.FindProperty("targetMaterialProperty").stringValue="_MainTex";
        ps.FindProperty("maximumResolution").intValue=1080;
        var targets=ps.FindProperty("targetAudioSources");targets.arraySize=1;targets.GetArrayElementAtIndex(0).objectReferenceValue=audio;
        var resync=ps.FindProperty("EnableAutomaticResync");if(resync!=null)resync.boolValue=false;
        ps.ApplyModifiedPropertiesWithoutUndo();

        // Booth behind the right-hand seating block, facing the screen.
        var booth=Group(room,"Cinema controls",new Vector3(5.2f,0,-5.4f));
        Box(booth,"Booth base",new Vector3(0,.5f,0),new Vector3(2.4f,1,.5f),Mat("Walnut"),true);
        Box(booth,"Booth board",new Vector3(0,1.6f,.21f),new Vector3(2.4f,1.2f,.08f),Mat("Midnight painted timber"),true);
        canvas.transform.SetParent(booth,false);canvas.transform.localPosition=new Vector3(0,1.98f,.16f);canvas.transform.localRotation=Quaternion.identity;canvas.transform.localScale=Vector3.one*.0045f;
        var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(480,44);
        var fieldRect=(RectTransform)field.transform;fieldRect.anchorMin=Vector2.zero;fieldRect.anchorMax=Vector2.one;fieldRect.offsetMin=Vector2.zero;fieldRect.offsetMax=Vector2.zero;fieldRect.localScale=Vector3.one;
        foreach(var t in field.GetComponentsInChildren<UnityEngine.UI.Text>(true)){t.fontSize=20;if(t.gameObject.name=="Placeholder")t.text="Paste a video link, then press Enter";}
        var status=Label(booth,"Cinema status","Paste a video link above",new Vector3(0,1.75f,.16f),new Vector2(2.2f,.24f),1.05f,Color.white);
        string[] names={"PLAY / PAUSE","STOP","RESYNC","VOL -","VOL +","HOST LOCK"};
        var sdc=go.AddUdonSharpComponent<SDCinema>();
        for(int i=0;i<names.Length;i++)
        {
            float x=-1.0f+i*.4f;
            var button=Part(booth,names[i],new Vector3(x,1.27f,.14f),new Vector3(.34f,.16f,.08f),i==5?Mat("Oatmeal upholstery"):Mat("Brushed brass"),PrimitiveType.Cube,true);
            var label=Label(booth,"Label - "+names[i],names[i],new Vector3(x,1.44f,.165f),new Vector2(.38f,.12f),.5f,Color.white);label.enableAutoSizing=true;label.fontSizeMin=.3f;label.fontSizeMax=.5f;
            var cb=button.AddUdonSharpComponent<SDCinemaButton>();cb.cinema=sdc;cb.action=i;InteractText(cb,names[i]);
        }
        sdc.player=player;sdc.urlField=field;sdc.screen=screenRenderer;sdc.speaker=audio;sdc.status=status;
        sdc.idleCard=room.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.text.StartsWith("SIMDEM CINEMA")).gameObject;
        screenRenderer.enabled=false;

        // Point the URL field's end-edit event at the new behaviour.
        var backing=UdonSharpEditorUtility.GetBackingUdonBehaviour(sdc);
        var fs=new SerializedObject(field);var calls=fs.FindProperty("m_OnEndEdit.m_PersistentCalls.m_Calls");
        if(calls.arraySize!=1)throw new Exception("Unexpected URL field events: "+calls.arraySize);
        var call=calls.GetArrayElementAtIndex(0);call.FindPropertyRelative("m_Target").objectReferenceValue=backing;
        var typeName=call.FindPropertyRelative("m_TargetAssemblyTypeName");if(typeName!=null)typeName.stringValue=typeof(UdonBehaviour).FullName+", "+typeof(UdonBehaviour).Assembly.GetName().Name;
        call.FindPropertyRelative("m_MethodName").stringValue="SendCustomEvent";call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue="OnURLChanged";
        fs.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(backing);
        return sdc;
    }

    static void Archive()
    {
        var data=JsonUtility.FromJson<Constitution>(File.ReadAllText("Assets/SimDemocracy/Data/constitution-r6870.json"));
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();
        int index=Array.IndexOf(reader.titles,"Constitution");if(index<0)throw new Exception("Constitution catalog entry not found.");
        var replace=new Dictionary<char,string>{{'⅓',"1/3"},{'⅔',"2/3"},{'¾',"3/4"},{'½',"1/2"},{'‘',"'"},{'’',"'"},{'“',"\""},{'”',"\""},{'–',"-"},{'—',"-"},{'…',"..."},{'§',"S"}};
        string Clean(string s)
        {
            if(font.HasCharacters(s,out uint[] missing,false,false))return s;
            foreach(var c in missing.Distinct().Select(u=>(char)u)){if(replace.TryGetValue(c,out var r))s=s.Replace(c.ToString(),r);else{Debug.LogWarning("V07 ARCHIVE: no glyph for U+"+((int)c).ToString("X4"));s=s.Replace(c.ToString(),"?");}}
            return s;
        }
        string footer="\n\narchive.simdemocracy.org  /  revision "+data.revision+"  /  1 Oct 2026";
        var titles=reader.titles.ToList();var docs=reader.documents.ToList();
        titles.RemoveAt(index);docs.RemoveAt(index);
        titles.InsertRange(index,data.parts.Select(p=>Clean("THE CONSTITUTION  /  "+p.title)));
        docs.InsertRange(index,data.parts.Select(p=>Clean(p.text+footer)));
        int shift=data.parts.Length-1;
        foreach(var a in UnityEngine.Object.FindObjectsOfType<SDAction>(true))if(a.action==0&&a.reader==reader&&a.entry>index){a.entry+=shift;EditorUtility.SetDirty(a);}
        reader.titles=titles.ToArray();reader.documents=docs.ToArray();EditorUtility.SetDirty(reader);
        Debug.Log("V07 ARCHIVE: "+data.parts.Length+" constitution parts; "+reader.documents.Length+" entries total.");
    }

    static void Trees()
    {
        var foliage=Mat("Foliage");var walnut=Mat("Walnut");var rng=new System.Random(2019);
        var keepOut=RoomNames.Select(n=>{var b=Room(n).Find("Floor").GetComponent<Renderer>().bounds;b.Expand(new Vector3(6,0,6));return b;}).ToList();
        var spots=new List<Vector3>();
        for(float z=-37;z<=58;z+=7){spots.Add(new Vector3(-44,0,z));spots.Add(new Vector3(44,0,z));}
        for(float x=-37;x<=37;x+=7){spots.Add(new Vector3(x,0,-38));spots.Add(new Vector3(x,0,58));}
        var group=Group(root,"Perimeter trees",Vector3.zero);int count=0;
        foreach(var s in spots)
        {
            var p=s+new Vector3((float)rng.NextDouble()*3-1.5f,0,(float)rng.NextDouble()*3-1.5f);
            if(keepOut.Any(b=>b.Contains(new Vector3(p.x,b.center.y,p.z))))continue;
            float k=.85f+(float)rng.NextDouble()*.5f;var t=Group(group,"Tree",p,(float)rng.NextDouble()*90);
            Part(t,"Trunk",new Vector3(0,.9f*k,0),new Vector3(.35f,1.8f,.35f)*k,walnut,PrimitiveType.Cube,true);
            Part(t,"Canopy",new Vector3(0,2.4f*k,0),new Vector3(2.3f,1.6f,2.3f)*k,foliage);
            var top=Part(t,"Canopy top",new Vector3(0,3.4f*k,0),new Vector3(1.5f,1.2f,1.5f)*k,foliage);top.transform.localRotation=Quaternion.Euler(0,45,0);
            count++;
        }
        Debug.Log("V07 TREES: "+count);
    }

    static void OutdoorProbes()
    {
        var positions=new List<Vector3>();
        for(float x=-12;x<=12;x+=4)for(float z=-14;z<=16;z+=5)foreach(float y in new[]{.6f,2.6f})positions.Add(new Vector3(x,y,z));
        for(int i=0;i<12;i++){float a=i*30*Mathf.Deg2Rad;foreach(float y in new[]{.7f,1.8f})positions.Add(new Vector3(Mathf.Sin(a)*4.2f,y,9.5f+Mathf.Cos(a)*4.2f));}
        var g=new GameObject("Outdoor light probes");g.AddComponent<LightProbeGroup>().probePositions=positions.ToArray();
    }

    [MenuItem("SimDemocracy/Create v0.7 Scene")]
    public static void Build()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        BuildScene();BakeAndReview();
    }

    // Deletes only what this script generates (the v07 scene, its baked lighting and Art-v07), then rebuilds unbaked.
    public static void Rebuild()
    {
        foreach(var path in new[]{Scene,Scene.Replace(".unity",""),Art})if(AssetDatabase.IsValidFolder(path)||File.Exists(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.Refresh();BuildScene();
    }

    // Builds v07 from v06 and checks it, without baking. In batch mode, bake in a fresh
    // editor process (BakeAndReview) so the lightmapper has the most memory available.
    public static void BuildScene()
    {
        if(File.Exists(Scene))throw new Exception("v07 already exists; refusing to overwrite.");
        EnsurePrograms();
        EditorSceneManager.OpenScene(Source);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();meshNumber=0;
        root=GameObject.Find("SimDemocracy - Capital v0.2").transform;
        font=UnityEngine.Object.FindObjectsOfType<TMP_Text>(true).First(t=>t.font!=null).font;
        textMaterial=UnityEngine.Object.FindObjectsOfType<TMP_Text>(true).Select(t=>t.fontSharedMaterial).FirstOrDefault(m=>m!=null&&m.name.StartsWith("Text-front"));
        Materials();Dusk();
        PresidentsWall();Mirror();WindowsAndPanels();
        Drawers();WitnessStand();Fountain();Timeline();Mugs();Cinema();Archive();Trees();OutdoorProbes();
        var lighting=UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);AssetDatabase.CreateAsset(lighting,Art+"/LightingSettings.asset");Lightmapping.lightingSettings=lighting;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions{IsEditorBuild=false});if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("Udon failed.");
        Check();LowerBakeMemory();
        Debug.Log("V07 SCENE BUILT (not yet baked): "+Scene);
    }

    // Smaller lightmap pages and a coarser open ground keep the CPU lightmapper's working set down.
    static void LowerBakeMemory()
    {
        var settings=Lightmapping.lightingSettings;settings.lightmapMaxSize=512;EditorUtility.SetDirty(settings);
        var ground=GameObject.Find("SimDemocracy - Capital v0.2").transform.Find("Ground").GetComponent<MeshRenderer>();
        ground.scaleInLightmap=.3f;EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }

    // Bake and review an already-built v07 scene (used when a bake needs a fresh editor process).
    public static void BakeAndReview()
    {
        EditorSceneManager.OpenScene(Scene);Check();
        LowerBakeMemory();
        if(!Lightmapping.Bake())throw new Exception("v07 bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("V07 BAKE COMPLETE: "+LightmapSettings.lightmaps.Length+" lightmaps.");
        Review();
    }

    // Recompiles Udon and re-runs every v07 check without changing the scene.
    public static void Verify()
    {
        EditorSceneManager.OpenScene(Scene);
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions{IsEditorBuild=false});if(UdonSharpProgramAsset.AnyUdonSharpScriptHasError())throw new Exception("Udon failed.");
        Check();Debug.Log("V07 VERIFY PASSED");
    }

    public static void Check()
    {
        SDValidateScene.Check();SDWalkthroughFix.CheckRoutes();
        var cinema=UnityEngine.Object.FindObjectsOfType<SDCinema>(true);if(cinema.Length!=1)throw new Exception("Expected one cinema.");
        var c=cinema[0];if(c.player==null||c.urlField==null||c.screen==null||c.speaker==null||c.status==null||c.idleCard==null)throw new Exception("Cinema references incomplete.");
        if(c.player.gameObject!=c.gameObject)throw new Exception("Cinema behaviour must share the video player's GameObject to receive video events.");
        var call=new SerializedObject(c.urlField).FindProperty("m_OnEndEdit.m_PersistentCalls.m_Calls").GetArrayElementAtIndex(0);
        if(call.FindPropertyRelative("m_Target").objectReferenceValue!=UdonSharpEditorUtility.GetBackingUdonBehaviour(c))throw new Exception("URL field not wired to cinema.");
        var buttons=UnityEngine.Object.FindObjectsOfType<SDCinemaButton>(true);if(buttons.Length!=6||buttons.Any(b=>b.cinema!=c))throw new Exception("Cinema buttons incomplete.");
        if(UnityEngine.Object.FindObjectsOfType<VRCPickup>(true).Length!=4)throw new Exception("Expected four mugs.");
        if(UnityEngine.Object.FindObjectsOfType<VRCMirrorReflection>(true).Count(m=>!m.gameObject.activeSelf)!=1)throw new Exception("Mirror should exist and start off.");
        if(UnityEngine.Object.FindObjectsOfType<SDAction>(true).Count(a=>a.action==5)!=5)throw new Exception("Expected Bonk, three drawers and the mirror toggle.");
        foreach(var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing&&t.name!="Label - Archive reader")throw new Exception("Text overflow: "+t.name+" in "+t.transform.parent.name);}
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();int pages=0;
        for(int i=0;i<reader.documents.Length;i++)
        {
            reader.Select(i);int count=(reader.documents[i].Split('\n').Length+reader.linesPerPage-1)/reader.linesPerPage;
            for(int page=0;page<count;page++){reader.display.ForceMeshUpdate();if(reader.display.isTextOverflowing)throw new Exception("Archive overflow: "+reader.titles[i]+" page "+(page+1));pages++;reader.NextPage();}
        }
        reader.Select(0);
        Physics.SyncTransforms();
        foreach(var pair in new[]{Tuple.Create("Courtroom",18f),Tuple.Create("Legislative Chamber",16f),Tuple.Create("President's Office",12f),Tuple.Create("Secretary Name National Archives",20f),Tuple.Create("The Queer Quorum - Cafe",16f),Tuple.Create("Community Cinema",16f),Tuple.Create("Suspicious Side Room",10f)})
        {
            var r=Room(pair.Item1);var start=new Vector3(0,.45f,-pair.Item2/2-.8f);
            if(Physics.CapsuleCast(r.TransformPoint(start),r.TransformPoint(start+Vector3.up*1.1f),.25f,r.forward,out var hit,1.6f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Entrance blocked: "+pair.Item1+" by "+hit.collider.name);
        }
        // Both sides of the fountain, and from the fountain to the chamber door, stay walkable.
        foreach(var from in new[]{new Vector3(4.5f,.45f,5),new Vector3(-4.5f,.45f,5),new Vector3(0,.45f,13)})
            if(Physics.CapsuleCast(from,from+Vector3.up*1.1f,.25f,Vector3.forward,out var hit,from.z==13?4.5f:9,~0,QueryTriggerInteraction.Ignore))throw new Exception("Square path blocked from "+from+" by "+hit.collider.name);
        Debug.Log("V07 CHECK PASSED: cinema wiring, 6 buttons, 4 mugs, mirror off, 5 local toggles, "+pages+" archive pages fit, 7 doorways clear.");
    }

    public static void Review()
    {
        string output=Environment.GetEnvironmentVariable("SIMDEM_REVIEW_OUTPUT");if(string.IsNullOrEmpty(output))return;
        Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("SIMDEM_AUDIT_SCENE",Scene);SDWalkthroughReview.Render();
        EditorSceneManager.OpenScene(Scene);
        var g=new GameObject("Feature camera");var cam=g.AddComponent<Camera>();cam.nearClipPlane=.04f;cam.farClipPlane=300;cam.fieldOfView=66;
        void Shot(string room,Vector3 pos,Vector3 look,string name)
        {
            var t=room==null?null:Room(room);cam.transform.position=t?t.TransformPoint(pos):pos;cam.transform.LookAt(t?t.TransformPoint(look):look);
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes(Path.Combine(output,"v07-"+name+".png"),tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        }
        Shot(null,new Vector3(0,1.65f,-9),new Vector3(0,2.5f,10),"Spawn-view");
        Shot(null,new Vector3(4,1.65f,2),new Vector3(-1,1,10),"Fountain");
        Shot(null,new Vector3(-3.4f,1.6f,9.6f),new Vector3(-6.2f,1.8f,9.6f),"Timeline");
        Shot(null,new Vector3(-30,9,-30),new Vector3(0,2,8),"Dusk-overview");
        Shot("President's Office",new Vector3(-2.6f,1.65f,0),new Vector3(-7,2.7f,0),"Presidents-wall");
        foreach(var a in UnityEngine.Object.FindObjectsOfType<SDAction>(true).Where(a=>a.action==5&&a.reveal.name.StartsWith("Drawer contents")))a.reveal.SetActive(true);
        Shot("President's Office",new Vector3(-1.05f,1.35f,2.8f),new Vector3(-1.05f,.65f,1.6f),"Drawers-open-left");
        Shot("President's Office",new Vector3(1.45f,1.35f,2.8f),new Vector3(1.45f,.65f,1.6f),"Drawers-open-right");
        Shot("Courtroom",new Vector3(1.5f,1.65f,-1.5f),new Vector3(5.3f,.9f,3.6f),"Witness-stand");
        Shot("Courtroom",new Vector3(-4,1.65f,-6),new Vector3(4,2.4f,4),"Court-windows");
        Shot("The Queer Quorum - Cafe",new Vector3(3,1.65f,-2),new Vector3(10,2,-1),"Cafe-windows");
        UnityEngine.Object.FindObjectsOfType<VRCMirrorReflection>(true).First().gameObject.SetActive(true);
        Shot("The Queer Quorum - Cafe",new Vector3(6.5f,1.65f,-4.5f),new Vector3(10,1.6f,-4.5f),"Mirror-on");
        Shot("The Queer Quorum - Cafe",new Vector3(5,1.4f,1.4f),new Vector3(5,.9f,.5f),"Mug");
        Shot("Community Cinema",new Vector3(5.2f,1.65f,-6.5f),new Vector3(5.2f,1.6f,-5.2f),"Cinema-booth");
        var c=UnityEngine.Object.FindObjectOfType<SDCinema>();c.idleCard.SetActive(false);c.screen.enabled=true;
        var test=new Material(Shader.Find("Unlit/Texture"));test.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SimDemocracy/Art-v05/Bonk.png");c.screen.sharedMaterial=test;
        Shot("Community Cinema",new Vector3(0,1.3f,-1),new Vector3(0,2.9f,7.6f),"Cinema-screen-test");
        var reader=UnityEngine.Object.FindObjectOfType<SDArchiveReader>();reader.Select(Array.FindIndex(reader.titles,t=>t.Contains("Bill of Rights")));
        Shot("Secretary Name National Archives",new Vector3(0,2.2f,3.5f),new Vector3(0,2.9f,9.6f),"Archive-constitution");
        UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("V07 REVIEW RENDERS SAVED to "+output);
    }
}
