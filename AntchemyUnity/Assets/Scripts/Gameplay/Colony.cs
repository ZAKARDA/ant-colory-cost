using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Antchemy
{
    // Colony naming is inherited from native level JSON; each instance is a colony of ants.
    public class Colony : MonoBehaviour
    {
        public ColonyState CurrentState = ColonyState.InQueue;
        public int ColorIndex { get; private set; }
        public int AntCount { get; private set; }
        public int InitialAntCount { get; private set; }
        public int SourceQueueIndex { get; private set; }
        public int SourceSpawnerIndex { get; private set; } = -1;
        public int SourceSpawnerQueueIndex { get; private set; } = -1;
        public int SourceTokenIndex { get; private set; }
        public int IceHp { get; private set; }
        public int ObstacleType { get; private set; }
        public int ObstaclePairId { get; private set; }
        private int _tunnelCovers;
        private int _tunnelRemaining;
        public bool IsCoveredByTunnel => _tunnelCovers>0;
        public bool CanDeploy => !IsLockPlaceholder&&!IsCoveredByTunnel&&IceHp<=0&&ObstacleType==0;
        public void SetSpawnerSource(int spawner,int index,int tokenIndex) { SourceSpawnerIndex=spawner; SourceSpawnerQueueIndex=index; SourceTokenIndex=tokenIndex; }
        public void AddTunnelCover(int remaining) { _tunnelCovers++; _tunnelRemaining=Mathf.Max(_tunnelRemaining,remaining); RefreshPresentation(); }
        public void UpdateTunnelCount(int remaining) { _tunnelRemaining=remaining; UpdateText(); }
        public void RemoveTunnelCover() { _tunnelCovers=Mathf.Max(0,_tunnelCovers-1); RefreshPresentation(); }
        public void ReduceIce() { if(IceHp<=0) return; IceHp--; RefreshPresentation(); }
        private void RefreshPresentation() { SetQueuePresentation(_visual!=null&&_visual.activeSelf,IsFrontColony); }
        public int LinkedGroupId { get; private set; } = -1;
        public bool IsLockPlaceholder => InitialAntCount==0&&ObstacleType==0;
        public bool IsHidden { get; private set; }
        private Renderer _tray;
        private GameObject _antVisual;
        private AntMotion _antMotion;
        public void SetSourceQueueIndex(int index) { SourceQueueIndex=index; }
        public void SetLinkedGroup(int group)
        {
            LinkedGroupId=group;
            if(group>=0)
            {
                GameplayVisuals.Shape(_visual.transform,"LinkBadge",PrimitiveType.Sphere,new Vector3(-.31f,-.24f,-.22f),new Vector3(.17f,.1f,.08f),new Color(1,.78f,.2f));
                GameplayVisuals.Shape(_visual.transform,"LinkBadge",PrimitiveType.Sphere,new Vector3(-.21f,-.24f,-.22f),new Vector3(.17f,.1f,.08f),new Color(1,.78f,.2f));
            }
        }
        public int InFlightCount { get; private set; }
        public ColonyLane ParentLane { get; set; }
        public Nest AssignedSlot { get; private set; }
        public bool IsFrontColony { get; set; }
        public bool CanUndoSelection => (CurrentState==ColonyState.MovingToSlot || CurrentState==ColonyState.ActiveShooting) && AntCount==InitialAntCount && InFlightCount==0;
        private TextMesh _countText;
        private Collider _collider;
        private GameObject _visual;
        private void OnDisable() { StopAllCoroutines(); }
        public void Initialize(ColonyData data,ColonyLane lane)
        {
            ColorIndex=data.ColorIndex; AntCount=Mathf.Max(0,data.AntCount); InitialAntCount=AntCount;
            ParentLane=lane; CurrentState=ColonyState.InQueue; IsHidden=data.IsHidden; IceHp=data.IceHp; ObstacleType=data.ObstacleType; ObstaclePairId=data.ObstaclePairId;
            _visual=new GameObject("ColonyVisual"); _visual.transform.SetParent(transform,false);
            _tray=GameplayVisuals.Shape(_visual.transform,"ColonyTray",PrimitiveType.Cube,Vector3.zero,new Vector3(.98f,.83f,.23f),ColorPalette.GetColonyColor(ColorIndex)).GetComponent<Renderer>();
            // Keep the neutral tile only for hidden, locked, and blocked tokens.
            _tray.enabled=IsHidden||IsLockPlaceholder||IsCoveredByTunnel||IceHp>0||ObstacleType!=0;
            _antVisual=new GameObject("AntCharacter"); _antVisual.transform.SetParent(_visual.transform,false);
            _antMotion=GameplayVisuals.Ant(_antVisual.transform,ColorIndex,.9f);
            var textObj=new GameObject("AntCount"); textObj.transform.SetParent(_visual.transform,false);
            textObj.transform.localPosition=new Vector3(.35f,-.26f,-.33f);
            _countText=textObj.AddComponent<TextMesh>(); _countText.fontSize=48; _countText.characterSize=IsLockPlaceholder?.035f:.065f;
            _countText.anchor=TextAnchor.MiddleCenter; _countText.alignment=TextAlignment.Center; _countText.fontStyle=FontStyle.Bold;
            _countText.color=new Color(.14f,.18f,.23f); UpdateText();
            var collider=gameObject.AddComponent<BoxCollider>(); collider.size=new Vector3(1,.86f,.45f); _collider=collider;
        }
        public ColonyData ExportData() => new ColonyData { ColorIndex=ColorIndex,AntCount=AntCount,IsHidden=IsHidden,IceHp=IceHp,ObstacleType=ObstacleType,ObstaclePairId=ObstaclePairId };
        public void SetQueuePresentation(bool visible,bool front)
        {
            IsFrontColony=front;
            if(front&&!IsCoveredByTunnel&&IceHp<=0) IsHidden=false;
            if(_visual!=null) _visual.SetActive(visible);
            if(_antVisual!=null) _antVisual.SetActive(!IsHidden&&!IsLockPlaceholder&&!IsCoveredByTunnel&&IceHp<=0&&ObstacleType==0);
            if(_tray!=null)
            {
                bool needsTile=IsHidden||IsLockPlaceholder||IsCoveredByTunnel||IceHp>0||ObstacleType!=0;
                _tray.enabled=needsTile;
                if(needsTile) GameplayVisuals.Paint(_tray,new Color(.66f,.72f,.79f));
            }
            UpdateText();
            if(_collider!=null) _collider.enabled=visible&&(front||CurrentState==ColonyState.InQueue);
        }
        private void UpdateText()
        {
            if(_countText==null) return;
            bool special=IsLockPlaceholder||IsCoveredByTunnel||IceHp>0||ObstacleType!=0||IsHidden;
            _countText.characterSize=special?.035f:.065f; _countText.fontSize=special?36:48;
            _countText.transform.localPosition=special?new Vector3(0,0,-.33f):new Vector3(.35f,-.26f,-.33f);
            _countText.color=new Color(.14f,.18f,.23f);
            _countText.text=IsCoveredByTunnel?"GATE "+_tunnelRemaining:IceHp>0?"ICE "+IceHp:ObstacleType==1?"TNT":ObstacleType==2?"ROCK":IsLockPlaceholder?"LOCK":IsHidden?"?":AntCount.ToString();
        }
        private void OnMouseDown()
        {
            if(Time.timeScale<=0 || (UIManager.Instance!=null&&UIManager.Instance.HasOpenModal) || (EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())) return;
            if(CurrentState==ColonyState.InQueue&&!IsFrontColony) { UIManager.Instance?.ShowToast("Tap the first ant in a lane"); return; }
            LevelManager.Instance?.OnColonyTapped(this);
        }
        public void MoveToSlot(Nest slot,Vector3 targetPos,float duration=.25f)
        {
            StopAllCoroutines();
            CurrentState=ColonyState.MovingToSlot; AssignedSlot=slot; IsFrontColony=false;
            if(_collider!=null) _collider.enabled=false;
            IsHidden=false; _antVisual.SetActive(true); _antMotion.Walking=true; _tray.enabled=false; UpdateText();
            _visual.SetActive(true);
            transform.SetParent(slot.transform,true); transform.localScale=Vector3.one;
            AudioManager.Instance?.PlaySfx("colony_pick_2");
            StartCoroutine(Move(duration));
        }
        private IEnumerator Move(float duration)
        {
            Vector3 start=transform.position; float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime;
                transform.position=Vector3.Lerp(start,AssignedSlot.transform.position,Mathf.SmoothStep(0,1,elapsed/duration));
                yield return null;
            }
            transform.localPosition=Vector3.zero;
            CurrentState=ColonyState.ActiveShooting;
            _antMotion.Walking=false;
            yield return new WaitForSeconds(.12f);
            while(CurrentState==ColonyState.ActiveShooting)
            {
                if(LevelManager.Instance==null) yield break;
                if(LevelManager.Instance.IsLevelEnded) { yield return null; continue; }
                if(AntCount<=0)
                {
                    if(InFlightCount==0&&Complete()) yield break;
                }
                else if(InFlightCount<3 && PictureGrid.Instance!=null)
                {
                    Tile target=PictureGrid.Instance.ReserveTargetCube(ColorIndex,transform.position);
                    if(target!=null)
                    {
                        AntCount--; InFlightCount++; UpdateText();
                        Projectile.Dispatch(transform.position,target,ColorIndex,OnWorkerReturned);
                    }
                    // A blocked colony keeps its ants and waits for another color to expose a route.
                }
                yield return new WaitForSeconds(.20f);
            }
        }
        private void OnWorkerReturned()
        {
            InFlightCount=Mathf.Max(0,InFlightCount-1);
            if(this!=null && CurrentState==ColonyState.ActiveShooting && AntCount==0 && InFlightCount==0) Complete();
            LevelManager.Instance?.CheckGameStateAfterMove();
        }
        private bool Complete()
        {
            return LevelManager.Instance!=null&&LevelManager.Instance.TryCompleteColonyGroup(this);
        }
        public void CompleteAfterGroupResolved()
        {
            if(CurrentState==ColonyState.Completed) return;
            CurrentState=ColonyState.Completed;
            if(AssignedSlot!=null) AssignedSlot.ClearSlot(); AssignedSlot=null;
            AudioManager.Instance?.PlaySfx("sfx_clear_colony");
            LevelManager.Instance?.OnColonyFinished(this);
            gameObject.SetActive(false); Destroy(gameObject);
        }
        public bool UndoSelection()
        {
            if(!CanUndoSelection||ParentLane==null) return false;
            StopAllCoroutines(); if(AssignedSlot!=null) AssignedSlot.ClearSlot(); AssignedSlot=null;
            CurrentState=ColonyState.InQueue;
            _antMotion.Walking=false;
            transform.SetParent(ParentLane.transform,true);
            ParentLane.ReturnToFront(this);
            return true;
        }
    }
}
