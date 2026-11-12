using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    // Native pickup counters are global: cookies count every food, doors count their color.
    // Their footprint is a road obstruction, separate from the collectible food population.
    public static class BlockerManager
    {
        sealed class Blocker
        {
            public List<int> Cells;
            public int Remaining;
            public int RequiredColor;
            public GameObject Visual;
            public TextMesh Count;
            public bool Resolved;
        }
        static readonly List<Blocker> Active = new List<Blocker>();
        public static bool CoversCell(int id) { foreach(var b in Active)if(!b.Resolved&&b.Cells.Contains(id))return true;return false; }
        public static int ActiveCount { get { int n=0;foreach(var b in Active)if(!b.Resolved)n++;return n; } }
        public static void Initialize(LevelData data)
        {
            foreach(var previous in Active) if(previous.Visual!=null) { previous.Visual.SetActive(false);Object.Destroy(previous.Visual); }
            Active.Clear();
            if(data.SealedTiles!=null)foreach(var cookie in data.SealedTiles) Add(cookie.PixelIds,cookie.RequireCount,-1);
            if(data.Gates!=null)foreach(var door in data.Gates) Add(door.PixelIds,door.RequireCount,door.RequireColor);
        }
        static void Add(List<int> ids,int count,int color)
        {
            if(ids==null||ids.Count==0||count<=0)return;
            var grid=PictureGrid.Instance;
            var blocker=new Blocker {Cells=new List<int>(ids),Remaining=count,RequiredColor=color};
            var visual=new GameObject(color<0?"Sealed tile":"Color gate");visual.transform.SetParent(grid.transform,false);blocker.Visual=visual;
            Color face=color<0?new Color(.69f,.42f,.20f):ColorPalette.GetColonyColor(color);
            Vector3 center=Vector3.zero;
            var data=LevelManager.Instance.CurrentLevelData;
            float spacing=Mathf.Min(7.2f/data.SizeX,6.2f/data.SizeY);
            foreach(int id in ids)
            {
                // The package stores x * SizeY + y, as verified against recognizable artwork.
                Vector3 local=new Vector3((id/data.SizeY-(data.SizeX-1)*.5f)*spacing,(id%data.SizeY-(data.SizeY-1)*.5f)*spacing,-.3f);
                center+=local;
                GameplayVisuals.Shape(visual.transform,"Covered cell "+id,PrimitiveType.Cube,local,new Vector3(spacing*.99f,spacing*.99f,spacing*.55f),face);
                if(color<0 && id%3==0)
                    GameplayVisuals.Shape(visual.transform,"Chocolate chip",PrimitiveType.Sphere,local+new Vector3(spacing*.15f,0,-spacing*.4f),new Vector3(spacing*.19f,spacing*.21f,spacing*.13f),new Color(.25f,.12f,.055f));
            }
            center/=ids.Count;center.z-=spacing*.45f;
            var text=new GameObject("Required food count");text.transform.SetParent(visual.transform,false);text.transform.localPosition=center;
            blocker.Count=text.AddComponent<TextMesh>();blocker.Count.fontSize=48;blocker.Count.characterSize=Mathf.Min(.095f,spacing*.2f);
            blocker.Count.fontStyle=FontStyle.Bold;blocker.Count.anchor=TextAnchor.MiddleCenter;blocker.Count.color=Color.white;blocker.Count.text=count.ToString();
            Active.Add(blocker);grid.SetBlockedCells(ids,true);
        }
        public static void OnCubeRemoved(Tile cube)
        {
            bool changed=false;
            foreach(var blocker in Active)
            {
                if(blocker.Resolved || blocker.RequiredColor>=0&&blocker.RequiredColor!=cube.ColorIndex)continue;
                blocker.Remaining=Mathf.Max(0,blocker.Remaining-1);
                if(blocker.Count!=null)blocker.Count.text=blocker.Remaining.ToString();
                if(blocker.Remaining>0)continue;
                blocker.Resolved=true;changed=true;
                if(blocker.Visual!=null){blocker.Visual.SetActive(false);Object.Destroy(blocker.Visual);}
                var uncovered=new List<int>();
                foreach(int id in blocker.Cells)
                {
                    bool covered=ConveyorManager.CoversCell(id);
                    foreach(var other in Active)if(!other.Resolved&&other.Cells.Contains(id)){covered=true;break;}
                    if(!covered)uncovered.Add(id);
                }
                PictureGrid.Instance.SetBlockedCells(uncovered,false);
            }
            if(changed)LevelManager.Instance?.CheckGameStateAfterMove();
        }
    }
}
