using UnityEngine;

namespace Antchemy
{
    // Keep full pages in one coordinate system on short or notched screens.
    public sealed class ReferencePage : MonoBehaviour
    {
        void LateUpdate()
        {
            var parent=transform.parent as RectTransform;
            if(parent==null) return;
            float scale=Mathf.Min(parent.rect.width/720,parent.rect.height/1280);
            transform.localScale=Vector3.one*Mathf.Max(.1f,scale);
        }
        public static RectTransform Create(RectTransform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(ReferencePage)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(720,1280); return rect;
        }
    }
}
