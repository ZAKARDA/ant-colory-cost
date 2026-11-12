using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    // Package behavior: a pipe refills a just-picked matching tile anywhere on the board.
    // After a color change, it can reuse a cleared tile retaining that color.
    public static class ConveyorManager
    {
        sealed class Pipe
        {
            public ConveyorData Data;
            public int Index;
            public GameObject Visual;
            public Renderer ColorCap;
            public TextMesh Count;
            public bool Released;
            public int Remaining => Data.BeltColors.Count-Index;
            public int Color => Data.BeltColors[Index];
        }
        static readonly List<Pipe> Pipes = new List<Pipe>();
        static readonly SortedDictionary<int,int> PickedColors = new SortedDictionary<int,int>();
        public static bool CoversCell(int id) { foreach(var pipe in Pipes)if(pipe.Remaining>0&&pipe.Data.BeltPositions.Contains(id))return true;return false; }
        public static int PendingFoodCount { get { int count=0;foreach(var pipe in Pipes)count+=pipe.Remaining;return count; } }
        public static void Clear()
        {
            foreach(var pipe in Pipes)if(pipe.Visual!=null){pipe.Visual.SetActive(false);Object.Destroy(pipe.Visual);}
            Pipes.Clear();PickedColors.Clear();
        }
        public static void Initialize(LevelData level)
        {
            Clear();
            if(level.ConveyorData==null)return;
            var grid=PictureGrid.Instance;
            foreach(var data in level.ConveyorData)
            {
                if(data.BeltPositions==null||data.BeltPositions.Count==0||data.BeltColors==null||data.BeltColors.Count==0)continue;
                var pipe=new Pipe{Data=data};Pipes.Add(pipe);
                var obj=new GameObject("Pixel refill pipe");obj.transform.SetParent(grid.transform,false);pipe.Visual=obj;
                Vector3 center=Vector3.zero;
                foreach(int id in data.BeltPositions)center+=grid.GetWorldPosition(id);
                center/=data.BeltPositions.Count;center.z=-.35f;obj.transform.position=center;
                float diameter=grid.CellSpacing*Mathf.Sqrt(data.BeltPositions.Count)*1.08f;
                var rim=GameplayVisuals.Shape(obj.transform,"Pipe rim",PrimitiveType.Cylinder,Vector3.zero,new Vector3(diameter,.12f,diameter),new Color(.34f,.39f,.43f));
                rim.transform.localRotation=Quaternion.Euler(90,0,0);
                var cap=GameplayVisuals.Shape(obj.transform,"Current refill color",PrimitiveType.Sphere,new Vector3(0,0,-.13f),new Vector3(diameter*.72f,diameter*.72f,.18f),ColorPalette.GetColor(pipe.Color));pipe.ColorCap=cap.GetComponent<Renderer>();
                var label=new GameObject("Pipe food remaining");label.transform.SetParent(obj.transform,false);label.transform.localPosition=new Vector3(0,0,-.3f);
                pipe.Count=label.AddComponent<TextMesh>();pipe.Count.anchor=TextAnchor.MiddleCenter;pipe.Count.fontStyle=FontStyle.Bold;pipe.Count.fontSize=48;pipe.Count.characterSize=diameter*.13f;pipe.Count.color=Color.white;
                grid.SetBlockedCells(data.BeltPositions,true);UpdateVisual(pipe);
            }
        }
        public static void OnFoodPicked(Tile food)
        {
            PickedColors[food.CubeDataId]=food.ColorIndex;
            // First callback claims the removed tile; other pipes see it as Selected in the source.
            foreach(var pipe in Pipes)
                if(pipe.Remaining>0 && pipe.Color==food.ColorIndex)
                { Emit(pipe,food.CubeDataId);break; }
            CheckAutomaticRefills();
        }
        static void CheckAutomaticRefills()
        {
            var grid=PictureGrid.Instance;
            foreach(var pipe in Pipes)
            {
                if(pipe.Remaining<=0)continue;
                int nextColor=pipe.Color;
                if(HasNormalFood(nextColor))continue;
                foreach(var picked in PickedColors)
                {
                    if (pipe.Remaining <= 0 || pipe.Color != nextColor) break;
                    if(picked.Value!=nextColor||grid.Cubes.ContainsKey(picked.Key)||grid.IsBlocked(picked.Key))continue;
                    Emit(pipe,picked.Key);
                }
            }
        }
        static bool HasNormalFood(int color)
        {
            var grid=PictureGrid.Instance;
            foreach(var cell in grid.Cubes)
                if(cell.Value.ColorIndex==color&&!cell.Value.IsDestroyed&&!cell.Value.IsReserved&&!grid.IsBlocked(cell.Key))return true;
            return false;
        }
        static void Emit(Pipe pipe,int cubeId)
        {
            int color=pipe.Color;pipe.Index++;
            if (!PictureGrid.Instance.AddFood(cubeId,color)) { pipe.Index--; return; }
            UpdateVisual(pipe);
            if(pipe.Remaining==0 && !pipe.Released)
            {
                pipe.Released=true;
                // Preserve a cookie/door occupying the same cell, should later content overlap.
                var release=new List<int>();foreach(int id in pipe.Data.BeltPositions)if(!BlockerManager.CoversCell(id))release.Add(id);
                PictureGrid.Instance.SetBlockedCells(release,false);
                if(pipe.Visual!=null){pipe.Visual.SetActive(false);Object.Destroy(pipe.Visual);}
            }
        }
        static void UpdateVisual(Pipe pipe)
        {
            if(pipe.Count!=null)pipe.Count.text=pipe.Remaining.ToString();
            if(pipe.Remaining>0&&pipe.ColorCap!=null)GameplayVisuals.Paint(pipe.ColorCap,ColorPalette.GetColor(pipe.Color));
        }
    }
}
