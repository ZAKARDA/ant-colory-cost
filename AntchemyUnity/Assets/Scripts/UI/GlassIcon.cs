using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    public sealed class GlassIcon : MaskableGraphic
    {
        public enum Kind { Nest,Restart,Sound,Pictures,Home,Pause,Shuffle,Play }
        public Kind icon;
        int _frames;
        public override Texture mainTexture => Texture2D.whiteTexture;
        void LateUpdate() { if(_frames<3) { _frames++; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(icon)
            {
                case Kind.Nest:
                    Line(vh,new Vector2(-11,0),new Vector2(-1,0)); Line(vh,new Vector2(-6,-5),new Vector2(-6,5));
                    Line(vh,new Vector2(5,3),new Vector2(9,6)); Line(vh,new Vector2(9,6),new Vector2(9,-6)); break;
                case Kind.Restart:
                    Vector2 last=Point(35,10);
                    for(int i=1;i<=24;i++) { var next=Point(35+i*300f/24,10); Line(vh,last,next); last=next; }
                    var tangent=Point(65,1); var normal=new Vector2(-tangent.y,tangent.x);
                    Line(vh,last,last-tangent*5+normal*3); Line(vh,last,last-tangent*5-normal*3); break;
                case Kind.Sound:
                    Line(vh,new Vector2(-4,-5),new Vector2(-4,9)); Line(vh,new Vector2(8,-2),new Vector2(8,12));
                    Line(vh,new Vector2(-4,9),new Vector2(8,12)); Dot(vh,new Vector2(-7,-6),4); Dot(vh,new Vector2(5,-3),4); break;
                case Kind.Pictures:
                    for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) Box(vh,new Vector2(x*6,y*6),new Vector2(7,7)); break;
                case Kind.Home:
                    Line(vh,new Vector2(-12,1),new Vector2(0,11)); Line(vh,new Vector2(0,11),new Vector2(12,1));
                    Line(vh,new Vector2(-8,3),new Vector2(-8,-10)); Line(vh,new Vector2(-8,-10),new Vector2(8,-10)); Line(vh,new Vector2(8,-10),new Vector2(8,3));
                    Line(vh,new Vector2(-2,-10),new Vector2(-2,-3)); Line(vh,new Vector2(-2,-3),new Vector2(3,-3)); Line(vh,new Vector2(3,-3),new Vector2(3,-10)); break;
                case Kind.Shuffle:
                    Line(vh,new Vector2(-11,6),new Vector2(-3,6)); Line(vh,new Vector2(-3,6),new Vector2(3,-6)); Line(vh,new Vector2(3,-6),new Vector2(10,-6));
                    Line(vh,new Vector2(-11,-6),new Vector2(-3,-6)); Line(vh,new Vector2(-3,-6),new Vector2(3,6)); Line(vh,new Vector2(3,6),new Vector2(10,6));
                    Line(vh,new Vector2(10,6),new Vector2(7,9)); Line(vh,new Vector2(10,6),new Vector2(7,3)); break;
                case Kind.Play:
                    { int s0=vh.currentVertCount; Vertex(vh,new Vector2(-6,-10)); Vertex(vh,new Vector2(-6,10)); Vertex(vh,new Vector2(11,0)); vh.AddTriangle(s0,s0+1,s0+2); } break;
                case Kind.Pause:
                    Box(vh,new Vector2(-5,0),new Vector2(3,20)); Box(vh,new Vector2(5,0),new Vector2(3,20)); break;
            }
        }
        static Vector2 Point(float degrees,float radius) => new Vector2(Mathf.Cos(degrees*Mathf.Deg2Rad),Mathf.Sin(degrees*Mathf.Deg2Rad))*radius;
        void Line(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var d=(b-a).normalized; var n=new Vector2(-d.y,d.x)*1.9f;
            Quad(vh,a-n,a+n,b+n,b-n); Dot(vh,a,1.9f); Dot(vh,b,1.9f);
        }
        void Box(VertexHelper vh,Vector2 center,Vector2 size)
        { var h=size*.5f; Quad(vh,center+new Vector2(-h.x,-h.y),center+new Vector2(-h.x,h.y),center+h,center+new Vector2(h.x,-h.y)); }
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int start=vh.currentVertCount; Vertex(vh,a); Vertex(vh,b); Vertex(vh,c); Vertex(vh,d);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
        void Vertex(VertexHelper vh,Vector2 point) { var r=rectTransform.rect; vh.AddVert(r.center+point*(r.width/32),color,Vector2.zero); }
        void Dot(VertexHelper vh,Vector2 center,float radius)
        {
            int start=vh.currentVertCount; Vertex(vh,center);
            for(int i=0;i<=12;i++) Vertex(vh,center+Point(i*30,radius));
            for(int i=0;i<12;i++) vh.AddTriangle(start,start+i+1,start+i+2);
        }
    }
}
