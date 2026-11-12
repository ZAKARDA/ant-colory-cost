using UnityEngine;

namespace Antchemy
{
    // Blur only the static paper backdrop. UI text and gameplay stay sharp.
    public sealed class FrostedBackdrop : MonoBehaviour
    {
        RenderTexture blurred,scratch;
        Material blur;
        float size;
        static readonly int Backdrop=Shader.PropertyToID("_ColonyFrostedBackdrop");
        void Awake() { Shader.SetGlobalTexture(Backdrop,Texture2D.whiteTexture); }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            int w=Mathf.Max(1,source.width/4),h=Mathf.Max(1,source.height/4);
            float currentSize=GetComponent<Camera>().orthographicSize;
            bool dirty=blurred==null || blurred.width!=w || blurred.height!=h || currentSize!=size;
            if(dirty)
            {
                Release(); size=currentSize;
                blurred=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32) {name="Frosted paper",filterMode=FilterMode.Bilinear};
                scratch=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32) {name="Frosted paper scratch",filterMode=FilterMode.Bilinear};
                if(blur==null) blur=new Material(Resources.Load<Shader>("UI/GlassBlur"));
                Graphics.Blit(source,blurred);
                blur.SetVector("_Direction",new Vector4(2f/w,0,0,0)); Graphics.Blit(blurred,scratch,blur);
                blur.SetVector("_Direction",new Vector4(0,2f/h,0,0)); Graphics.Blit(scratch,blurred,blur);
                Shader.SetGlobalTexture(Backdrop,blurred);
            }
            Graphics.Blit(source,destination);
        }
        void Release()
        {
            if(blurred!=null) { blurred.Release(); Destroy(blurred); }
            if(scratch!=null) { scratch.Release(); Destroy(scratch); }
        }
        void OnDestroy() { Release(); if(blur!=null) Destroy(blur); Shader.SetGlobalTexture(Backdrop,Texture2D.whiteTexture); }
    }
}
