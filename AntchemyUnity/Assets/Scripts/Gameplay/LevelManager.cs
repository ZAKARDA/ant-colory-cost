using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private Transform _slotsContainer;
        [SerializeField] private Transform _lanesContainer;
        [SerializeField] private int _defaultSlotCount=3;
        [SerializeField] private float _slotSpacing=1.35f;
        [SerializeField] private float _laneSpacing=1.35f;
        private readonly List<Nest> _battleSlots=new List<Nest>();
        private readonly List<ColonyLane> _lanes=new List<ColonyLane>();
        private readonly List<string> _unsupportedFeatures=new List<string>();
        private bool _isLevelEnded;
        private bool _hasLoaded;
        private float _nextCheck;
        private Colony _lastSelected;
        private readonly Dictionary<Colony,List<Colony>> _linkedMembers=new Dictionary<Colony,List<Colony>>();
        private readonly List<LockRuntime> _locks=new List<LockRuntime>();
        private readonly List<TunnelRuntime> _tunnels=new List<TunnelRuntime>();
        private class TunnelRuntime { public List<Colony> Colonies=new List<Colony>(); public int Color; public int Remaining; }
        private class LockRuntime { public Colony Colony; public LockKeyData Data; public GameObject Marker; }
        private int _combo; private float _lastFinishTime=-99f;
        // Finishing colonies back-to-back builds a combo that pays bonus coins.
        private void RegisterComboFinish()
        {
            float now=Time.unscaledTime;
            _combo=(now-_lastFinishTime<4f)?_combo+1:1; _lastFinishTime=now;
            if(_combo<3||GameManager.Instance==null) return;
            int bonus=(_combo-2)*2;
            GameManager.Instance.AddCoins(bonus);
            UIManager.Instance?.ShowToast("COMBO x"+_combo+"  +"+bonus+" coins");
        }
        public bool IsLevelEnded => _isLevelEnded;
        public LoseReason LastLoseReason { get; private set; }
        public int LoadedLevelNumber { get; private set; }
        public int LoadGeneration { get; private set; }
        public int SourceLevelNumber { get; private set; }
        public LevelData CurrentLevelData { get; private set; }
        public IReadOnlyList<Nest> Nests => _battleSlots;
        public IReadOnlyList<ColonyLane> Lanes => _lanes;
        public IReadOnlyList<string> UnsupportedFeatures => _unsupportedFeatures;
        public bool HasUnsupportedFeatures => _unsupportedFeatures.Count>0;
        public string UnsupportedFeatureSummary => string.Join(", ",_unsupportedFeatures);
        public int OccupiedSlotCount { get { int count=0; foreach(var slot in _battleSlots) if(!slot.IsFree) count++; return count; } }
        public int SlotCount => _battleSlots.Count;
        public bool TryShuffleQueues()
        {
            var rng=new System.Random(); bool any=false;
            foreach(var lane in _lanes) if(lane!=null&&lane.ShuffleQueue(rng)) any=true;
            if(any) OnBoardChanged();
            return any;
        }
        public int QueuedColonyCount { get { int count=0; foreach(var lane in _lanes) count+=lane.RemainingColonies; return count; } }
        public bool CanUndoLastMove => !_isLevelEnded&&_lastSelected!=null&&_lastSelected.CanUndoSelection;
        private void Awake() { Instance=this; }
        private void OnDestroy() { if(Instance==this) { Projectile.CancelAll(); Instance=null; } }
        private void Start() { if(!_hasLoaded) LoadLevel(GameManager.Instance!=null?GameManager.Instance.CurrentLevel:1); }
        private void Update()
        {
            if(!_hasLoaded||_isLevelEnded||Time.timeScale<=0||Time.time<_nextCheck) return;
            _nextCheck=Time.time+.25f; EvaluateGameState();
        }
        public void LoadLevel(int levelNumber)
        {
            levelNumber=Mathf.Max(1,levelNumber);
            int total=GameManager.Instance!=null?GameManager.Instance.LevelCount:Mathf.Max(1,Resources.LoadAll<TextAsset>("Levels").Length);
            int source=((levelNumber-1)%total)+1;
            var asset=Resources.Load<TextAsset>($"Levels/Level_{source}");
            if(asset==null) { source=1; asset=Resources.Load<TextAsset>("Levels/Level_1"); }
            if(asset==null) { Debug.LogError("No level JSON available in Resources/Levels."); return; }
            var data=JsonUtility.FromJson<LevelData>(asset.text);
            if(data==null||data.SizeX<=0||data.SizeY<=0) { Debug.LogError("Invalid level dimensions."); return; }
            SourceLevelNumber=source;
            LoadLevelData(data,levelNumber);
            if(HasUnsupportedFeatures) Debug.LogWarning("This level uses simplified mechanics. Unsupported: "+UnsupportedFeatureSummary);
        }
        public void LoadLevelData(LevelData data,int levelNumber=1)
        {
            if(data==null||data.SizeX<=0||data.SizeY<=0) return;
            data.Normalize();
            LoadGeneration++; _combo=0; _lastFinishTime=-99f;
            _isLevelEnded=true; Time.timeScale=1;
            StopAllCoroutines(); Projectile.CancelAll(); ConveyorManager.Clear(); _lastSelected=null;
            foreach(var slot in _battleSlots) if(slot!=null) { slot.gameObject.SetActive(false); Destroy(slot.gameObject); }
            foreach(var lane in _lanes) if(lane!=null) { lane.gameObject.SetActive(false); Destroy(lane.gameObject); }
            _battleSlots.Clear(); _lanes.Clear(); _unsupportedFeatures.Clear(); _linkedMembers.Clear(); _locks.Clear(); _tunnels.Clear();
            CurrentLevelData=data; LoadedLevelNumber=levelNumber; LastLoseReason=LoseReason.None;

            PictureGrid grid=PictureGrid.Instance;
            if(grid==null)
            {
                var board=new GameObject("PictureGrid"); board.transform.SetParent(_boardContainer!=null?_boardContainer:transform,false);
                grid=board.AddComponent<PictureGrid>();
            }
            grid.BuildGrid(data); BlockerManager.Initialize(data); ConveyorManager.Initialize(data);
            BuildNests(Mathf.Clamp(_defaultSlotCount,1,7)); BuildLanes(data.LaneData);
            ConfigureAdvancedMechanics(data); OnBoardChanged();
            _hasLoaded=true; _isLevelEnded=false; _nextCheck=Time.time+.5f;
            UIManager.Instance?.OnLevelLoaded(levelNumber,grid.TotalRemainingCubes);
            AudioManager.Instance?.PlayMusic(UIManager.Instance != null && UIManager.Instance.IsHomeVisible ? "music_home" : "music_gameplay");
        }
        private void BuildNests(int count)
        {
            for(int i=0;i<count;i++) CreateSlot(i);
            RepositionSlots();
        }
        private void CreateSlot(int index)
        {
            var obj=new GameObject($"Nest_{index}"); obj.transform.SetParent(_slotsContainer!=null?_slotsContainer:transform,false);
            var slot=obj.AddComponent<Nest>(); slot.Initialize(index); _battleSlots.Add(slot);
        }
        // Usable world width of the play area for the current screen shape.
        private static float PlayWidth()
        {
            var view=GameplayViewport.Calculate(Screen.safeArea,Screen.width);
            float aspect=view.width/Mathf.Max(1f,view.height);
            return 2f*GameplayViewport.CameraSize(aspect)*aspect;
        }
        private void RepositionSlots()
        {
            // Nests always keep a visible gap and shrink together when the row would not fit.
            const float nestWidth=1.12f, gapRatio=.18f;
            int n=Mathf.Max(1,_battleSlots.Count);
            float available=Mathf.Max(2f,PlayWidth()-.6f);
            float w=Mathf.Min(nestWidth,available/(n+(n-1)*gapRatio));
            float spacing=w*(1f+gapRatio), k=w/nestWidth;
            for(int i=0;i<_battleSlots.Count;i++)
            {
                var t=_battleSlots[i].transform;
                t.localScale=Vector3.one*k;
                t.position=new Vector3((i-(_battleSlots.Count-1)*.5f)*spacing,-2f,0);
            }
        }
        private void BuildLanes(List<LaneColumn> columns)
        {
            float spacing=Mathf.Min(_laneSpacing,Mathf.Max(.9f,(PlayWidth()-1.2f)/Mathf.Max(1,columns.Count-1)));
            for(int i=0;i<columns.Count;i++)
            {
                var obj=new GameObject($"ColonyQueue_{i}"); obj.transform.SetParent(_lanesContainer!=null?_lanesContainer:transform,false);
                obj.transform.position=new Vector3((i-(columns.Count-1)*.5f)*spacing,-3.6f,0);
                var lane=obj.AddComponent<ColonyLane>(); lane.Initialize(i,columns[i]); _lanes.Add(lane);
            }
        }
        private bool TryGetDeployableGroup(Colony colony,int availableSlots,out List<Colony> group)
        {
            group=null;
            if(colony==null||colony.CurrentState!=ColonyState.InQueue||!colony.IsFrontColony||colony.ParentLane==null||!colony.CanDeploy) return false;
            group=_linkedMembers.TryGetValue(colony,out var members)?members:new List<Colony>{colony};
            if(availableSlots<group.Count) return false;
            foreach(var member in group) if(member==null||member.CurrentState!=ColonyState.InQueue||!member.CanDeploy) return false;
            foreach(var lane in _lanes) if(!lane.IsContiguousFrontGroup(group)) return false;
            return true;
        }
        public void OnColonyTapped(Colony colony)
        {
            if(_isLevelEnded||Time.timeScale<=0||colony==null||colony.CurrentState!=ColonyState.InQueue||!colony.IsFrontColony||colony.ParentLane==null) return;
            var free=new List<Nest>(); foreach(var slot in _battleSlots) if(slot.IsFree) free.Add(slot);
            if(!TryGetDeployableGroup(colony,free.Count,out var group)) return;
            // Validate the entire link group before any queue, slot, or state mutation.
            foreach(var lane in _lanes) lane.RemoveLinkedPrefix(group);
            for(int i=0;i<group.Count;i++) { free[i].AssignColony(group[i]); group[i].MoveToSlot(free[i],free[i].transform.position); }
            _lastSelected=group.Count==1?colony:null;
            OnBoardChanged(); _nextCheck=Time.time+.35f;
        }
        private Colony FindSourceColony(ColonyPositionData position)
        {
            if(position==null||position.LaneId<0||position.LaneId>=_lanes.Count) return null;
            foreach(var colony in _lanes[position.LaneId].Colonies) if(colony.SourceQueueIndex==position.ColonyId) return colony;
            return null;
        }
        private void ConfigureAdvancedMechanics(LevelData data)
        {
            var graph=new Dictionary<Colony,HashSet<Colony>>();
            if(data.LinkedColonies!=null) foreach(var linked in data.LinkedColonies)
            {
                var found=new List<Colony>();
                foreach(var position in linked.Ids) { var member=FindSourceColony(position); if(member!=null) found.Add(member); }
                foreach(var member in found)
                {
                    if(!graph.ContainsKey(member)) graph[member]=new HashSet<Colony>();
                    foreach(var other in found) graph[member].Add(other);
                }
            }
            var visited=new HashSet<Colony>(); int groupId=0;
            foreach(var member in graph.Keys)
            {
                if(!visited.Add(member)) continue;
                var group=new List<Colony>(); var queue=new Queue<Colony>(); queue.Enqueue(member);
                while(queue.Count>0)
                {
                    var current=queue.Dequeue(); group.Add(current);
                    foreach(var other in graph[current]) if(visited.Add(other)) queue.Enqueue(other);
                }
                group.Sort((a,b)=>a.ParentLane.ColumnIndex==b.ParentLane.ColumnIndex?a.SourceQueueIndex.CompareTo(b.SourceQueueIndex):a.ParentLane.ColumnIndex.CompareTo(b.ParentLane.ColumnIndex));
                foreach(var current in group) { _linkedMembers[current]=group; current.SetLinkedGroup(groupId); }
                groupId++;
            }
            if(data.LockKeys!=null) foreach(var key in data.LockKeys)
            {
                var colony=FindSourceColony(key.LockPos); if(colony==null||!colony.IsLockPlaceholder) { _unsupportedFeatures.Add("invalid lock reference"); continue; }
                var marker=new GameObject("KeyFor_"+key.LockPos.LaneId+"_"+key.LockPos.ColonyId);
                marker.transform.SetParent(PictureGrid.Instance.transform,false);
                Vector3 center=Vector3.zero; foreach(int id in key.PixelIds) center+=PictureGrid.Instance.GetWorldPosition(id);
                if(key.PixelIds.Count>0) center/=key.PixelIds.Count; center.z=-.4f; marker.transform.position=center;
                float scale=PictureGrid.Instance.CellSpacing;
                GameplayVisuals.Shape(marker.transform,"KeyBow",PrimitiveType.Sphere,new Vector3(-.28f,0,0)*scale,new Vector3(.55f,.55f,.18f)*scale,new Color(1,.75f,.08f));
                GameplayVisuals.Shape(marker.transform,"KeyShaft",PrimitiveType.Cube,new Vector3(.2f,0,0)*scale,new Vector3(.7f,.15f,.15f)*scale,new Color(1,.75f,.08f));
                GameplayVisuals.Shape(marker.transform,"KeyTooth",PrimitiveType.Cube,new Vector3(.48f,-.13f,0)*scale,new Vector3(.14f,.25f,.15f)*scale,new Color(1,.75f,.08f));
                _locks.Add(new LockRuntime { Colony=colony,Data=key,Marker=marker });
            }
            for(int column=0;column<data.LaneData.Count;column++)
            {
                var authored=data.LaneData[column]; if(authored.Burrows==null) continue;
                foreach(var tunnel in authored.Burrows)
                {
                    if(tunnel.RequireCount<=0) continue;
                    var runtime=new TunnelRuntime {Color=tunnel.RequireColor,Remaining=tunnel.RequireCount};
                    foreach(var colony in _lanes[column].Colonies)
                        if(colony.SourceTokenIndex>=Mathf.Max(0,tunnel.StartIndex)&&colony.SourceTokenIndex<tunnel.StartIndex+tunnel.CoverCount)
                        { runtime.Colonies.Add(colony); colony.AddTunnelCover(runtime.Remaining); }
                    _tunnels.Add(runtime);
                }
            }
        }
        public void OnFoodCollected(int colorIndex)
        {
            foreach(var tunnel in _tunnels)
            {
                if(tunnel.Remaining<=0||tunnel.Color!=colorIndex) continue;
                tunnel.Remaining--;
                foreach(var colony in tunnel.Colonies)
                {
                    if(colony==null) continue;
                    if(tunnel.Remaining==0) colony.RemoveTunnelCover(); else colony.UpdateTunnelCount(tunnel.Remaining);
                }
            }
        }
        public void OnBoardChanged()
        {
            // A native key occupies empty road cells. It resolves once the flood-fill can
            // reach any key cell and its zero-capacity queue lock is removable at the front.
            bool changed;
            do
            {
                changed=false;
                for(int i=_locks.Count-1;i>=0;i--)
                {
                    var item=_locks[i]; if(item.Colony==null) { _locks.RemoveAt(i); continue; }
                    if(!item.Colony.IsFrontColony||item.Colony.IsCoveredByTunnel) continue;
                    bool reachable=false; foreach(int id in item.Data.PixelIds) if(PictureGrid.Instance.IsEmptyReachable(id)) { reachable=true; break; }
                    if(!reachable) continue;
                    item.Colony.ParentLane.RemoveUnlockedPlaceholder(item.Colony);
                    if(item.Marker!=null) { item.Marker.SetActive(false); Destroy(item.Marker); }
                    _locks.RemoveAt(i); changed=true;
                }
                Colony dynamite=null,rock=null;
                foreach(var lane in _lanes)
                {
                    var front=lane.FrontColony;
                    if(front==null||front.IsCoveredByTunnel||front.IceHp>0||front.ObstacleType!=1) continue;
                    foreach(var other in _lanes)
                    {
                        var candidate=other.FrontColony;
                        if(candidate!=null&&!candidate.IsCoveredByTunnel&&candidate.IceHp<=0&&candidate.ObstacleType==2&&candidate.ObstaclePairId==front.ObstaclePairId)
                        { dynamite=front; rock=candidate; break; }
                    }
                    if(dynamite!=null) break;
                }
                if(dynamite!=null)
                {
                    dynamite.ParentLane.RemoveUnlockedPlaceholder(dynamite); rock.ParentLane.RemoveUnlockedPlaceholder(rock); changed=true;
                }
            } while(changed);
            CheckGameStateAfterMove();
        }
        public Nest GetFirstFreeSlot() { foreach(var slot in _battleSlots) if(slot.IsFree) return slot; return null; }
        public bool TryUndoLastMove()
        {
            if(!CanUndoLastMove||!_lastSelected.UndoSelection()) return false;
            _lastSelected=null; _nextCheck=Time.time+.25f; return true;
        }
        private bool IsColonyGroupDrained(Colony colony,out List<Colony> group)
        {
            group=_linkedMembers.TryGetValue(colony,out var linked)?linked:new List<Colony>{colony};
            foreach(var member in group)
                if(member!=null&&(member.AntCount>0||member.InFlightCount>0||member.CurrentState==ColonyState.MovingToSlot)) return false;
            return true;
        }
        public bool TryCompleteColonyGroup(Colony colony)
        {
            if(colony==null||colony.CurrentState==ColonyState.Completed||!IsColonyGroupDrained(colony,out var group)) return false;
            // No yield: all linked nests stay occupied until every member's workers return.
            foreach(var member in group) if(member!=null) member.CompleteAfterGroupResolved();
            return true;
        }
        public void OnColonyFinished(Colony colony)
        {
            RegisterComboFinish();
            foreach(var lane in _lanes) foreach(var queued in lane.Colonies) if(queued!=colony) queued.ReduceIce();
            OnBoardChanged(); CheckGameStateAfterMove();
        }
        public void CheckGameStateAfterMove() { _nextCheck=Mathf.Min(_nextCheck,Time.time+.05f); }
        public void EvaluateGameState()
        {
            if(_isLevelEnded||PictureGrid.Instance==null) return;
            if(PictureGrid.Instance.TotalRemainingCubes==0) { OnAllCubesCleared(); return; }
            if(Projectile.InFlightCount>0) return;
            foreach(var slot in _battleSlots)
            {
                var colony=slot.OccupyingColony;
                if(colony==null) continue;
                if(colony.CurrentState==ColonyState.MovingToSlot||colony.InFlightCount>0) return;
                if(colony.AntCount>0&&PictureGrid.Instance.HasAvailableTarget(colony.ColorIndex)) return;
                if(colony.AntCount==0&&IsColonyGroupDrained(colony,out _)) return; // Completion loop will release this entire group.
            }
            // Remaining empty nests must fit a whole legal front group. One free nest
            // cannot make progress when the next linked group needs two nests.
            int freeSlots=_battleSlots.Count-OccupiedSlotCount;
            bool anyLegalGroup=false;
            foreach(var lane in _lanes)
            {
                if(TryGetDeployableGroup(lane.FrontColony,freeSlots,out _)) return;
                if(TryGetDeployableGroup(lane.FrontColony,int.MaxValue,out _)) anyLegalGroup=true;
            }
            OnLevelFailed(anyLegalGroup?LoseReason.OutOfSlot:LoseReason.LockStuck);
        }
        public void OnAllCubesCleared()
        {
            if(_isLevelEnded) return; _isLevelEnded=true;
            AudioManager.Instance?.PlaySfx("sfx_win"); GameManager.Instance?.OnLevelWon(); UIManager.Instance?.ShowWinDialog();
        }
        private void OnLevelFailed(LoseReason reason)
        {
            if(_isLevelEnded) return; _isLevelEnded=true; LastLoseReason=reason;
            AudioManager.Instance?.PlaySfx("sfx-Lose"); UIManager.Instance?.ShowLoseDialog(reason);
        }
        public bool CanAddSlot => _battleSlots.Count<7 && PictureGrid.Instance!=null && PictureGrid.Instance.TotalRemainingCubes>0;
        public bool TryAddSlot()
        {
            if(!CanAddSlot) return false;
            _isLevelEnded=false; LastLoseReason=LoseReason.None;
            CreateSlot(_battleSlots.Count); RepositionSlots(); AudioManager.Instance?.PlaySfx("SFX_INGAME_ADD");
            _nextCheck=Time.time+.25f; return true;
        }
        public void ApplyAddSlotBooster() { TryAddSlot(); }
    }
}
