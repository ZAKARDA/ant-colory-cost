using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    public class ColonyLane : MonoBehaviour
    {
        public int ColumnIndex { get; private set; }
        private readonly List<Colony> _colonies=new List<Colony>();
        public int RemainingColonies => _colonies.Count;
        public Colony FrontColony => _colonies.Count>0?_colonies[0]:null;
        public IReadOnlyList<Colony> Colonies => _colonies;
        private Coroutine _advance;
        public void Initialize(int colIndex,LaneColumn columnData)
        {
            ColumnIndex=colIndex; _colonies.Clear();
            var tokens=new List<QueueToken>();
            for(int i=0;i<columnData.ColonyDatas.Count;i++) tokens.Add(new QueueToken {Data=columnData.ColonyDatas[i],SourceIndex=i});
            if(columnData.ColonySpawnerDatas!=null)
            {
                var spawners=new List<ColonySpawnerData>(columnData.ColonySpawnerDatas); spawners.Sort((a,b)=>a.Index.CompareTo(b.Index));
                foreach(var spawner in spawners) tokens.Insert(Mathf.Clamp(spawner.Index,Mathf.Min(1,tokens.Count),tokens.Count),new QueueToken {Spawner=spawner,SourceIndex=-1});
            }
            for(int tokenIndex=0;tokenIndex<tokens.Count;tokenIndex++)
            {
                var token=tokens[tokenIndex];
                if(token.Spawner==null) CreateQueuedColony(token.Data,token.SourceIndex,-1,-1,tokenIndex);
                else for(int i=0;i<token.Spawner.ColonyDatas.Count;i++) CreateQueuedColony(token.Spawner.ColonyDatas[i],-1,token.Spawner.Index,i,tokenIndex);
            }
            UpdateFrontColonyStatus();
        }
        private class QueueToken { public ColonyData Data; public ColonySpawnerData Spawner; public int SourceIndex; }
        private void CreateQueuedColony(ColonyData data,int sourceIndex,int spawnerIndex,int spawnerQueueIndex,int tokenIndex)
        {
            var obj=new GameObject($"Colony_Column{ColumnIndex}_{_colonies.Count}"); obj.transform.SetParent(transform,false);
            obj.transform.localPosition=GetLocalPositionForIndex(_colonies.Count);
            var colony=obj.AddComponent<Colony>(); colony.Initialize(data,this); colony.SetSourceQueueIndex(sourceIndex);
            colony.SetSpawnerSource(spawnerIndex,spawnerQueueIndex,tokenIndex); _colonies.Add(colony);
        }
        public Vector3 GetLocalPositionForIndex(int index) => new Vector3(0,-index*1.05f,0);
        public bool TryRemoveFrontColony(Colony expected)
        {
            if(expected==null || FrontColony!=expected || !expected.IsFrontColony || expected.CurrentState!=ColonyState.InQueue) return false;
            expected.IsFrontColony=false;
            if(_advance!=null) StopCoroutine(_advance);
            _colonies.RemoveAt(0); UpdateFrontColonyStatus();
            _advance=StartCoroutine(Advance()); return true;
        }
        public void RemoveFrontColony() { TryRemoveFrontColony(FrontColony); }
        public bool IsContiguousFrontGroup(ICollection<Colony> group)
        {
            int memberCount=0;
            foreach(var colony in _colonies) if(group.Contains(colony)) memberCount++;
            for(int i=0;i<memberCount;i++) if(!group.Contains(_colonies[i])) return false;
            return true;
        }
        public void RemoveLinkedPrefix(ICollection<Colony> group)
        {
            if(!_colonies.Exists(colony=>group.Contains(colony))) return;
            if(_advance!=null) StopCoroutine(_advance);
            _colonies.RemoveAll(colony=>group.Contains(colony));
            UpdateFrontColonyStatus(); _advance=StartCoroutine(Advance());
        }
        public void RemoveUnlockedPlaceholder(Colony colony)
        {
            if(!_colonies.Remove(colony)) return;
            if(_advance!=null) StopCoroutine(_advance);
            colony.gameObject.SetActive(false); Destroy(colony.gameObject);
            UpdateFrontColonyStatus(); _advance=StartCoroutine(Advance());
        }
        public void ReturnToFront(Colony colony)
        {
            if(_advance!=null) StopCoroutine(_advance);
            _colonies.Insert(0,colony);
            for(int i=0;i<_colonies.Count;i++) _colonies[i].transform.localPosition=GetLocalPositionForIndex(i);
            UpdateFrontColonyStatus();
        }
        private IEnumerator Advance()
        {
            var snapshot=_colonies.ToArray(); var starts=new Vector3[snapshot.Length];
            for(int i=0;i<snapshot.Length;i++) starts[i]=snapshot[i].transform.localPosition;
            float elapsed=0;
            while(elapsed<.18f)
            {
                elapsed+=Time.deltaTime; float t=Mathf.SmoothStep(0,1,elapsed/.18f);
                for(int i=0;i<snapshot.Length;i++) if(snapshot[i]!=null&&snapshot[i].CurrentState==ColonyState.InQueue) snapshot[i].transform.localPosition=Vector3.Lerp(starts[i],GetLocalPositionForIndex(i),t);
                yield return null;
            }
            for(int i=0;i<_colonies.Count;i++) _colonies[i].transform.localPosition=GetLocalPositionForIndex(i);
            _advance=null; UpdateFrontColonyStatus();
        }
        public bool ShuffleQueue(System.Random rng)
        {
            var slots=new List<int>();
            for(int i=0;i<_colonies.Count;i++) if(_colonies[i].CanDeploy&&_colonies[i].LinkedGroupId<0&&_colonies[i].CurrentState==ColonyState.InQueue) slots.Add(i);
            if(slots.Count<2) return false;
            var items=new List<Colony>(); foreach(var s in slots) items.Add(_colonies[s]);
            for(int i=items.Count-1;i>0;i--) { int j=rng.Next(i+1); var t=items[i]; items[i]=items[j]; items[j]=t; }
            for(int k=0;k<slots.Count;k++) _colonies[slots[k]]=items[k];
            if(_advance!=null) { StopCoroutine(_advance); _advance=null; }
            for(int i=0;i<_colonies.Count;i++) _colonies[i].transform.localPosition=GetLocalPositionForIndex(i);
            UpdateFrontColonyStatus(); return true;
        }
        private void UpdateFrontColonyStatus()
        {
            for(int i=0;i<_colonies.Count;i++) _colonies[i].SetQueuePresentation(i<3,i==0);
        }
    }
}
