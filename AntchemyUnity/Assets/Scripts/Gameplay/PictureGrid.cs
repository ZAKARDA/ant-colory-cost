using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    public class PictureGrid : MonoBehaviour
    {
        public static PictureGrid Instance { get; private set; }
        private readonly Dictionary<int, Tile> _cells = new Dictionary<int, Tile>();
        private readonly HashSet<int> _exposed = new HashSet<int>();
        private readonly HashSet<int> _blocked = new HashSet<int>();
        private readonly HashSet<int> _reachableEmpty = new HashSet<int>();
        public float CellSpacing => _spacing;
        public int Width => _width;
        public int Height => _height;
        private int _width, _height;
        private float _spacing;
        public int TotalRemainingCubes => _cells.Count+ConveyorManager.PendingFoodCount;
        public int TotalInitialCubes { get; private set; }
        public int ReservedCubeCount { get { int count = 0; foreach(var cube in _cells.Values) if(cube.IsReserved) count++; return count; } }
        public IReadOnlyDictionary<int, Tile> Cubes => _cells;
        private void Awake() { Instance = this; }
        private void OnDestroy() { if(Instance == this) Instance = null; }
        public void BuildGrid(LevelData data)
        {
            foreach(Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _cells.Clear(); _exposed.Clear(); _blocked.Clear();
            _width = Mathf.Max(1,data.SizeX); _height = Mathf.Max(1,data.SizeY);
            transform.position = new Vector3(0,3.5f,0);
            float spacing = Mathf.Min(7.2f/_width, 6.2f/_height); _spacing=spacing;
            var pipeFootprint=new HashSet<int>(); int pipeFood=0;
            if(data.ConveyorData!=null) foreach(var pipe in data.ConveyorData) { pipeFood+=pipe.BeltColors.Count; foreach(int id in pipe.BeltPositions) pipeFootprint.Add(id); }
            foreach(var group in data.ColorData)
            foreach(int id in group.CubeDataIds)
            {
                if(id < 0 || id >= _width*_height || _cells.ContainsKey(id)||pipeFootprint.Contains(id)) continue;
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"Cube_{id}_Color_{group.ColorIndex}";
                obj.transform.SetParent(transform,false);
                obj.transform.localPosition = new Vector3((id/_height-(_width-1)*.5f)*spacing, (id%_height-(_height-1)*.5f)*spacing,0);
                obj.transform.localScale = new Vector3(spacing*.92f,spacing*.92f,spacing*.72f);
                Destroy(obj.GetComponent<Collider>());
                var cube = obj.AddComponent<Tile>(); cube.Initialize(id,group.ColorIndex);
                cube.ConfigureHidden(data.HiddenData!=null&&data.HiddenData.HiddenCubeDataIds.Contains(id)); _cells.Add(id,cube);
            }
            TotalInitialCubes = _cells.Count+pipeFood;
            RefreshExposure();
        }
        // Empty cells reachable from a padded outside border are flood-filled.
        // A cube bordering that region is collectible; an enclosed hole is not exterior.
        private void RefreshExposure()
        {
            _exposed.Clear(); _reachableEmpty.Clear();
            int stride = _width+2, paddedHeight = _height+2;
            var visited = new bool[stride*paddedHeight];
            var queue = new Queue<int>(); queue.Enqueue(0); visited[0]=true;
            while(queue.Count>0)
            {
                int p = queue.Dequeue(), x = p%stride, y = p/stride;
                Visit(x-1,y,stride,paddedHeight,visited,queue);
                Visit(x+1,y,stride,paddedHeight,visited,queue);
                Visit(x,y-1,stride,paddedHeight,visited,queue);
                Visit(x,y+1,stride,paddedHeight,visited,queue);
            }
            foreach(int id in _exposed) if(!_blocked.Contains(id)) _cells[id].Reveal();
        }
        private void Visit(int x,int y,int stride,int paddedHeight,bool[] visited,Queue<int> queue)
        {
            if(x<0||y<0||x>=stride||y>=paddedHeight) return;
            int paddedId=x+y*stride;
            if(visited[paddedId]) return;
            if(x>0&&x<=_width&&y>0&&y<=_height)
            {
                int cellId=(x-1)*_height+y-1;
                if(_blocked.Contains(cellId)) return;
                if(_cells.ContainsKey(cellId)) { _exposed.Add(cellId); return; }
                _reachableEmpty.Add(cellId);
            }
            visited[paddedId]=true; queue.Enqueue(paddedId);
        }
        public bool AddFood(int cubeId,int colorIndex)
        {
            if(cubeId<0||cubeId>=_width*_height||_cells.ContainsKey(cubeId)) return false;
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=$"RefillCube_{cubeId}_Color_{colorIndex}";
            obj.transform.SetParent(transform,false); obj.transform.position=GetWorldPosition(cubeId);
            obj.transform.localScale=new Vector3(_spacing*.92f,_spacing*.92f,_spacing*.72f);
            Destroy(obj.GetComponent<Collider>());
            var cube=obj.AddComponent<Tile>(); cube.Initialize(cubeId,colorIndex); _cells.Add(cubeId,cube);
            RefreshExposure(); return true;
        }
        public bool IsEmptyReachable(int cubeId) => _reachableEmpty.Contains(cubeId);
        public Vector3 GetWorldPosition(int cubeId)
        {
            return transform.TransformPoint(new Vector3((cubeId/_height-(_width-1)*.5f)*_spacing,(cubeId%_height-(_height-1)*.5f)*_spacing,0));
        }
        public bool IsWorldPositionOccupied(Vector3 world)
        {
            Vector3 local=transform.InverseTransformPoint(world);
            int x=Mathf.RoundToInt(local.x/_spacing+(_width-1)*.5f);
            int y=Mathf.RoundToInt(local.y/_spacing+(_height-1)*.5f);
            return x>=0&&x<_width&&y>=0&&y<_height&&(_cells.ContainsKey(x*_height+y)||_blocked.Contains(x*_height+y));
        }
        public List<Vector3> GetPathToCube(int cubeId,Vector3 home)
        {
            var path=new List<Vector3>();
            if(!_cells.ContainsKey(cubeId)||!IsExposed(cubeId)) return path;
            int stride=_width+2,paddedHeight=_height+2;
            int start=Mathf.Clamp((_width+1)/2,1,_width); // Bottom exterior, near the shared ant hole.
            int targetX=cubeId/_height+1,targetY=cubeId%_height+1;
            var parents=new int[stride*paddedHeight];
            for(int i=0;i<parents.Length;i++) parents[i]=-2;
            var queue=new Queue<int>(); queue.Enqueue(start); parents[start]=-1;
            int goal=-1;
            int[] dx={0,-1,1,0},dy={1,0,0,-1};
            while(queue.Count>0&&goal<0)
            {
                int current=queue.Dequeue(),x=current%stride,y=current/stride;
                for(int d=0;d<4;d++)
                {
                    int nx=x+dx[d],ny=y+dy[d];
                    if(nx==targetX&&ny==targetY) { goal=current; break; }
                    if(nx<0||ny<0||nx>=stride||ny>=paddedHeight) continue;
                    int next=nx+ny*stride;
                    if(parents[next]!=-2) continue;
                    if(nx>0&&nx<=_width&&ny>0&&ny<=_height&&(_cells.ContainsKey((nx-1)*_height+ny-1)||_blocked.Contains((nx-1)*_height+ny-1))) continue;
                    parents[next]=current; queue.Enqueue(next);
                }
            }
            if(goal<0) return path;
            var reversed=new List<int>();
            for(int current=goal;current>=0;current=parents[current]) reversed.Add(current);
            home.z=-.6f; path.Add(home); path.Add(new Vector3(0,-.7f,-.6f));
            for(int i=reversed.Count-1;i>=0;i--)
            {
                int id=reversed[i];
                Vector3 world=transform.TransformPoint(new Vector3((id%stride-1-(_width-1)*.5f)*_spacing,(id/stride-1-(_height-1)*.5f)*_spacing,0));
                world.z=-.6f; path.Add(world);
            }
            Vector3 destination=_cells[cubeId].transform.position; destination.z=-.6f; path.Add(destination);
            return path;
        }
        public bool IsBlocked(int cubeId) => _blocked.Contains(cubeId);
        public void SetBlockedCells(IEnumerable<int> cubeIds,bool blocked)
        {
            foreach(int id in cubeIds) { if(blocked) _blocked.Add(id); else _blocked.Remove(id); }
            RefreshExposure();
        }
        public bool IsExposed(int cubeId) => _exposed.Contains(cubeId)&&!_blocked.Contains(cubeId);
        public bool HasCubesOfColor(int colorIndex)
        {
            foreach(var cube in _cells.Values) if(cube.ColorIndex==colorIndex) return true;
            return false;
        }
        public bool HasAvailableTarget(int colorIndex) => GetTargetCube(colorIndex,Vector3.zero)!=null;
        public Tile GetTargetCube(int colorIndex,Vector3 fromPos)
        {
            Tile best=null; float distance=float.MaxValue;
            foreach(int id in _exposed)
            {
                var cube = _cells[id];
                if(cube.ColorIndex!=colorIndex||cube.IsDestroyed||cube.IsReserved||_blocked.Contains(id)) continue;
                float d=(cube.transform.position-fromPos).sqrMagnitude;
                if(d<distance || (Mathf.Approximately(d,distance)&&best!=null&&id<best.CubeDataId)) { best=cube; distance=d; }
            }
            return best;
        }
        public Tile ReserveTargetCube(int colorIndex, Vector3 fromPos)
        {
            Tile cube=GetTargetCube(colorIndex,fromPos);
            return cube!=null&&cube.TryReserve()?cube:null;
        }
        public void OnCubeDestroyed(Tile cube)
        {
            if(!_cells.TryGetValue(cube.CubeDataId,out var existing)||existing!=cube) return;
            _cells.Remove(cube.CubeDataId); BlockerManager.OnCubeRemoved(cube); ConveyorManager.OnFoodPicked(cube); LevelManager.Instance?.OnFoodCollected(cube.ColorIndex); RefreshExposure();
            LevelManager.Instance?.OnBoardChanged();
            UIManager.Instance?.UpdateProgress(TotalRemainingCubes,TotalInitialCubes);
            if(TotalRemainingCubes==0) LevelManager.Instance?.OnAllCubesCleared();
        }
    }
}
