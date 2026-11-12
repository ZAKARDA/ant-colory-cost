using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    // Covers the initial ad-loading window and consumes UI taps until the opportunity ends.
    static class AppOpenLoadingScreen
    {
        public static GameObject Create(Transform parent)
        {
            var root=new GameObject("Opening Colony",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(parent,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=32760;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(540,960);
            var panel=new GameObject("Loading",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(root.transform,false);
            var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(1f,.955f,.855f);
            var label=new GameObject("Title",typeof(RectTransform),typeof(Text)); label.transform.SetParent(panel.transform,false);
            rect=label.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
            var text=label.GetComponent<Text>(); text.font=UIManager.PremiumFont(true);
            text.text="ANT COLONY COLORS\n\nOpening your colony…"; text.fontSize=28; text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(.40f,.27f,.18f); text.raycastTarget=false;
            return root;
        }
    }
}
