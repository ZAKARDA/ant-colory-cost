using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    // A real 3D character rendered into the Home canvas; animation also runs while gameplay is paused.
    public class HomeHero : MonoBehaviour
    {
        Camera previewCamera;
        RenderTexture texture;
        Transform ant;
        readonly Transform[] cubes = new Transform[3];
        readonly Vector3[] positions = { new Vector3(-1.65f,-.55f,0), new Vector3(1.45f,-.75f,-.1f), new Vector3(1.6f,.55f,.25f) };
        public static HomeHero Create(RawImage target, Transform parent)
        {
            var root = new GameObject("Home 3D showcase"); root.transform.SetParent(parent,false);
            root.transform.position = new Vector3(1000,1000,0);
            var hero = root.AddComponent<HomeHero>();
            var cameraObject = new GameObject("Home preview camera"); cameraObject.transform.SetParent(root.transform,false);
            cameraObject.transform.localPosition = new Vector3(0,.25f,-8);
            hero.previewCamera = cameraObject.AddComponent<Camera>(); hero.previewCamera.orthographic = true;
            hero.previewCamera.orthographicSize = 2.2f; hero.previewCamera.clearFlags = CameraClearFlags.SolidColor;
            hero.previewCamera.backgroundColor = Color.clear; hero.previewCamera.cullingMask = 1 << 30;
            hero.previewCamera.allowHDR = false; hero.previewCamera.depth = -10;
            if(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                hero.texture = new RenderTexture(780,420,24,RenderTextureFormat.ARGB32) {name="Home character preview",antiAliasing=4};
                hero.texture.Create(); hero.previewCamera.targetTexture = hero.texture; target.texture = hero.texture;
            }
            else target.enabled = false;
            hero.ant = new GameObject("Golden worker").transform; hero.ant.SetParent(root.transform,false);
            GameplayVisuals.Ant(hero.ant,0,3f);
            int[] colors = {9,24,4};
            for(int i=0;i<hero.cubes.Length;i++)
            {
                var cube = GameplayVisuals.Shape(root.transform,"Colorful toy cube "+i,PrimitiveType.Cube,hero.positions[i],Vector3.one*(i==2?.74f:.95f),ColorPalette.GetColor(colors[i]));
                hero.cubes[i] = cube.transform;
            }
            foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=30;
            var main = Camera.main; if(main!=null)main.cullingMask &= ~(1 << 30);
            return hero;
        }
        public void SetVisible(bool visible) { if(previewCamera!=null)previewCamera.enabled=visible && texture != null; }
        void Update()
        {
            if(previewCamera==null||!previewCamera.enabled)return;
            float t=Time.unscaledTime;
            ant.localPosition=new Vector3(0,-.2f+Mathf.Sin(t*1.4f)*.065f,-.2f);
            ant.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*.8f)*3);
            for(int i=0;i<cubes.Length;i++)
            {
                cubes[i].localPosition=positions[i]+Vector3.up*(Mathf.Sin(t*1.2f+i*1.6f)*.075f);
                cubes[i].localRotation=Quaternion.Euler(-12,18+i*15,Mathf.Sin(t*.7f+i)*9);
            }
        }
        void OnDestroy()
        {
            if(previewCamera!=null)previewCamera.targetTexture=null;
            if(texture!=null){texture.Release();Destroy(texture);}
        }
    }
}
