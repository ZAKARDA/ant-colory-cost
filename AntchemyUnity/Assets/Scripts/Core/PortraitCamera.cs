using UnityEngine;
namespace Antchemy
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public class PortraitCamera : MonoBehaviour
    {
        Camera camera,backdrop;
        void Awake()
        {
            Application.targetFrameRate=60; camera=GetComponent<Camera>();
            var paper=GameObject.Find("Diamond paper");
            if(paper!=null)
            {
                paper.layer=31;
                var go=new GameObject("Paper backdrop camera"); go.transform.SetParent(transform.parent,false);
                go.transform.position=new Vector3(0,0,-35);
                backdrop=go.AddComponent<Camera>(); backdrop.orthographic=true; backdrop.depth=-30;
                backdrop.clearFlags=CameraClearFlags.SolidColor; backdrop.backgroundColor=camera.backgroundColor;
                backdrop.cullingMask=1<<31; backdrop.allowHDR=false;
                go.AddComponent<FrostedBackdrop>();
                camera.cullingMask &= ~(1<<31); camera.clearFlags=CameraClearFlags.Depth;
            }
        }
        void LateUpdate()
        {
            camera.pixelRect=GameplayViewport.Calculate(Screen.safeArea,Screen.width);
            camera.transform.position=new Vector3(0,3.55f,-20);
            camera.orthographicSize=GameplayViewport.CameraSize(camera.aspect);
            if(backdrop!=null) backdrop.orthographicSize=Mathf.Max(10,6f/((float)Screen.width/Screen.height));
        }
        void OnDestroy() { if(backdrop!=null) Destroy(backdrop.gameObject); }
    }
}
