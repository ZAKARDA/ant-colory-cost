using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    // Legacy name retained for scene/API compatibility. The visual is a worker ant.
    public class Projectile : MonoBehaviour
    {
        private static readonly HashSet<Projectile> Active = new HashSet<Projectile>();
        private Tile _target;
        private Action _onReturned;
        private bool _finished;
        public static int InFlightCount => Active.Count;
        public static void Fire(Vector3 startPos,Tile target,int colorIndex,float speed=14f)
        {
            if(target==null || !target.TryReserve()) return;
            Dispatch(startPos,target,colorIndex,null,speed);
        }
        public static void Dispatch(Vector3 startPos,Tile reservedTarget,int colorIndex,Action onReturned,float speed=14f)
        {
            GameObject obj = new GameObject("WorkerAnt_CarryingCube");
            obj.transform.position=startPos;
            GameplayVisuals.Ant(obj.transform,colorIndex,.47f).Walking=true;
            var worker=obj.AddComponent<Projectile>();
            worker._target=reservedTarget; worker._onReturned=onReturned; Active.Add(worker);
            worker.StartCoroutine(worker.Collect(startPos,speed));
        }
        public static void CancelAll()
        {
            var copy=new List<Projectile>(Active);
            foreach(var worker in copy)
            {
                if(worker==null) continue;
                worker._onReturned=null;
                worker.Finish();
                worker.gameObject.SetActive(false);
                Destroy(worker.gameObject);
            }
            Active.Clear();
        }
        private IEnumerator Collect(Vector3 home,float speed)
        {
            if(_target==null) { Finish(); Destroy(gameObject); yield break; }
            var path=PictureGrid.Instance!=null?PictureGrid.Instance.GetPathToCube(_target.CubeDataId,home):new List<Vector3>();
            if(path.Count<2) { Finish(); Destroy(gameObject); yield break; }
            for(int i=1;i<path.Count;i++) yield return Travel(path[i-1],path[i],speed);
            if(_target!=null&&!_target.IsDestroyed)
            {
                int color=_target.ColorIndex;
                _target.HitAndDestroy();
                GameplayVisuals.Shape(transform,"CarriedCube",PrimitiveType.Cube,new Vector3(0,.22f,-.24f),Vector3.one*.12f,GameplayVisuals.Pastel(color));
            }
            for(int i=path.Count-1;i>0;i--) yield return Travel(path[i],path[i-1],speed);
            Finish(); Destroy(gameObject);
        }
        private IEnumerator Travel(Vector3 from,Vector3 to,float speed)
        {
            float duration=Mathf.Max(.02f,Vector3.Distance(from,to)/Mathf.Max(1,speed));
            float elapsed=0;
            Vector3 delta=to-from;
            Quaternion facing=delta.sqrMagnitude>.001f
                ? Quaternion.Euler(0,0,Mathf.Atan2(-delta.x,delta.y)*Mathf.Rad2Deg)
                : transform.rotation;
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime; float t=Mathf.Min(1,elapsed/duration);
                transform.rotation=Quaternion.RotateTowards(transform.rotation,facing,720f*Time.deltaTime);
                transform.position=Vector3.Lerp(from,to,t);
                yield return null;
            }
            transform.position=to;
        }
        private void Finish()
        {
            if(_finished) return;
            _finished=true; Active.Remove(this);
            if(_target!=null) _target.ReleaseReservation();
            var callback=_onReturned; _onReturned=null; callback?.Invoke();
        }
        private void OnDestroy() { Finish(); }
    }
}
