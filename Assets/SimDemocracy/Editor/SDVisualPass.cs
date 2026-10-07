using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only art pass. Procedural assets are authored locally; no asset-store dependencies.
public static class SDVisualPass
{
    const string Art="Assets/SimDemocracy/Art-v03";
    static Material oak, walnut, plaster, navy, brass, linen, velvet, paving, grass, ceramic, glass, glow, leaves;
    static int meshIndex;
    static string scenePath;
    static Material Surface(string name,Color color,Texture2D texture=null,float smooth=.15f)
    {
        string path=Art+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("VRChat/Mobile/Standard Lite")); AssetDatabase.CreateAsset(m,path); }
        m.color=color;m.mainTexture=texture;m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",name=="Brushed brass"?.45f:0f);
        EditorUtility.SetDirty(m);
        return m;
    }
    static Color Hex(string value) { Color color;ColorUtility.TryParseHtmlString(value,out color);return color; }
    static Texture2D Texture(string name,int kind)
    {
        const int n=512;
        var texture=new Texture2D(n,n,TextureFormat.RGB24,true);
        var pixels=new Color[n*n];
        for(int y=0;y<n;y++) for(int x=0;x<n;x++)
        {
            float u=(float)x/n,v=(float)y/n;
            float noise=Mathf.PerlinNoise(u*42+7,v*42+13);
            float c=.88f+noise*.12f;
            if(kind==0) // Oak boards, fine longitudinal grain and staggered end seams.
            {
                int board=x/64;int end=(y+(board%2)*170)%256;
                float grain=Mathf.Sin(u*610+Mathf.Sin(v*15)*2+Mathf.PerlinNoise(u*7,v*25)*7);
                c=.73f+grain*.07f+noise*.1f+(board%3)*.035f;
                if(x%64<2||end<2)c*=.58f;
            }
            else if(kind==1) // Woven fabric.
                c=.8f+noise*.12f+((x%4<2)? .04f:-.04f)+((y%4<2)?.04f:-.04f);
            else if(kind==2) // Ashlar paving with staggered joints.
            {
                int row=y/128;int col=(x+(row%2)*128)%256;
                c=.84f+noise*.13f;
                if(col<5||y%128<5)c=.48f;
            }
            else if(kind==3) c=.82f+noise*.15f+Mathf.PerlinNoise(u*150,v*150)*.035f;
            else if(kind==4) c=.7f+Mathf.PerlinNoise(u*60,v*4)*.22f;
            pixels[y*n+x]=new Color(c,c,c);
        }
        texture.SetPixels(pixels);texture.Apply();
        string path=Art+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;
        importer.maxTextureSize=512;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Materials()
    {
        var wood=Texture("Oak-board-grain",0);var weave=Texture("Linen-weave",1);var stone=Texture("Limestone-joints",2);var stucco=Texture("Plaster-grain",3);var rain=Texture("Rain-glass",4);
        oak=Surface("Honey oak",Hex("#C49B6E"),wood);
        walnut=Surface("Walnut",Hex("#715444"),wood);
        plaster=Surface("Warm plaster",Hex("#ECE0CB"),stucco);
        navy=Surface("Midnight painted timber",Hex("#273B4D"),stucco);
        brass=Surface("Brushed brass",Hex("#C9A365"),stucco,.35f);
        linen=Surface("Oatmeal upholstery",Hex("#D8C7AB"),weave);
        velvet=Surface("Mulberry upholstery",Hex("#905D73"),weave);
        paving=Surface("Limestone paving",Hex("#C7BCA7"),stone);
        grass=Surface("Sage ground",Hex("#667A65"),stucco);
        ceramic=Surface("Glazed porcelain",Hex("#E9DFC8"),null,.4f);
        glass=Surface("Blue evening glass",Hex("#457786"),rain,.4f);
        leaves=Surface("Foliage",Hex("#537958"),stucco);
        glow=Surface("Warm lamp diffuser",Hex("#FFE6AF"));
        // Small emissive fixture geometry; illumination comes from baked lights.
        glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",Hex("#FFD38B")*1.2f);
        glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
    }
    static GameObject Part(Transform parent,string name,Vector3 pos,Vector3 size,Material material,PrimitiveType shape=PrimitiveType.Cube,bool collision=false)
    {
        var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic);
        return g;
    }
    // World-scale UVs on box faces avoid stretching one wood plank over an entire room.
    static void TileBox(MeshFilter filter)
    {
        if(filter.sharedMesh==null) return;
        Mesh mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
        var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
        Vector3 size=filter.transform.lossyScale;
        if(mesh.vertexCount==24) for(int i=0;i<uv.Length;i++)
        {
            Vector3 p=Vector3.Scale(vertices[i],size), normal=normals[i];
            uv[i]=Mathf.Abs(normal.y)>.5f ? new Vector2(p.x,p.z) : Mathf.Abs(normal.x)>.5f ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y);
            uv[i]*=.5f;
        }
        if(mesh.vertexCount==24)
        {
            mesh.uv=uv;
            Vector3 scale=filter.transform.localScale;
            mesh.vertices=vertices.Select(v=>Vector3.Scale(v,scale)).ToArray();
            var box=filter.GetComponent<BoxCollider>();if(box!=null)box.size=Vector3.Scale(box.size,scale);
            filter.transform.localScale=Vector3.one;mesh.RecalculateBounds();
        }
        LightmapUV(mesh);
        mesh.name="Tiled detail "+meshIndex;
        AssetDatabase.CreateAsset(mesh,Art+"/Meshes/Detail-"+(meshIndex++)+".asset");filter.sharedMesh=mesh;
    }
    static void LightmapUV(Mesh mesh)
    {
        if(mesh.vertexCount!=24)
        {
            UnwrapParam unwrap;UnwrapParam.SetDefaults(out unwrap);unwrap.packMargin=.06f;
            Unwrapping.GenerateSecondaryUVSet(mesh,unwrap);return;
        }
        var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[24];Vector3 size=mesh.bounds.size;
        for(int i=0;i<24;i++)
        {
            Vector3 p=vertices[i]-mesh.bounds.min,n=normals[i];
            p=new Vector3(p.x/size.x,p.y/size.y,p.z/size.z);
            int face;float u,v;
            if(Mathf.Abs(n.y)>.5f){face=n.y>0?0:1;u=p.x;v=p.z;}
            else if(Mathf.Abs(n.x)>.5f){face=n.x>0?2:3;u=p.z;v=p.y;}
            else{face=n.z>0?4:5;u=p.x;v=p.y;}
            uv[i]=new Vector2((face%3)/3f+.04f+u*(1f/3-.08f),(face/3)*.5f+.04f+v*.42f);
        }
        mesh.uv2=uv;
    }
    static void DressExisting()
    {
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
        {
            if(renderer.GetComponent<TMPro.TMP_Text>()!=null || renderer.sharedMaterial==null)continue;
            string n=renderer.name;string old=renderer.sharedMaterial.name;
            if(n.Contains("Flag") || n.StartsWith("Pride stripe")) continue;
            Material material=plaster;
            if(old=="Wood")material=walnut;
            else if(old=="Navy"||old=="Wall inset")material=navy;
            else if(old=="Gold")material=brass;
            else if(old=="Rose")material=velvet;
            else if(old=="Leaves")material=leaves;
            else if(old=="Ink")material=navy;
            else if(old=="Rug")material=velvet;
            if(n=="Ground")material=grass;
            if(n=="Civic Square"||n.EndsWith(" Walk"))material=paving;
            if(n=="Floor")material=oak;
            if(n.Contains("wall")||n.StartsWith("Door ")||n.StartsWith("Roof -"))material=plaster;
            if(n=="Seat"||n=="Back")material=linen;
            if(n=="Warm lantern")material=glow;
            renderer.sharedMaterial=material;
            bool reveal=renderer.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Local Doge reveal");
            if(!reveal)GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic);
        }
        foreach(var room in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.Find("Floor")!=null).ToArray())
        {
            var floor=room.Find("Floor");float w=floor.localScale.x,d=floor.localScale.z;
            foreach(float side in new[]{-1f,1f})
            {
                Part(room,"Navy wainscot",new Vector3(side*(w/2-.17f),.8f,0),new Vector3(.08f,1.4f,d),navy);
                Part(room,"Brass dado rail",new Vector3(side*(w/2-.22f),1.5f,0),new Vector3(.1f,.06f,d),brass);
                Part(room,"Timber skirting",new Vector3(side*(w/2-.2f),.22f,0),new Vector3(.12f,.22f,d),walnut);
            }
            for(int i=-1;i<=1;i++)
                Part(room,"Ceiling cross beam",new Vector3(0,4.82f,i*d/3),new Vector3(w,.2f,.22f),walnut);
            Pendant(room,new Vector3(-w*.23f,4.5f,0));Pendant(room,new Vector3(w*.23f,4.5f,0));
            BakedLight(room,"Warm room fill",new Vector3(0,3.6f,0),LightType.Point,Hex("#FFE0AF"),4f,14);
            BakedLight(room,"Rear room fill",new Vector3(0,3.7f,d*.3f),LightType.Point,Hex("#FFE5BD"),3f,12);
        }
        foreach(var chair in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name=="Chair").ToArray())
        {
            var block=chair.Find("Base");if(block!=null)block.GetComponent<Renderer>().enabled=false;
            foreach(float x in new[]{-.34f,.34f})foreach(float z in new[]{-.3f,.3f})
                Part(chair,"Chair leg",new Vector3(x,.26f,z),new Vector3(.1f,.52f,.1f),walnut);
            foreach(float x in new[]{-.45f,.45f})
                Part(chair,"Back timber frame",new Vector3(x,.92f,-.38f),new Vector3(.07f,.88f,.18f),walnut);
            Part(chair,"Upholstery piping",new Vector3(0,.59f,-.02f),new Vector3(.92f,.045f,.85f),brass);
        }
    }
    static void BakedLight(Transform parent,string name,Vector3 pos,LightType type,Color color,float intensity,float range)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        var light=g.AddComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.range=range;light.lightmapBakeType=LightmapBakeType.Baked;light.shadows=LightShadows.Soft;
    }
    static void Pendant(Transform parent,Vector3 pos)
    {
        Part(parent,"Pendant stem",pos+Vector3.up*.3f,new Vector3(.05f,.7f,.05f),brass,PrimitiveType.Cylinder);
        Part(parent,"Pendant shade",pos,new Vector3(.85f,.16f,.85f),navy,PrimitiveType.Cylinder);
        Part(parent,"Pendant diffuser",pos-Vector3.up*.17f,new Vector3(.74f,.035f,.74f),glow,PrimitiveType.Cylinder);
    }
    static void Plant(Transform parent,Vector3 pos,float size)
    {
        Part(parent,"Terracotta planter",pos+Vector3.up*.3f*size,new Vector3(.65f,.3f,.65f)*size,walnut,PrimitiveType.Cylinder);
        Part(parent,"Plant stem",pos+Vector3.up*.85f*size,new Vector3(.07f,.7f,.07f)*size,walnut,PrimitiveType.Cylinder);
        for(int i=0;i<5;i++)
        {
            float angle=i*2.4f;
            var leaf=Part(parent,"Leaf cluster",pos+new Vector3(Mathf.Sin(angle)*.24f,1.05f+i*.07f,Mathf.Cos(angle)*.24f)*size,new Vector3(.6f,.16f,.32f)*size,leaves,PrimitiveType.Sphere);
            leaf.transform.localRotation=Quaternion.Euler(15,i*137,25);
        }
    }
    static void Cafe()
    {
        var r=GameObject.Find("The Queer Quorum - Cafe").transform;
        foreach(var t in r.GetComponentsInChildren<Transform>().Where(t=>t.name=="Cafe foliage"||t.name=="Cafe planter"||t.name=="Coffee mug"||t.name=="Coffee").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        Plant(r,new Vector3(-8,.1f,2),1.4f);Plant(r,new Vector3(8,.1f,2),1.4f);
        Part(r,"Stone bar top",new Vector3(0,1.44f,5),new Vector3(8.2f,.12f,1.35f),plaster);
        for(int i=0;i<20;i++)Part(r,"Counter fluting",new Vector3(-3.8f+i*.4f,.72f,4.42f),new Vector3(.12f,1.22f,.09f),brass);
        Part(r,"Menu frame",new Vector3(0,3.3f,7.8f),new Vector3(4.7f,2.6f,.18f),walnut);
        Part(r,"Menu board",new Vector3(0,3.3f,7.68f),new Vector3(4.48f,2.38f,.06f),navy);
        // Close only the unused opening behind the counter. Main entry remains unchanged.
        Part(r,"Cafe back panel",new Vector3(0,2.5f,8),new Vector3(4,5,.25f),plaster,PrimitiveType.Cube,true);
        var sign=r.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.text.StartsWith("THE QUEER QUORUM"));
        sign.transform.localPosition=new Vector3(0,4.3f,7.61f);sign.text="THE QUEER QUORUM\n\nCOFFEE / COMPANY / COMMON GROUND\n\nEspresso     Flat white     Tea\nToday's special: a questionable motion";sign.fontSize=2.1f;
        foreach(float side in new[]{-1f,1f})
        {
            var window=new GameObject("Rainy evening window").transform;window.SetParent(r,false);window.localPosition=new Vector3(side*9.8f,2.9f,-.5f);window.localRotation=Quaternion.Euler(0,side*90,0);
            Part(window,"Window glass",Vector3.zero,new Vector3(5.5f,2.5f,.06f),glass);
            foreach(float x in new[]{-2.8f,0,2.8f})Part(window,"Window upright",new Vector3(x,0,-.1f),new Vector3(.1f,2.7f,.12f),brass);
            foreach(float y in new[]{-1.3f,0,1.3f})Part(window,"Window crossbar",new Vector3(0,y,-.1f),new Vector3(5.7f,.1f,.12f),brass);
            BakedLight(r,"Cool window bounce",new Vector3(side*8.7f,3,-.5f),LightType.Point,Hex("#AECED5"),2,7);
        }
        foreach(var sofa in r.GetComponentsInChildren<Transform>().Where(t=>t.name=="Sofa").ToArray())
        {
            Vector3 p=sofa.localPosition;
            foreach(float side in new[]{-1f,1f})
            {
                Part(r,"Sofa arm",p+new Vector3(side*1.52f,.22f,0),new Vector3(.25f,.55f,1.25f),velvet);
                Part(r,"Sofa timber foot",p+new Vector3(side*1.25f,-.4f,0),new Vector3(.18f,.3f,.85f),walnut);
                var cushion=Part(r,"Loose sofa cushion",p+new Vector3(side*.85f,.48f,-.34f),new Vector3(.58f,.55f,.17f),side<0?linen:navy);
                cushion.transform.localRotation=Quaternion.Euler(8,0,side*8);
            }
            for(int i=-1;i<=1;i++)Part(r,"Seat cushion panel",p+new Vector3(i*.93f,.31f,0),new Vector3(.89f,.08f,1.03f),velvet);
            Vector3 mug=p+new Vector3(0,.36f,1.5f);
            Part(r,"Coffee cup",mug,new Vector3(.22f,.12f,.22f),ceramic,PrimitiveType.Cylinder);
            Part(r,"Coffee surface",mug+Vector3.up*.125f,new Vector3(.18f,.006f,.18f),walnut,PrimitiveType.Cylinder);
            Part(r,"Cup saucer",mug-Vector3.up*.13f,new Vector3(.35f,.02f,.35f),ceramic,PrimitiveType.Cylinder);
        }
        foreach(float x in new[]{-3f,3f})
        {
            Part(r,"Service shelf",new Vector3(x,2.5f,7.4f),new Vector3(1.5f,.1f,.7f),walnut);
            for(int i=0;i<3;i++)Part(r,"Counter cup",new Vector3(x-.45f+i*.4f,2.68f,7.3f),new Vector3(.22f,.13f,.22f),ceramic,PrimitiveType.Cylinder);
        }
        Pendant(r,new Vector3(-2.5f,3.5f,5));Pendant(r,new Vector3(2.5f,3.5f,5));
    }
    [MenuItem("SimDemocracy/Create Textured v0.3 Scene")]
    public static void Create()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string source=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/SimDemocracy/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("v02")).OrderBy(File.GetLastWriteTimeUtc).Last();
        EditorSceneManager.OpenScene(source);
        scenePath=AssetDatabase.GenerateUniqueAssetPath("Assets/SimDemocracy/Scenes/SimDemocracy-Capital-v03.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),scenePath);
        Directory.CreateDirectory(Art+"/Meshes");AssetDatabase.Refresh();
        meshIndex=Directory.GetFiles(Art+"/Meshes","*.asset").Length;
        Materials();DressExisting();Cafe();
        foreach(var filter in UnityEngine.Object.FindObjectsOfType<MeshFilter>(true))
            if(filter.GetComponent<MeshRenderer>()!=null && filter.GetComponent<TMPro.TMP_Text>()==null && filter.GetComponent<MeshRenderer>().sharedMaterial!=null && filter.GetComponent<MeshRenderer>().sharedMaterial.mainTexture!=null && !filter.name.Contains("Flag")) TileBox(filter);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Hex("#9DAAC3");RenderSettings.ambientEquatorColor=Hex("#AE997C");RenderSettings.ambientGroundColor=Hex("#494B51");RenderSettings.ambientIntensity=.8f;
        BakedLight(null,"Late afternoon sun",Vector3.zero,LightType.Directional,Hex("#FFE2B3"),1.3f,100);
        GameObject.Find("Late afternoon sun").transform.rotation=Quaternion.Euler(40,-35,0);
        var settings=new LightingSettings();settings.name="SimDem Quest baked lighting";settings.bakedGI=true;settings.realtimeGI=false;
        settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;settings.lightmapResolution=10;settings.lightmapMaxSize=1024;settings.lightmapPadding=4;settings.directionalityMode=LightmapsMode.NonDirectional;
        settings.directSampleCount=16;settings.indirectSampleCount=32;settings.environmentSampleCount=16;settings.maxBounces=2;
        AssetDatabase.CreateAsset(settings,AssetDatabase.GenerateUniqueAssetPath(Art+"/LightingSettings.asset"));Lightmapping.lightingSettings=settings;
        LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;
        ConfigureReceivers();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();SDValidateScene.Check();
        Debug.Log("SIMDEM VISUAL PASS CREATED: "+scenePath);
    }
    public static void Bake()
    {
        Create();
        if(!Lightmapping.Bake())throw new Exception("Lighting bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var renderers=UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true);
        int triangles=UnityEngine.Object.FindObjectsOfType<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Sum(f=>f.sharedMesh.triangles.Length/3);
        Debug.Log("SIMDEM BAKE COMPLETE: "+LightmapSettings.lightmaps.Length+" lightmaps; "+renderers.Length+" mesh renderers; "+triangles+" triangles.");
    }
    public static void RefineBake()
    {
        string latest=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/SimDemocracy/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("v03")).OrderBy(File.GetLastWriteTimeUtc).Last();
        EditorSceneManager.OpenScene(latest);
        meshIndex=Directory.GetFiles(Art+"/Meshes","*.asset").Length;
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Art}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            m.SetFloat("_Metallic",m.name=="Brushed brass"?.45f:0f);EditorUtility.SetDirty(m);
        }
        foreach(var filter in UnityEngine.Object.FindObjectsOfType<MeshFilter>(true))
        {
            if(filter.GetComponent<TMPro.TMP_Text>()!=null||filter.name.Contains("Flag"))continue;
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled)continue;
            Vector3 bounds=renderer.bounds.size;
            if(Mathf.Min(bounds.x,bounds.y,bounds.z)<.16f && Mathf.Max(bounds.x,bounds.y,bounds.z)<4)
            {
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
                renderer.receiveGI=ReceiveGI.LightProbes;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;
            }
            TileBox(filter);
        }
        var probeObject=new GameObject("Interior baked light probes");var probes=probeObject.AddComponent<LightProbeGroup>();
        var positions=new System.Collections.Generic.List<Vector3>();
        foreach(var room in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.Find("Floor")!=null).ToArray())
            foreach(float x in new[]{-4f,0f,4f})foreach(float z in new[]{-3f,0f,3f})foreach(float y in new[]{1f,3f})positions.Add(room.TransformPoint(new Vector3(x,y,z)));
        probes.probePositions=positions.ToArray();
        Lightmapping.lightingSettings.directionalityMode=LightmapsMode.NonDirectional;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        if(!Lightmapping.Bake())throw new Exception("Revised bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("SIMDEM REFINED BAKE COMPLETE: "+LightmapSettings.lightmaps.Length+" lightmaps.");
    }
    public static void FinalizeLighting()
    {
        string latest=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/SimDemocracy/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("v03")).OrderBy(File.GetLastWriteTimeUtc).Last();
        EditorSceneManager.OpenScene(latest);
        foreach(var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>(true))
        {
            if(f.sharedMesh==null || !AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Art))continue;
            LightmapUV(f.sharedMesh);EditorUtility.SetDirty(f.sharedMesh);
        }
        Lightmapping.lightingSettings.lightmapResolution=10;
        Lightmapping.lightingSettings.lightmapPadding=4;
        EditorUtility.SetDirty(Lightmapping.lightingSettings);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        if(!Lightmapping.Bake())throw new Exception("Final bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("SIMDEM FINAL LIGHTING: "+LightmapSettings.lightmaps.Length+" lightmaps.");
    }
    static void ConfigureReceivers()
    {
        if(GameObject.Find("Interior baked light probes")==null)
        {
            var group=new GameObject("Interior baked light probes").AddComponent<LightProbeGroup>();
            var points=new System.Collections.Generic.List<Vector3>();
            foreach(var room in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.Find("Floor")!=null).ToArray())
                foreach(float x in new[]{-4f,0f,4f})foreach(float z in new[]{-3f,0f,3f})foreach(float y in new[]{1f,3f})points.Add(room.TransformPoint(new Vector3(x,y,z)));
            group.probePositions=points.ToArray();
        }
        foreach(var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
        {
            if(r.GetComponent<TMPro.TMP_Text>()!=null || !r.enabled || r.name.Contains("Flag"))continue;
            if(r.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Local Doge reveal"))continue;
            string n=r.name;
            bool structural=n=="Ground"||n=="Floor"||n=="Civic Square"||n.EndsWith(" Walk")||n=="West wall"||n=="East wall"||n.StartsWith("Door ")||n.StartsWith("Roof -")||n=="Central rug"||n=="Coffee counter"||n=="Cafe back panel";
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic);
            // Small details still contribute shadows, but receive baked light through probes.
            r.receiveGI=structural?ReceiveGI.Lightmaps:ReceiveGI.LightProbes;
            r.lightProbeUsage=LightProbeUsage.BlendProbes;
        }
    }
    public static void FinishProbeLighting()
    {
        string latest=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/SimDemocracy/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("v03")).OrderBy(File.GetLastWriteTimeUtc).Last();
        EditorSceneManager.OpenScene(latest);ConfigureReceivers();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        if(!Lightmapping.Bake())throw new Exception("Probe bake failed.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("SIMDEM PROBE LIGHTING COMPLETE: "+LightmapSettings.lightmaps.Length+" lightmaps.");
    }
}

