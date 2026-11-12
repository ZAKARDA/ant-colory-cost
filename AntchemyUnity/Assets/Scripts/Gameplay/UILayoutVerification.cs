using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    static class UILayoutVerification
    {
        public static IEnumerator CheckCollection(UIManager ui,Action<bool,string> check)
        {
            // Pagination retires the previous page at the end of the current frame.
            yield return null;
            var scroll=ui.GetComponentInChildren<ScrollRect>();
            var back=ui.GetComponentsInChildren<Button>().First(b=>b.name=="BACK TO COLONY");
            var header=ui.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Collection header");
            Canvas.ForceUpdateCanvases();
            check(scroll.viewport.GetComponent<RectMask2D>()!=null && !back.transform.IsChildOf(scroll.transform) &&
                ScreenRect((RectTransform)back.transform).yMax<ScreenRect(scroll.viewport).yMin &&
                ScreenRect(scroll.viewport).yMax<=ScreenRect(header).yMin+1,
                "collection clips without stencil and reserves separate header and Back areas");
            var first=scroll.content.GetChild(0).GetComponent<Image>();
            var last=scroll.content.GetChild(scroll.content.childCount-1).GetComponent<Image>();
            foreach(float position in new[]{1f,.5f,0f})
            {
                scroll.StopMovement();scroll.verticalNormalizedPosition=position;
                Canvas.ForceUpdateCanvases();
#if UNITY_EDITOR
                // Batch-mode Editor tests have no end-of-frame rendering callback.
                yield return null;
                Canvas.ForceUpdateCanvases();
#else
                yield return new WaitForEndOfFrame();
#endif
                bool clipped=position==1?last.canvasRenderer.cull:position==0?first.canvasRenderer.cull:first.canvasRenderer.cull&&last.canvasRenderer.cull;
                check(clipped,"offscreen collection cards are culled at scroll position "+position);
                var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {position=ScreenRect((RectTransform)back.transform).center};
                var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
                check(hits.Count>0 && hits[0].gameObject==back.gameObject,"Back remains the first tap target at scroll position "+position);
            }
            scroll.verticalNormalizedPosition=1;
        }
        public static void Run(UIManager ui,Action<bool,string> check)
        {
            var sizes=new[]{new Vector2(540,960),new Vector2(750,1334),new Vector2(1206,2622),new Vector2(1536,2048)};
            foreach(var size in sizes)
            {
                var safe=new Rect(0,size==sizes[2]?102:0,size.x,size.y-(size==sizes[2]?264:0));
                var view=GameplayViewport.Calculate(safe,size.x);
                float scale=size.x/GameplayViewport.ReferenceWidth;
                float half=GameplayViewport.CameraSize(view.width/view.height);
                var transform=Matrix4x4.TRS(new Vector3(0,3.55f,-20),Quaternion.Euler(8.53f,0,0),Vector3.one).inverse;
                bool inside=true;
                foreach(float x in new[]{-4.1f,4.1f}) foreach(float y in new[]{-6.2f,7.2f}) foreach(float z in new[]{-.6f,1.2f})
                {
                    var p=transform.MultiplyPoint(new Vector3(x,y,z));
                    inside &= Mathf.Abs(p.y)<half && Mathf.Abs(p.x)<half*view.width/view.height;
                }
                check(inside && view.yMin>=safe.yMin+GameplayViewport.ControlsHeight*scale-1 && view.yMax<=safe.yMax-GameplayViewport.HeaderHeight*scale+1,
                    "board, nests and three queue rows fit between separate controls at "+size.x+"x"+size.y);
            }
            var rects=ui.GetComponentsInChildren<RectTransform>();
            var bar=rects.First(x=>x.name=="Glass toolbar");
            var hint=rects.First(x=>x.name=="Game hint");
            var header=rects.First(x=>x.name=="Game header");
            var viewport=Camera.main.pixelRect;
            check(ScreenRect(bar).yMax<ScreenRect(hint).yMin && ScreenRect(hint).yMax<viewport.yMin && viewport.yMax<=ScreenRect(header).yMin+1,
                "live header, play viewport, hint and glass toolbar occupy separate vertical bands");
            var buttons=bar.GetComponentsInChildren<Button>();
            bool separate=buttons.Length==4;
            for(int a=0;a<buttons.Length;a++) for(int b=a+1;b<buttons.Length;b++)
                separate &= !ScreenRect((RectTransform)buttons[a].transform).Overlaps(ScreenRect((RectTransform)buttons[b].transform));
            check(separate && buttons.All(b=>b.GetComponentInChildren<GlassIcon>()!=null),"four glass tool buttons have distinct hit areas and font-independent icons");
            var front=LevelManager.Instance.Lanes.First(e=>e.FrontColony!=null).FrontColony;
            var point=Camera.main.WorldToScreenPoint(front.transform.position);
            Physics.SyncTransforms();
            bool hit=Physics.Raycast(Camera.main.ScreenPointToRay(point),out var contact,100);
            check(hit && contact.collider.GetComponent<Colony>()==front,"camera viewport maps colony taps back to the correct world collider");
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {position=point};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
            check(hits.Count==0,"header and glass controls do not intercept taps on front colonies");
        }
        static Rect ScreenRect(RectTransform transform)
        {
            var corners=new Vector3[4]; transform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));
        }
    }
}
