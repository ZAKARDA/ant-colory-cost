using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antchemy.Editor
{
    public static class SceneSetup
    {
        [MenuItem("Antchemy/Setup Main Scene")]
        public static void BuildMainScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camObj = new GameObject("Main Camera"); camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 10;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("F7D49B");
            camObj.transform.position = new Vector3(0,3,-20);camObj.transform.rotation=Quaternion.Euler(8.53f,0,0);camObj.AddComponent<AudioListener>();camObj.AddComponent<PortraitCamera>();
            var lightObj = new GameObject("Soft morning light");var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;light.color = new Color(1,.97f,.90f);light.intensity=.65f;lightObj.transform.rotation=Quaternion.Euler(28,-25,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.38f,.38f);
            // A quiet, recessed play surface and a separate colony tray.
            Plate("Board shadow",new Vector3(.05f,3.4f,1),new Vector3(8.04f,7.15f,.2f),"CCA66F");
            Plate("Ivory board",new Vector3(0,3.5f,.8f),new Vector3(8.04f,7.15f,.2f),"FFF5DF");
            Plate("Board inset",new Vector3(0,3.5f,.65f),new Vector3(7.62f,6.73f,.1f),"F0EBD9");
            PaperBackground();
            Nest();
            new GameObject("AudioManager").AddComponent<AudioManager>();new GameObject("GameManager").AddComponent<GameManager>();
            var gridObj = new GameObject("PictureGrid");gridObj.transform.position=new Vector3(0,3.5f,0);gridObj.AddComponent<PictureGrid>();
            var slots = new GameObject("SlotsContainer");var lanes=new GameObject("LanesContainer");
            var manager = new GameObject("LevelManager").AddComponent<LevelManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("_boardContainer").objectReferenceValue=gridObj.transform;
            so.FindProperty("_slotsContainer").objectReferenceValue=slots.transform;
            so.FindProperty("_lanesContainer").objectReferenceValue=lanes.transform;so.ApplyModifiedProperties();
            new GameObject("UIManager").AddComponent<UIManager>();
            Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/MainGame.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MainGame.unity",true)};
            PlayerSettings.companyName="Ant Colony Colors";PlayerSettings.productName="Ant Colony Colors";
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.defaultWebScreenWidth=540;PlayerSettings.defaultWebScreenHeight=960;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.antcolonycolors.game");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"com.antcolonycolors.game");
            PlayerSettings.bundleVersion="1.0.0";PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.Disable;
            var launchImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/LaunchLabPortrait.png");
            if(launchImporter==null)throw new Exception("The color-lab launch art is missing.");
            launchImporter.textureType=TextureImporterType.Sprite;
            launchImporter.mipmapEnabled=false;
            launchImporter.SaveAndReimport();
            var launchSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LaunchLabPortrait.png");
            PlayerSettings.SplashScreen.show=true;
            PlayerSettings.SplashScreen.showUnityLogo=false;
            PlayerSettings.SplashScreen.background=launchSprite;
            PlayerSettings.SplashScreen.backgroundPortrait=launchSprite;
            PlayerSettings.SplashScreen.blurBackgroundImage=false;
            PlayerSettings.SplashScreen.overlayOpacity=0;
            foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/UI"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(path));
                bool paper=importer.assetPath.EndsWith("HomePaper.png");
                importer.textureType=paper?TextureImporterType.Default:TextureImporterType.Sprite;
                if(paper) importer.wrapMode=TextureWrapMode.Repeat;
                if(importer.assetPath.EndsWith("HomeAntWelcome.png") || importer.assetPath.EndsWith("LabAntScientist.png") || importer.assetPath.EndsWith("LabAntMixing.png")) importer.isReadable=true;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("COLONY_SCENE_READY");
        }
        static void Plate(string name,Vector3 position,Vector3 scale,string hex)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.position=position;
            var mesh=RoundedMesh(scale.x,scale.y,.23f);Directory.CreateDirectory("Assets/Materials");
            string meshPath="Assets/Materials/"+name.Replace(" ","")+".asset";
            var previousMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(previousMesh!=null){EditorUtility.CopySerialized(mesh,previousMesh);mesh=previousMesh;}else AssetDatabase.CreateAsset(mesh,meshPath);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Shader.Find("Unlit/Color"));material.color=Hex(hex);material.SetFloat("_Glossiness",.12f);
            go.GetComponent<Renderer>().sharedMaterial=material;
            Directory.CreateDirectory("Assets/Materials");string path="Assets/Materials/"+name.Replace(" ","")+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old!=null){EditorUtility.CopySerialized(material,old);go.GetComponent<Renderer>().sharedMaterial=old;}else AssetDatabase.CreateAsset(material,path);
        }
        static Mesh RoundedMesh(float width,float height,float radius)
        {
            radius=Mathf.Min(radius,width*.5f,height*.5f);
            var vertices=new System.Collections.Generic.List<Vector3>{Vector3.zero};
            for(int corner=0;corner<4;corner++)
            {
                float cx=(corner==0||corner==3?1:-1)*(width*.5f-radius),cy=(corner<2?1:-1)*(height*.5f-radius);
                for(int step=0;step<=8;step++){float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;vertices.Add(new Vector3(cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius,0));}
            }
            var triangles=new System.Collections.Generic.List<int>();for(int i=1;i<vertices.Count;i++){triangles.Add(0);triangles.Add(i==vertices.Count-1?1:i+1);triangles.Add(i);}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void PaperBackground()
        {
            const int s=256;var texture=new Texture2D(s,s,TextureFormat.RGBA32,false);var pixels=new Color[s*s];
            for(int y=0;y<s;y++)for(int x=0;x<s;x++){float d=Mathf.Abs(x-s/2)+Mathf.Abs(y-s/2);pixels[y*s+x]=d<s/2?Hex("F8D9A9"):Hex("F4CF95");}
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;
            System.IO.File.WriteAllBytes("Assets/Materials/Paper.png",texture.EncodeToPNG());AssetDatabase.ImportAsset("Assets/Materials/Paper.png");
            var material=new Material(Shader.Find("Unlit/Texture"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Paper.png");material.mainTextureScale=new Vector2(12,20);
            string path="Assets/Materials/Paper.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old!=null){EditorUtility.CopySerialized(material,old);material=old;}else AssetDatabase.CreateAsset(material,path);
            var paper=GameObject.CreatePrimitive(PrimitiveType.Quad);paper.name="Diamond paper";paper.transform.position=new Vector3(0,0,2);paper.transform.localScale=new Vector3(24,40,1);paper.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(paper.GetComponent<Collider>());
        }
        static void Nest()
        {
            Plate("Nest rim",new Vector3(0,-.65f,.3f),new Vector3(1.4f,.65f,.1f),"B5834E");
            Plate("Nest opening",new Vector3(-.08f,-.63f,.15f),new Vector3(1.15f,.42f,.1f),"5C381F");
            Plate("Nest lid",new Vector3(.95f,-.7f,.2f),new Vector3(.9f,.57f,.1f),"E3AD74");
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        [MenuItem("Antchemy/Build macOS")]
        public static void BuildMac()
        {
            BuildMainScene();Directory.CreateDirectory("../Builds");
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/Antchemy.app",BuildTarget.StandaloneOSX,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("macOS build failed: "+report.summary.result);
            Debug.Log("COLONY_MAC_BUILD_OK");
        }
        [MenuItem("Antchemy/Build Android APK")]
        public static void BuildAndroid()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new Exception("Install Android Build Support in Unity Hub first.");
            BuildMainScene();Directory.CreateDirectory("../Builds");PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/Antchemy.apk",BuildTarget.Android,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
        }
        static bool testAdsSimulatorBuild;
        [MenuItem("Antchemy/Build iOS Test Ads Simulator")]
        public static void BuildIOSTestAdsSimulator()
        {
            testAdsSimulatorBuild=true;
            try { BuildIOS(); }
            finally { testAdsSimulatorBuild=false; }
        }
        [MenuItem("Antchemy/Build iOS Xcode project")]
        public static void BuildIOS()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))throw new Exception("Install iOS Build Support in Unity Hub first.");
            BuildMainScene(); Directory.CreateDirectory("../Builds");
            PlayerSettings.iOS.sdkVersion=testAdsSimulatorBuild || Array.IndexOf(Environment.GetCommandLineArgs(),"-colony-ios-simulator")>=0?iOSSdkVersion.SimulatorSDK:iOSSdkVersion.DeviceSDK;
            var options=testAdsSimulatorBuild || Array.IndexOf(Environment.GetCommandLineArgs(),"-colony-ad-diagnostics")>=0?BuildOptions.Development:BuildOptions.None;
            if(options==BuildOptions.Development && !AdConfiguration.Load().useTestAds)
                throw new Exception("The ad diagnostics build requires useTestAds=true; live ad units cannot be tested from this menu.");
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"../Builds/AntColonyColors-iOS",BuildTarget.iOS,options);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("iOS export failed: "+report.summary.result);
            AdMobIOSPlist.Configure(BuildTarget.iOS,"../Builds/AntColonyColors-iOS");
            Debug.Log("COLONY_IOS_EXPORT_OK");
        }
    }
}
